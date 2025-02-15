using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// Citation of Last FM documentation 
// Citation of HTTP POST/GET
// Citation of HTTP Json parsing
// Citation of exponential backoff algorithms
// Citation of 
namespace NettspendToSautiSol
{
    public class ArtistExpander
    {
        private readonly DatabaseManager _databaseManager;
        private readonly SpotifyClientCredentials _spotifyClientCredentials;
        private string _accessToken;
        private DateTime _tokenExpiryTime;

        private string _apiKey; // API key
        private int _wastedCalls;
        private int _totalCalls;
        private int _numberOfArtists;
        public string DebugLine;
        private Queue<ArtistNode>? _queue;

        private static readonly HttpClient HttpClient = new HttpClient();

        public ArtistExpander(DatabaseManager databaseManager)
        {
            _spotifyClientCredentials = new SpotifyClientCredentials();
            _apiKey = "00751a650c0182344603b9252c66d416";
            _databaseManager = databaseManager;
            _wastedCalls = 0;
            _totalCalls = 0;

            _queue = new Queue<ArtistNode>();

            if (_databaseManager.GetExpanderQueue() == null)
            {
                _queue.Enqueue(new ArtistNode("Drake", "3TVXtAsR1Inumwj472S9r4"));
            }
            else
            {
                _queue = _databaseManager.GetExpanderQueue();
            }

            _accessToken = _spotifyClientCredentials.GetAccessTokenAsync().Result.AccessToken;
            _tokenExpiryTime =
                DateTime.UtcNow.AddSeconds(_spotifyClientCredentials.GetAccessTokenAsync().Result.ExpiresIn);
        }

        public void Expand()
        {
            HashSet<ArtistNode> visited = new HashSet<ArtistNode>();
            int callCount = 0;
            int level = 0;

            while (true)
            {
                level++;
                DebugLine =
                    $"\nLevel {level}\nQueue size: {_queue.Count}\nVisited nodes: {visited.Count}\nTotal calls: " +
                    $"{_totalCalls}\nCall Efficiency: {(_totalCalls - _wastedCalls) / (double)_totalCalls:P2}\nCalls to Artists Found: " +
                    $"{_totalCalls}:{visited.Count}";
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(DebugLine);
                Console.ResetColor();

                int queueLength = _queue.Count;
                for (int i = 0; i < queueLength; i++)
                {
                    ArtistNode currentNode = _queue.Dequeue();
                    _databaseManager.UpdateExpanded(currentNode.SpotifyId);
                    List<ArtistNode> similarArtists = GetSimilarArtists(currentNode);

                    callCount++;
                    Console.WriteLine($"{callCount} API calls made");

                    foreach (ArtistNode similarArtist in similarArtists)
                    {
                        if (!visited.Contains(similarArtist))
                        {
                            _queue.Enqueue(similarArtist);
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
                    (string AccessToken, int ExpiresIn) tokenData = await _spotifyClientCredentials.GetAccessTokenAsync();
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

        private List<ArtistNode> GetSimilarArtists(ArtistNode startingArtistNode)
        {
            RefreshAccessToken().Wait();
            List<ArtistNode> similarArtistDictionary = new List<ArtistNode>();

            // Clear any previous Authorization header to ensure LastFM API is called without it.
            HttpClient.DefaultRequestHeaders.Authorization = null;
            string url = "http://ws.audioscrobbler.com/2.0/";
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "method", "artist.getSimilar" },
                { "artist", startingArtistNode.Name },
                { "api_key", _apiKey },
                { "format", "json" },
                { "limit", "5"}
            };

            HttpResponseMessage response = HttpClient.GetAsync(BuildUrlWithParams(url, parameters)).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(responseContent)) return similarArtistDictionary;

                JsonDocument document = JsonDocument.Parse(responseContent);

                if (!IsDataValid(document)) return similarArtistDictionary;

                foreach (JsonElement artistData in document.RootElement.GetProperty("similarartists").GetProperty("artist").EnumerateArray())
                {
                    if (!DoesArtistHaveNameAndMatch(artistData)) continue;

                    string foundArtist = artistData.GetProperty("name").GetString();
                    double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                    if (foundArtist.Contains('&')) continue;

                    GetSpotifyDetails spotifyDetails = new GetSpotifyDetails(foundArtist, _accessToken);
                    string? spotifyId = spotifyDetails.SpotifyId;
                    string? name = spotifyDetails.Name;
                    int? popularity = spotifyDetails.Popularity;
                    string? date = spotifyDetails.LatestTopTrackReleaseDate;

                    if (spotifyId == null || popularity == null || name == null || (_databaseManager.DoesIdExist(spotifyId) && _databaseManager.DoesConnectionExist(startingArtistNode.Name, foundArtist)) || !spotifyDetails.MeetsPopularityRequirement())
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
                            case var _ when name == null:
                                Console.WriteLine($"Skipping {foundArtist} due to missing name");
                                break;
                            case var _ when !spotifyDetails.MeetsPopularityRequirement():
                                Console.WriteLine($"Skipping {foundArtist} due to low popularity for date ({popularity} {date})");
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
                        _databaseManager.AddConnection(startingArtistNode.Name, name, match);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Adding new connection between {startingArtistNode.Name} and {name} as the Spotify ID exists in database");
                        Console.WriteLine($"Total calls: {_totalCalls++}");
                        Console.ResetColor();
                        continue;
                    }
                    
                    _databaseManager.AddArtist(name, spotifyId);
                    _databaseManager.AddConnection(startingArtistNode.Name, name, match);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Added {name} to the database and added a new connection between {startingArtistNode.Name} and {name}");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.WriteLine($"Total artists: {_numberOfArtists++}");
                    Console.WriteLine($"Popularity: {popularity}");
                    Console.ResetColor();

                    similarArtistDictionary.Add(new ArtistNode(name, spotifyId));
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

                Console.WriteLine($"Rate limit exceeded. Waiting {randomTime / 1000} seconds before retrying...");
                Console.ResetColor();
                Task.Delay(randomTime).Wait();
                return false;
            }

            return true;

        }

        private static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            List<string> paramList = new List<string>();
            foreach (KeyValuePair<string, string> param in parameters)
            {
                paramList.Add($"{param.Key}={param.Value}");
            }
            return $"{url}?{string.Join("&", paramList)}";
        }

