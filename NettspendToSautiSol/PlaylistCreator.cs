using System.Text;
using System.Text.Json;

namespace NettspendToSautiSol
{
    public class PlaylistCreator
    {
        private readonly List<ArtistNode> _artists;
        private readonly int _tracksPerArtist;
        private readonly string _firstArtist;
        private readonly string _secondArtist;
        private readonly bool _lookForFeatures;
        private SpotifyAuthorizer _spotifyAuthorizer;
        private readonly string _accessToken;
        
        public PlaylistCreator(List<ArtistNode> artists, int tracksPerArtist, bool lookForFeatures, string firstArtist, string secondArtist)
        {
            _spotifyAuthorizer = new SpotifyAuthorizer();
            _accessToken = _spotifyAuthorizer.GetAuthoizationPKCEAccessToken();
            _lookForFeatures = lookForFeatures;
            _artists = artists;
            _tracksPerArtist = tracksPerArtist;
            _firstArtist = firstArtist;
            _secondArtist = secondArtist;   
            
            AddToPlaylist(GetSongs());
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
                    name = $"from {_firstArtist} to {_secondArtist}",
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

        private void AddToPlaylist(List<string> trackIds)
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

                // Create the query string for both artists
                string rawQuery = $"track:{artist1.Name} {artist2.Name}";
                string encodedQuery = Uri.EscapeDataString(rawQuery);
                string url = $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit=10";

                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode) 
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return null;
                }

                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var document = JsonDocument.Parse(jsonResponse);

                var trackItems = document.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray();
                if (trackItems.Count() == 0)
                {
                    Console.WriteLine("No Feature Found between " + artist1.Name + " and " + artist2.Name);
                    return null;
                }

                foreach (var track in trackItems)
                {
                    var artists = track.GetProperty("artists").EnumerateArray();
                    bool hasArtist1 = artists.Any(a => a.GetProperty("name").GetString() == artist1.Name);
                    bool hasArtist2 = artists.Any(a => a.GetProperty("name").GetString() == artist2.Name);

                    // If both artists are found in the track's artists array, return the song ID
                    if (hasArtist1 && hasArtist2)
                    {
                        Console.WriteLine("Found Feature: " + track.GetProperty("name").GetString());
                        song = track.GetProperty("id").GetString();
                        break;
                    }
                }
            }
            return song; 
        }

        private List<string> FindSongsForArtist(ArtistNode artist, int tracksPerArtist)
        {
            List<string> songs = new List<string>();
            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine($"Searching for songs for {artist.Name}");
                Console.WriteLine($"Number of songs to search for: {tracksPerArtist}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                
                string query = $"q=artist:\"{artist.Name}\"&type=track&limit={tracksPerArtist}";
                string url = $"https://api.spotify.com/v1/search?{query}";
                Console.WriteLine(url);
                
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode) {Console.WriteLine("Error: " + response.StatusCode); return songs;}
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                var document = JsonDocument.Parse(jsonResponse); 
                var topSongs = document.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray();
                foreach (var song in topSongs)
                {
                    string? songId = song.GetProperty("id").GetString();
                    if (songId == null)
                    {
                        break;
                    }
                    Console.WriteLine($"Adding Song " + song.GetProperty("name").GetString() + $" for {artist.Name}");
                    songs.Add(songId);
                }
            }

            return songs;
        }
        
 
    }
}


