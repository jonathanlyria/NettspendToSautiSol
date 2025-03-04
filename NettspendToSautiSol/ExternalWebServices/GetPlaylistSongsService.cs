
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NettspendToSautiSol
{
    public class GetPlaylistSongsService : IGetPlaylistSongsService
    {
        private readonly ISpotifyClientCredentialAuthorizer _clientCredentialAuthorizer;
        private readonly HttpClient _httpClient;

        public GetPlaylistSongsService(ISpotifyClientCredentialAuthorizer clientCredentialAuthorizer, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _clientCredentialAuthorizer = clientCredentialAuthorizer; 
        }
        
        private async Task<string> GetAccessToken()
        {
            string accessToken = string.Empty;
            try
            {
                var tokenData = await _clientCredentialAuthorizer.GetAccessToken();
                accessToken = tokenData.AccessToken;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Access token refreshed successfully.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing access token: {ex.Message}");
                throw new Exception("Failed to refresh access token.", ex);
            }
            return accessToken;
        
        }

        public async Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists)
        {
            string accessToken = await GetAccessToken();
            
            List<string> songIds = new List<string>();  // List to be returned
            List<string> songNames = new List<string>(); // List to track song names for deduplication
            List<int> songsPerArtist = pathOfArtists.Select(_ => 3).ToList();

            for (int i = 0; i < pathOfArtists.Count; i++)
            {
                if (i + 1 < pathOfArtists.Count)
                {
                    var featureResult = await FindFeature(pathOfArtists[i], 
                        pathOfArtists[i + 1],
                        accessToken);
                    if (featureResult != null)
                    {
                        string featureId = featureResult.Value.id;
                        string featureName = featureResult.Value.normalizedName;
                        
                        songsPerArtist[i]--;
                        songsPerArtist[i + 1]--;
                        
                        var artistSongs = await FindSongsForArtist(
                            pathOfArtists[i], 
                            songsPerArtist[i], 
                            songNames, accessToken);
                            
                        songIds.AddRange(artistSongs.Item1);
                        songNames.AddRange(artistSongs.Item2);
                        
                        songIds.Add(featureId);
                        songNames.Add(featureName);
                    }
                    else
                    {
                        var artistSongs = await FindSongsForArtist(
                            pathOfArtists[i], 
                            songsPerArtist[i], 
                            songNames, accessToken);
                            
                        songIds.AddRange(artistSongs.Item1);
                        songNames.AddRange(artistSongs.Item2);
                    }
                }
                else
                {
                    var artistSongs = await FindSongsForArtist(
                        pathOfArtists[i], 
                        songsPerArtist[i], 
                        songNames, accessToken);
                        
                    songIds.AddRange(artistSongs.Item1);
                    songNames.AddRange(artistSongs.Item2);
                }
            }

            foreach (var trackId in songIds)
            {
                Console.WriteLine(trackId);
            }
            
            return songIds;
        }

        private async Task<(string id, string normalizedName)?> FindFeature(ArtistNode artist1, ArtistNode artist2, string accessToken)
        {
            Console.WriteLine($"Searching for feature between {artist1.Name} and {artist2.Name}");
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string query = Uri.EscapeDataString($"{artist1.Name} {artist2.Name}");
            string url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=10";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonDocument document = JsonDocument.Parse(jsonResponse);

            if (!document.RootElement.TryGetProperty("tracks", out var tracks) || 
                !tracks.TryGetProperty("items", out var items))
            {
                Console.WriteLine($"No tracks found in search response for {artist1.Name} and {artist2.Name}");
                return null;
            }

            foreach (JsonElement track in items.EnumerateArray())
            {
                var artists = track.GetProperty("artists").EnumerateArray();
                bool hasArtist1 = artists.Any(a => string.Equals(
                    a.GetProperty("name").GetString(), 
                    artist1.Name, 
                    StringComparison.OrdinalIgnoreCase));
                
                bool hasArtist2 = artists.Any(a => string.Equals(
                    a.GetProperty("name").GetString(), 
                    artist2.Name, 
                    StringComparison.OrdinalIgnoreCase));

                int artistCount = track.GetProperty("artists").GetArrayLength();
                
                if (hasArtist1 && hasArtist2 && artistCount == 2)
                {
                    string trackName = track.GetProperty("name").GetString();
                    string songId = track.GetProperty("id").GetString();
                    string normalizedName = NormalizeSongName(trackName);
                    Console.WriteLine($"Found Feature: {trackName}");
                    return (songId, normalizedName);
                }
            }

            Console.WriteLine($"No feature found between {artist1.Name} and {artist2.Name}");
            return null;
        }

        // Also modify FindSongsForArtist to return tuples
        private async Task<(List<string>, List<string>)> FindSongsForArtist(ArtistNode artist, int tracksPerArtist, List<string> previousSongNames, string accessToken)
        {
            List<string> songIds = new List<string>();
            List<string> songNames = new List<string>();
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            string url = $"https://api.spotify.com/v1/artists/{artist.SpotifyId}/top-tracks?market=US";
            Console.WriteLine(url);

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return (songIds, songNames);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonDocument document = JsonDocument.Parse(jsonResponse);
            var topSongs = document.RootElement.GetProperty("tracks").EnumerateArray(); 

            Dictionary<string, string> potentialSongs = new Dictionary<string, string>(); // Track ID -> Normalized Name
            foreach (JsonElement song in topSongs)
            {
                int artistCount = song.GetProperty("artists").GetArrayLength();
                bool hasFeatures = artistCount > 1;

                if (!hasFeatures)
                {
                    string songId = song.GetProperty("id").GetString();
                    string trackName = song.GetProperty("name").GetString();
                    string normalizedSongName = NormalizeSongName(trackName);
                    
                    bool isDuplicate = songNames.Any(existingName =>
                        existingName.Contains($" {normalizedSongName} ") || 
                        normalizedSongName.Contains($" {existingName} ") || 
                        existingName.Equals(normalizedSongName));

                    if (!isDuplicate)
                    {
                        potentialSongs.Add(songId, normalizedSongName);
                    }
                }
            }
            
            if (potentialSongs.Count > 0)
            {
                Random random = new Random();
                var selectedSongs = potentialSongs
                    .OrderBy(x => random.Next())
                    .Take(tracksPerArtist)
                    .ToList();
                    
                // Add selected song IDs to return list
                foreach (var song in selectedSongs)
                {
                    songIds.Add(song.Key);
                    songNames.Add(song.Value); // Add normalized name to the tracking list
                }
            }

            return (songIds, songNames);
        }
        private string NormalizeSongName(string songName)
        {
            songName = songName.ToLower();

            songName = Regex.Replace(songName, @"\s*-\s*.*|\s*\(.*?\)", "");
            
            songName = Regex.Replace(songName, @"[^\w\s]", "");
            
            songName = Regex.Replace(songName, @"\s{2,}", " ").Trim();

            return songName;
        }
    }
}