        private bool IsDataValid(JsonDocument document)
        {
            return document.RootElement.TryGetProperty("similarartists", out JsonElement similarArtistsElement) &&
                   similarArtistsElement.TryGetProperty("artist", out _);
        }

        private bool DoesArtistHaveNameAndMatch(JsonElement artistData)
        {
            return artistData.TryGetProperty("name", out _) && artistData.TryGetProperty("match", out _);
        }

        public class GetSpotifyDetails
        {
            public string? SpotifyId { get; private set; }
            public int? Popularity { get; private set; }
            public string? Name { get; private set; }
            public string? LatestTopTrackReleaseDate { get; private set; }
            private readonly string _accessToken;

            public GetSpotifyDetails(string artistName, string accessToken)
            {
                _accessToken = accessToken;
                GetArtistDetails(artistName);
                if (SpotifyId != null)
                {
                    GetLatestTopTrackReleaseDate();
                }
            }

            private void GetArtistDetails(string artistName)
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                string url = $"https://api.spotify.com/v1/search?q=artist:\"{artistName}\"&type=artist&limit=1";

                HttpResponseMessage response = HttpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                    JsonElement artistData = document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray().FirstOrDefault();

                    if (!artistData.Equals(default(JsonElement)))
                    {
                        SpotifyId = artistData.GetProperty("id").GetString();
                        Name = artistData.GetProperty("name").GetString();
                        Popularity = artistData.GetProperty("popularity").GetInt32();
                    }
                }
                HttpClient.DefaultRequestHeaders.Authorization = null;
            }

            private void GetLatestTopTrackReleaseDate()
            {
                if (SpotifyId == null) return;

                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                string url = $"https://api.spotify.com/v1/artists/{SpotifyId}/top-tracks?market=US";

                HttpResponseMessage response = HttpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                    var tracks = document.RootElement.GetProperty("tracks").EnumerateArray().Take(5);
            
                    LatestTopTrackReleaseDate = tracks
                        .Select(track => track.GetProperty("album").GetProperty("release_date").GetString())
                        .OrderByDescending(date => date)
                        .FirstOrDefault();
                }
                HttpClient.DefaultRequestHeaders.Authorization = null;
            }
            
            
            public bool MeetsPopularityRequirement()
            {
                if (LatestTopTrackReleaseDate == null || Popularity == null)
                    return false;

                // Validate that LatestTopTrackReleaseDate is in the expected format "yyyy-MM-dd"
                if (!Regex.IsMatch(LatestTopTrackReleaseDate, @"^\d{4}-\d{2}-\d{2}$"))
                {
                    return false; // Reject if the format is incorrect
                }
    
                DateTime latestDate = DateTime.Parse(LatestTopTrackReleaseDate);

                TimeSpan timeSinceRelease = DateTime.UtcNow - latestDate;
                int yearsSinceRelease = (int)(timeSinceRelease.TotalDays / 365);

                int minPopularity = yearsSinceRelease switch
                {
                    < 1 => 45, 
                    <= 5 => 50,
                    <= 10 => 60,
                    _ => 65
                };

                return Popularity >= minPopularity;
            }
        }

    }
}
