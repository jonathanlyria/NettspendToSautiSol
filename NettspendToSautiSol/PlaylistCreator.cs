using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        
        public PlaylistCreator(List<ArtistNode> artists, int tracksPerArtist, bool lookForFeatures, string _pkceToken)
        {
            _lookForFeatures = lookForFeatures;
            _songs = new List<string>();
            _artists = artists;
            _tracksPerArtist = tracksPerArtist;
            _accessToken = _pkceToken;
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
                    foreach (var song in FindSongsForArtist(_artists[i], songsPerArtist[i]))
                    {
                        tracks.Add(song);
                    }
                    tracks.Add(FindFeature(_artists[i], _artists[i + 1]));
                }
                else
                {
                    foreach (var song in FindSongsForArtist(_artists[i], songsPerArtist[i]))
                    {
                        tracks.Add(song);
                    }
                }
                
            }

            return tracks;
        }
       
        
        public string CreatePlaylist()
        {
            string playlistId = "";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                // Get the user ID
                string userId = GetUserId();

                // Prepare the request payload
                var payload = new
                {
                    name = $"from {_artists.First().Name} to {_artists.Last().Name}",
                    description = $"tool created by,  creates a playlist that transitions between" +
                                  $" {_artists.First().Name} and {_artists.First().Name}.",
                    @public = true
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // Send the POST request to create the playlist
                var response = client.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"Playlist created successfully: {jsonResponse}");
                    var document = JsonDocument.Parse(jsonResponse);
                    playlistId = document.RootElement.GetProperty("id").GetString();
                    Console.WriteLine($"Playlist ID: {playlistId}");
                    return playlistId;
                }
                else
                {
                    Console.WriteLine($"Failed to create playlist. Status code: {response.StatusCode}");
                    Console.WriteLine($"Response: {response.Content.ReadAsStringAsync().Result}");
                    return playlistId;
                }
            }
            
        }
        public void AddToPlaylist(List<string> trackIds)
        {
            using (HttpClient client = new HttpClient())
            {
                string playlistId = CreatePlaylist();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                // Spotify API endpoint for adding tracks to a playlist
                string url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";

                // Prepare the request body with track URIs
                var requestBody = new
                {
                    uris = trackIds.Select(trackId => $"spotify:track:{trackId}").ToArray()
                };

                // Serialize the request body to JSON
                string jsonBody = JsonSerializer.Serialize(requestBody);

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send POST request
                var response = client.PostAsync(url, content).Result;

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

                // Send GET request to Spotify's "me" endpoint
                var response = client.GetAsync("https://api.spotify.com/v1/me").Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to retrieve user ID. Status code: {response.StatusCode}");
                    Console.WriteLine($"Response: {response.Content.ReadAsStringAsync().Result}");
                    throw new Exception("Failed to retrieve user ID.");
                }

                // Parse the response to extract the user ID
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var document = JsonDocument.Parse(jsonResponse);
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

                // Create the query stnring for both artists
                string query = Uri.EscapeDataString($"{artist1.Name} {artist2.Name}");
                string url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=10";

                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode) 
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return null;
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var document = JsonDocument.Parse(jsonResponse);

                if (!document.RootElement.TryGetProperty("tracks", out var tracks) || 
                    !tracks.TryGetProperty("items", out var items))
                {
                    Console.WriteLine($"No tracks found in search response for {artist1.Name} and {artist2.Name}");
                    return null;
                }

                var trackItems = items.EnumerateArray();
                foreach (var track in trackItems)
                {
                    var artists = track.GetProperty("artists").EnumerateArray();
                    bool hasArtist1 = artists.Any(a => string.Equals(a.GetProperty("name").GetString(), artist1.Name, StringComparison.OrdinalIgnoreCase));
                    bool hasArtist2 = artists.Any(a => string.Equals(a.GetProperty("name").GetString(), artist2.Name, StringComparison.OrdinalIgnoreCase));

                    // If both artists are found in the track's artists array, return the song ID
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
            // Convert to lowercase
            songName = songName.ToLower();

            // Remove content in parentheses or after a dash
            songName = Regex.Replace(songName, @"\s*-\s*.*|\s*\(.*?\)", "");

            // Remove punctuation
            songName = Regex.Replace(songName, @"[^\w\s]", "");

            // Remove extra spaces and trim
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

                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return songs;
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var document = JsonDocument.Parse(jsonResponse);
                var topSongs = document.RootElement.GetProperty("tracks").EnumerateArray(); // Fixed "items" to "tracks"

                List<string> potentialSongs = new();
                foreach (var song in topSongs)
                {
                    // Check if the song has only one artist (the primary artist)
                    var songArtists = song.GetProperty("artists").EnumerateArray();
                    bool hasFeatures = songArtists.Count() > 1; // More than one artist indicates features

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

        
 
    }
}


