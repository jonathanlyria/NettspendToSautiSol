using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NettspendToSautiSol
{
    public class GetPlaylistSongsService : IGetPlaylistSongsService
    {
        private readonly ISpotifyClientCredentialAuthorizer _clientCredentialAuthorizer;
        private readonly HttpClient _httpClient;
        private string _accessToken;
        private DateTime _tokenExpiryTime;

        public GetPlaylistSongsService(ISpotifyClientCredentialAuthorizer clientCredentialAuthorizer, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _clientCredentialAuthorizer = clientCredentialAuthorizer; 
            RefreshAccessToken();
        }
        
        private async Task RefreshAccessToken()
        {
            if (DateTime.UtcNow >= _tokenExpiryTime.AddMinutes(-1))
            {
                try
                {
                    (string AccessToken, int ExpiresIn) tokenData = await _clientCredentialAuthorizer.GetAccessToken();
                    _accessToken = tokenData.AccessToken;
                    _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Access token refreshed successfully.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing access token: {ex.Message}");
                    throw new Exception("Failed to refresh access token.", ex);
                }
            }
        }

        public async Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists)
        {
            await RefreshAccessToken();
            
            List<string> tracks = new List<string>();
            List<int> songsPerArtist = pathOfArtists.Select(_ => 3).ToList();

            for (int i = 0; i < pathOfArtists.Count; i++)
            {
                if (i + 1 < pathOfArtists.Count && await FindFeature(pathOfArtists[i], pathOfArtists[i + 1]) is string feature)
                {
                    songsPerArtist[i]--;
                    songsPerArtist[i + 1]--;
                    
                    var artistSongs = await FindSongsForArtist(
                        pathOfArtists[i], 
                        songsPerArtist[i], 
                        tracks);
                        
                    tracks.AddRange(artistSongs);
                    tracks.Add(feature);
                }
                else
                {
                    var artistSongs = await FindSongsForArtist(
                        pathOfArtists[i], 
                        songsPerArtist[i], 
                        tracks);
                        
                    tracks.AddRange(artistSongs);
                }
            }
            
            return tracks;
        }

        private async Task<string> FindFeature(ArtistNode artist1, ArtistNode artist2)
        {
            Console.WriteLine($"Searching for feature between {artist1.Name} and {artist2.Name}");
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

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
                    Console.WriteLine($"Found Feature: {trackName}");
                    return songId;
                }
            }

            Console.WriteLine($"No feature found between {artist1.Name} and {artist2.Name}");
            return null;
        }
        
        private async Task<List<string>> FindSongsForArtist(ArtistNode artist, int tracksPerArtist, List<string> currentSongs)
        {
            List<string> songs = new List<string>();
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            string url = $"https://api.spotify.com/v1/artists/{artist.SpotifyId}/top-tracks?market=US";
            Console.WriteLine(url);

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return songs;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonDocument document = JsonDocument.Parse(jsonResponse);
            var topSongs = document.RootElement.GetProperty("tracks").EnumerateArray(); 

            List<string> potentialSongs = new List<string>();
            foreach (JsonElement song in topSongs)
            {
                int artistCount = song.GetProperty("artists").GetArrayLength();
                bool hasFeatures = artistCount > 1;

                if (!hasFeatures)
                {
                    string songId = song.GetProperty("id").GetString();
                    string trackName = song.GetProperty("name").GetString();
                    string normalizedSongName = NormalizeSongName(trackName);
                    
                    bool isDuplicate = currentSongs.Any(existingSong =>
                        existingSong.Contains($" {normalizedSongName} ") || 
                        normalizedSongName.Contains($" {existingSong} ") || 
                        existingSong.Equals(normalizedSongName));

                    if (!isDuplicate)
                    {
                        potentialSongs.Add(songId);
                        currentSongs.Add(normalizedSongName);
                    }
                }
            }
            
            if (potentialSongs.Count > 0)
            {
                Random random = new Random();
                songs = potentialSongs
                    .OrderBy(x => random.Next())
                    .Take(tracksPerArtist)
                    .ToList();
            }

            return songs;
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