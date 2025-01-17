using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NettspendToSautiSol
{
    public class ArtistExpander
    {
        private readonly DatabaseManager _databaseManager;
        private readonly SpotifyClientCredentials _spotifyClientCredentials = new SpotifyClientCredentials();
        private string _accessToken;
        private DateTime _tokenExpiryTime;

        private string _apiKey; // API key
        private int _wastedCalls;
        private int _totalCalls;
        public string DebugLine;
        private PriorityQueue<ArtistNode, double> Queue;

        public ArtistExpander(string apiKey, DatabaseManager databaseManager)
        {
            _apiKey = apiKey;
            _databaseManager = databaseManager;
            _wastedCalls = 0;
            _totalCalls = 0;

           // Queue = _databaseManager.GetExpanderQueue();
           Queue = new PriorityQueue<ArtistNode, double>();

            _accessToken = _spotifyClientCredentials.GetAccessTokenAsync().Result.AccessToken;
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(_spotifyClientCredentials.GetAccessTokenAsync().Result.ExpiresIn);

            Expand();
        }

        private void Expand()
        {
            HashSet<ArtistNode> visited = new HashSet<ArtistNode>();
            int callCount = 0;
            int level = 0;
            

            while (true)
            {
                level++;
                DebugLine =
                    $"\nLevel {level}\nQueue size: {Queue.Count}\nVisited nodes: {visited.Count}\nTotal calls: " +
                    $"{_totalCalls}\nCall Efficiency: {(_totalCalls - _wastedCalls) / (double)_totalCalls:P2}\nCalls to Artists Found: " +
                    $"{_totalCalls}:{visited.Count}";
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(DebugLine);
                Console.ResetColor();

                int queueLength = Queue.Count;
                for (int i = 0; i < queueLength; i++)
                {

                    ArtistNode currentNode = Queue.Dequeue();
                    var connections = GetConnections(currentNode);

                    callCount++;
                    Console.WriteLine($"{callCount} API calls made");

                    foreach (var connection in connections)
                    {
                        Console.WriteLine($"Comparing {connection.Key.Name} to Nettspend");
                            if (connection.Key.Name == "Nettspend")
                        {
                            return;
                        }
                        if (!visited.Contains(connection.Key))
                        {
                            Queue.Enqueue(connection.Key, connection.Value);
                            visited.Add(connection.Key);
                        }
                    }
                }
            }
        }

        private async Task RefreshAccessToken()
        {
            if (DateTime.UtcNow >= _tokenExpiryTime)
            {
                try
                {
                    var tokenData = await _spotifyClientCredentials.GetAccessTokenAsync();
                    _accessToken = tokenData.AccessToken;
                    _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Access token refreshed successfully.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing access token: {ex.Message}");
                }
            }
        }

        private Dictionary<ArtistNode, double> GetConnections(ArtistNode startingArtistNode)
        {
            RefreshAccessToken().Wait();
            Dictionary<ArtistNode, double> similarArtistDictionary = new Dictionary<ArtistNode, double>();

            using (HttpClient client = new HttpClient())
            {
                string url = "http://ws.audioscrobbler.com/2.0/";
                var parameters = new Dictionary<string, string>
                {
                    { "method", "artist.getSimilar" },
                    { "artist", startingArtistNode.Name },
                    { "api_key", _apiKey },
                    { "format", "json" },
                    { "limit", "20"}
                };

                var response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrWhiteSpace(responseContent)) return similarArtistDictionary;

                    _databaseManager.UpdateLastExpanded(startingArtistNode.Name, DateTime.UtcNow.ToString());
                    JsonDocument document = JsonDocument.Parse(responseContent);

                    if (!IsDataValid(document)) return similarArtistDictionary;

                    foreach (var artistData in document.RootElement.GetProperty("similarartists").GetProperty("artist").EnumerateArray())
                    {
                        if (!DoesArtistHaveNameAndMatch(artistData)) continue;

                        string foundArtist = artistData.GetProperty("name").GetString();
                        double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                        if (foundArtist.Contains('&')) continue;

                        GetSpotifyDetails spotifyDetails = new GetSpotifyDetails(foundArtist, _accessToken);
                        string? spotifyId = spotifyDetails.SpotifyId;
                        int? popularity = spotifyDetails.Popularity;

                        if (spotifyId == null || popularity == null || (_databaseManager.DoesIdExist(spotifyId) && _databaseManager.DoesConnectionExist(startingArtistNode.Name, foundArtist)) || popularity < 40)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            switch (true)
                            {
                                case var _ when spotifyId == null:
                                    Console.WriteLine($"Skipping {foundArtist} due to missing Spotify ID");
                                    break;

                                case var _ when popularity == null:
                                    Console.WriteLine($"Skipping {foundArtist} due to missing popularity data");
                                    break;

                                case var _ when _databaseManager.DoesIdExist(spotifyId) && _databaseManager.DoesConnectionExist(startingArtistNode.Name, foundArtist):
                                    Console.WriteLine($"Skipping {foundArtist} as the Spotify ID already exists in the database and the connection already exists");
                                    break;

                                case var _ when popularity < 40:
                                    Console.WriteLine($"Skipping {foundArtist} due to low popularity ({popularity})");
                                    break;
                                default:
                                    Console.WriteLine($"Skipping {foundArtist}");
                                    break;
                            }
                            Console.WriteLine($"Total calls: {_totalCalls++}");
                            Console.WriteLine($"Wasted calls: {_wastedCalls++}");
                            Console.ResetColor();
                            continue;
                        }

                        if (_databaseManager.DoesIdExist(spotifyId))
                        {
                            _databaseManager.AddConnection(startingArtistNode.Name, foundArtist, match);
                            _wastedCalls++;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Adding new connection between {startingArtistNode.Name} and {foundArtist} as the Spotify ID exists in database");
                            Console.WriteLine($"Total calls: {_totalCalls++}");
                            Console.ResetColor();
                            continue;
                        }
                        
                        _databaseManager.AddArtist(foundArtist, spotifyId);
                        _databaseManager.AddConnection(startingArtistNode.Name, foundArtist, match);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Added {foundArtist} to the database and added a new connection between {startingArtistNode.Name} and {foundArtist}");
                        Console.WriteLine($"Total calls: {_totalCalls++}");
                        Console.ResetColor();
                        similarArtistDictionary.Add(new ArtistNode(foundArtist, spotifyId), 1 - match);
                    }
                }
            }

            return similarArtistDictionary;
        }

        private static bool RateLimitHandler(HttpResponseMessage response)
        {
            Random random = new Random();
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                int randomTime = random.Next(60000, 200000);
                
                Console.WriteLine($"Rate limit exceeded. Waiting {randomTime/1000} seconds before retrying...");
                Console.ResetColor();
                Task.Delay(randomTime).Wait();
                return false;
            }

            return true;

        }

        private static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            var paramList = new List<string>();
            foreach (var param in parameters)
            {
                paramList.Add($"{param.Key}={param.Value}");
            }
            return $"{url}?{string.Join("&", paramList)}";
        }

        private bool IsDataValid(JsonDocument document)
        {
            return document.RootElement.TryGetProperty("similarartists", out var similarArtistsElement) &&
                   similarArtistsElement.TryGetProperty("artist", out var _);
        }

        private bool DoesArtistHaveNameAndMatch(JsonElement artistData)
        {
            return artistData.TryGetProperty("name", out _) && artistData.TryGetProperty("match", out _);
        }

        public class GetSpotifyDetails
        {
            public string? SpotifyId;
            public int? Popularity;
            private readonly string _accessToken;

            public GetSpotifyDetails(string artistName, string accessToken)
            {
                _accessToken = accessToken;
                GetDetails(artistName);
            }

            private void GetDetails(string artistName)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    string url = $"https://api.spotify.com/v1/search?q=artist:\"{artistName}\"&type=artist&limit=1";

                    var response = client.GetAsync(url).Result;
                    while (RateLimitHandler(response) == false)
                    {
                        response = client.GetAsync(url).Result;
                        
                    }
                    Console.WriteLine(response.StatusCode);
                    if (response.IsSuccessStatusCode)
                    {
                        JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                        var artistData = document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray().FirstOrDefault();

                        if (!artistData.Equals(default(JsonElement)))
                        {
                            SpotifyId = artistData.GetProperty("id").GetString();
                            Popularity = artistData.GetProperty("popularity").GetInt32();
                        }
                    }
                }
            }
        }
    }
}
