using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExternalWebServices.Interfaces;
using GlobalTypes;

namespace ExternalWebServices
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
            try
            {
                var tokenData = await _clientCredentialAuthorizer.GetAccessToken();
                string accessToken = tokenData.AccessToken;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Access token refreshed successfully.");
                Console.ResetColor();
                return accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing access token: {ex.Message}");
                throw new Exception("Failed to refresh access token.", ex);
            }
        }

        public async Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists)
        {
            try
            {
                string accessToken = await GetAccessToken();
                
                List<string> songIds = new List<string>();
                List<string> songNames = new List<string>();
                List<int> songsPerArtist = pathOfArtists.Select(_ => 3).ToList();

                for (int i = 0; i < pathOfArtists.Count; i++)
                {
                    if (i + 1 < pathOfArtists.Count)
                    {
                        try
                        {
                            var featureResult = await FindFeature(pathOfArtists[i],
                                pathOfArtists[i + 1],
                                accessToken);
                            
                            if (featureResult != null)
                            {
                                string featureId = featureResult.Value.id;
                                string featureName = featureResult.Value.name;
                                
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
                                (List<string>, List<string>) artistSongs = await FindSongsForArtist(
                                    pathOfArtists[i], 
                                    songsPerArtist[i], 
                                    songNames, accessToken);
                                    
                                songIds.AddRange(artistSongs.Item1);
                                songNames.AddRange(artistSongs.Item2);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing artists {pathOfArtists[i].Name} and {pathOfArtists[i + 1].Name}: {ex.Message}");
                            
                            try
                            {
                                (List<string>, List<string>) artistSongs = await FindSongsForArtist(
                                    pathOfArtists[i], 
                                    songsPerArtist[i], 
                                    songNames, accessToken);
                                    
                                songIds.AddRange(artistSongs.Item1);
                                songNames.AddRange(artistSongs.Item2);
                            }
                            catch (Exception songEx)
                            {
                                Console.WriteLine($"Failed to get songs for artist {pathOfArtists[i].Name}: {songEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            (List<string>, List<string>) artistSongs = await FindSongsForArtist(
                                pathOfArtists[i], 
                                songsPerArtist[i], 
                                songNames, accessToken);
                                
                            songIds.AddRange(artistSongs.Item1);
                            songNames.AddRange(artistSongs.Item2);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to get songs for artist {pathOfArtists[i].Name}: {ex.Message}");
                        }
                    }
                }

                foreach (var trackId in songIds)
                {
                    Console.WriteLine(trackId);
                }
                
                return songIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in GetPlaylistSongIds: {ex.Message}");
                throw new Exception("Failed to generate playlist song IDs", ex);
            }
        }

        private async Task<(string id, string name)?> FindFeature(ArtistNode artist1, ArtistNode artist2, string accessToken)
        {
            try
            {
                Console.WriteLine($"Searching for feature between {artist1.Name} and {artist2.Name}");
                
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string query = Uri.EscapeDataString($"{artist1.Name} {artist2.Name}");
                string url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=10";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

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
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed when finding feature: {ex.Message}");
                throw new Exception($"Failed to search for feature between {artist1.Name} and {artist2.Name}", ex);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error when finding feature: {ex.Message}");
                throw new Exception($"Failed to parse response for feature between {artist1.Name} and {artist2.Name}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error finding feature: {ex.Message}");
                throw new Exception($"Unexpected error searching for feature between {artist1.Name} and {artist2.Name}", ex);
            }
        }

        private async Task<(List<string>, List<string>)> FindSongsForArtist(ArtistNode artist, int tracksPerArtist, List<string> previousSongNames, string accessToken)
        {
            List<string> songIds = new List<string>();
            List<string> songNames = new List<string>();
            songNames.AddRange(previousSongNames);
            
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string url = $"https://api.spotify.com/v1/artists/{artist.SpotifyId}/top-tracks?market=US";
                Console.WriteLine(url);

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonDocument document = JsonDocument.Parse(jsonResponse);
                var topSongs = document.RootElement.GetProperty("tracks").EnumerateArray(); 

                Dictionary<string, string> potentialSongs = new Dictionary<string, string>();
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
                        
                    foreach (var song in selectedSongs)
                    {
                        songIds.Add(song.Key);
                        songNames.Add(song.Value);
                    }
                }

                return (songIds, songNames);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP request failed for artist {artist.Name}: {ex.Message}");
                throw new Exception($"Failed to get top tracks for artist {artist.Name}", ex);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error for artist {artist.Name}: {ex.Message}");
                throw new Exception($"Failed to parse response for artist {artist.Name}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error finding songs for artist {artist.Name}: {ex.Message}");
                throw new Exception($"Unexpected error getting songs for artist {artist.Name}", ex);
            }
        }
        
        private string NormalizeSongName(string songName)
        {
            try
            {
                songName = songName.ToLower();
                songName = Regex.Replace(songName, @"\s*-\s*.*|\s*\(.*?\)", "");
                songName = Regex.Replace(songName, @"[^\w\s]", "");
                songName = Regex.Replace(songName, @"\s{2,}", " ").Trim();
                return songName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error normalizing song name: {ex.Message}");
                return songName;
            }
        }
    }
}
