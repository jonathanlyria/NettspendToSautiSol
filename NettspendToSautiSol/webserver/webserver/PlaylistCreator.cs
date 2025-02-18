using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
// Citation of Spotify API 
// Citation of Regex
namespace NettspendToSautiSol
{
    public class PlaylistCreator
    {
        private readonly List<ArtistNode> _artists;
        private readonly int _tracksPerArtist;
        private readonly bool _lookForFeatures;
        private SpotifyAuthorizer _spotifyAuthorizer;
        private List<string> _songs;
        private readonly string _accessToken;
        private string playlistId;
        
        public PlaylistCreator(List<ArtistNode> artists, int tracksPerArtist, bool lookForFeatures, string _pkceToken)
        {
            _lookForFeatures = lookForFeatures;
            _songs = new List<string>();
            _artists = artists;
            _tracksPerArtist = tracksPerArtist;
            _accessToken = _pkceToken;
            CreatePlaylist();
        }


        public List<string> GetSongs()
        {
            List<string> tracks = new List<string>();
            List<int> songsPerArtist = new List<int>();

            for (int i = 0; i < _artists.Count(); i++)
            {
                songsPerArtist.Add(_tracksPerArtist);
            }

            for (int i = 0; i < _artists.Count(); i++)
            {
                string? feature = FindFeature(_artists[i], _artists[i]);
                if (_lookForFeatures && i + 1 < _artists.Count() && FindFeature(_artists[i], _artists[i + 1]) != null)
                {
                    songsPerArtist[i]--;
                    songsPerArtist[i + 1]--;
                    foreach (string song in FindSongsForArtist(_artists[i], songsPerArtist[i]))
                    {
                        tracks.Add(song);
                    }
                    tracks.Add(FindFeature(_artists[i], _artists[i + 1]));
                }
                else
                {
                    foreach (string song in FindSongsForArtist(_artists[i], songsPerArtist[i]))
                    {
                        tracks.Add(song);
                    }
                }
                
            }

            return tracks;
        }
       
        
        public void CreatePlaylist()
        {

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string userId = GetUserId();

                var payload = new
                {
                    name = $"from {_artists.First().Name} to {_artists.Last().Name}",
                    description = $"tool created by,  creates a playlist that transitions between" +
                                  $" {_artists.First().Name} and {_artists.First().Name}.",
                    @public = true
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                StringContent content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"Playlist created successfully: {jsonResponse}");
                    JsonDocument document = JsonDocument.Parse(jsonResponse);
                    playlistId = document.RootElement.GetProperty("id").GetString();
                    Console.WriteLine($"Playlist ID: {playlistId}");
                }
                else
                {
                    Console.WriteLine($"Failed to create playlist. Status code: {response.StatusCode}");
                    Console.WriteLine($"Response: {response.Content.ReadAsStringAsync().Result}");
                }
            }
            
        }
        public void AddToPlaylist(List<string> trackIds)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";

                var requestBody = new
                {
                    uris = trackIds.Select(trackId => $"spotify:track:{trackId}").ToArray()
                };
                
                string jsonBody = JsonSerializer.Serialize(requestBody);

                StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(url, content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to add tracks to playlist. Status code: {response.StatusCode}");
                    Console.WriteLine($"Response: {response.Content.ReadAsStringAsync().Result}");
                    throw new Exception("Failed to add tracks to the playlist.");
                }

                Console.WriteLine("Tracks successfully added to the playlist.");
            }
        }
        
        private string GetUserId()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                HttpResponseMessage response = client.GetAsync("https://api.spotify.com/v1/me").Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to retrieve user ID. Status code: {response.StatusCode}");
                    Console.WriteLine($"Response: {response.Content.ReadAsStringAsync().Result}");
                    throw new Exception("Failed to retrieve user ID.");
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);
                string userId = document.RootElement.GetProperty("id").GetString();

                return userId;
            }
        }

        private string? FindFeature(ArtistNode artist1, ArtistNode artist2)
        {
            string? song = null;
            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine($"Searching for feature between {artist1.Name} and {artist2.Name}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string query = Uri.EscapeDataString($"{artist1.Name} {artist2.Name}");
                string url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=10";

                HttpResponseMessage response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode) 
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return null;
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);

                if (!document.RootElement.TryGetProperty("tracks", out var tracks) || 
                    !tracks.TryGetProperty("items", out var items))
                {
                    Console.WriteLine($"No tracks found in search response for {artist1.Name} and {artist2.Name}");
                    return null;
                }

                JsonElement.ArrayEnumerator trackItems = items.EnumerateArray();
                foreach (JsonElement track in trackItems)
                {
                    JsonElement.ArrayEnumerator artists = track.GetProperty("artists").EnumerateArray();
                    bool hasArtist1 = artists.Any(a => string.Equals(a.GetProperty("name").GetString(), artist1.Name, StringComparison.OrdinalIgnoreCase));
                    bool hasArtist2 = artists.Any(a => string.Equals(a.GetProperty("name").GetString(), artist2.Name, StringComparison.OrdinalIgnoreCase));

                    if (hasArtist1 && hasArtist2 && artists.Count() == 2)
                    {
                        Console.WriteLine($"Found Feature: {track.GetProperty("name").GetString()}");
                        song = track.GetProperty("id").GetString();
                        break;
                    }
                }

                if (song == null)
                {
                    Console.WriteLine($"No feature found between {artist1.Name} and {artist2.Name}");
                }
            }

            return song;  
        }
        private string NormalizeSongName(string songName)
        {
            songName = songName.ToLower();

            songName = Regex.Replace(songName, @"\s*-\s*.*|\s*\(.*?\)", "");
            songName = Regex.Replace(songName, @"[^\w\s]", "");
            songName = Regex.Replace(songName, @"\s{2,}", " ").Trim();

            return songName;
        }

        private List<string> FindSongsForArtist(ArtistNode artist, int tracksPerArtist)
        {
            List<string> songs = new List<string>();
            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine($"Searching for songs for {artist.Name}");
                Console.WriteLine($"Number of songs to search for: {tracksPerArtist}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string url = $"https://api.spotify.com/v1/artists/{artist.SpotifyId}/top-tracks?market=US"; // Added market parameter
                Console.WriteLine(url);

                HttpResponseMessage response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return songs;
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);
                JsonElement.ArrayEnumerator topSongs = document.RootElement.GetProperty("tracks").EnumerateArray(); 

                List<string> potentialSongs = new();
                foreach (JsonElement song in topSongs)
                {
                    JsonElement.ArrayEnumerator songArtists = song.GetProperty("artists").EnumerateArray();
                    bool hasFeatures = songArtists.Count() > 1;

                    if (!hasFeatures)
                    {
                        string? songId = song.GetProperty("id").GetString();
                        if (songId != null)
                        {
                            string songName = NormalizeSongName(song.GetProperty("name").GetString());
                            
                            bool isDuplicate = _songs.Any(existingSong =>
                                existingSong.Contains($" {songName} ") || 
                                songName.Contains($" {existingSong} ") || 
                                existingSong.Equals(songName));

                            if (!isDuplicate)
                            {
                                potentialSongs.Add(songId);
                                _songs.Add(songName);
                            }
                        }
                    }
                }
                if (potentialSongs.Count > 0)
                {
                    Random random = new Random();
                    potentialSongs = potentialSongs.OrderBy(x => random.Next()).Take(tracksPerArtist).ToList();
                    songs.AddRange(potentialSongs);
                }
            }
            return songs;
        }
        public string GetPlaylistLink()
        {
            return $"https://open.spotify.com/playlist/{playlistId}";
        }

        
 
    }
}


