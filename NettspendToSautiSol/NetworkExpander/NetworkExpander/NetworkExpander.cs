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
    public class NetworkExpander
    {
        private readonly DatabaseManager _databaseManager;
        private readonly SpotifyClientCredentials _spotifyClientCredentials;
        private string _accessToken;
        private DateTime _tokenExpiryTime;

        private int _wastedCalls;
        private int _totalCalls;
        private int _numberOfArtists;
        private Queue<ArtistNode>? _queue;

        private static readonly HttpClient HttpClient = new HttpClient();

        public NetworkExpander(DatabaseManager databaseManager)
        {
            _spotifyClientCredentials = new SpotifyClientCredentials();
            _databaseManager = databaseManager;
            _wastedCalls = 0;
            _totalCalls = 0;
            _queue = new Queue<ArtistNode>();
            
            if (_databaseManager.GetExpanderQueueFromDb() == null)
            {
                ArtistNode startingArtistNode = new ArtistNode("Drake", "3TVXtAsR1Inumwj472S9r4");
                _queue.Enqueue(startingArtistNode);
                databaseManager.AddArtistToDb("Drake", "3TVXtAsR1Inumwj472S9r4");
            }
            else
            {
                _queue = _databaseManager.GetExpanderQueueFromDb();
            }

            _accessToken = _spotifyClientCredentials.GetAccessTokenAsync().Result.AccessToken;
            _tokenExpiryTime =
                DateTime.UtcNow.AddSeconds(_spotifyClientCredentials.GetAccessTokenAsync().Result.ExpiresIn);
        }

        public void Expand()
        {
            int callCount = 0;
            int level = 0;
            while (_queue.Count > 0)
            {
                level++;
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
                        _queue.Enqueue(similarArtist);
                    }
                }
            }
        }
        
        private List<ArtistNode> GetSimilarArtists(ArtistNode startingArtistNode)
        {
            RefreshAccessToken().Wait();
            List<ArtistNode> similarArtists = new List<ArtistNode>();
            HttpClient.DefaultRequestHeaders.Authorization = null;
            
            LastFmData startingArtistLastFmData = new LastFmData(startingArtistNode.Name);
            if (!startingArtistLastFmData.isLastFmDataValid)
            {
                Console.WriteLine($"{startingArtistNode.Name} last fm data is not valid, {startingArtistLastFmData.lastFmDataInvalidReason}");
                return similarArtists;
            }
            Dictionary<string, double> lastFmSimilarArtists = startingArtistLastFmData.GetSimilarArtists();
            
            foreach (var similarArtist in lastFmSimilarArtists)
            {
                string similarArtistName = similarArtist.Key;
                double similarArtistWeight = similarArtist.Value;
                
                LastFmData similarArtistLastFmData = new LastFmData(similarArtistName);
                List<string> lastFmTopTracks = similarArtistLastFmData.GetTopTracks();
                foreach (var topTrack in lastFmTopTracks)
                {
                    Console.WriteLine($"{similarArtistName} - {topTrack}");
                }
                
                if (!similarArtistLastFmData.isLastFmDataValid)
                {
                    Console.WriteLine($"{startingArtistNode.Name} last fm data is not valid, {startingArtistLastFmData.lastFmDataInvalidReason}");
                    continue;
                }
                SpotifyData spotifyData = new SpotifyData(similarArtistName, _accessToken, lastFmTopTracks);
                
                if (spotifyData.GetValidationError() != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{similarArtistName} {spotifyData.GetValidationError()}");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.WriteLine($"Wasted calls: {_wastedCalls++}");
                    Console.ResetColor();
                    continue;
                }
                
                string spotifyId = spotifyData.SpotifyId;
                string name = spotifyData.Name;
                int popularity = (int)spotifyData.Popularity;
                
                if (_databaseManager.IsArtistInDbById(spotifyId) && _databaseManager.DoesConnectionExistInDb(name, startingArtistNode.Name))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{name} is in database and connection exists for {name} -> {startingArtistNode.Name}");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.WriteLine($"Wasted calls: {_wastedCalls++}");
                    Console.ResetColor();
                    continue;
                }
            
                if (_databaseManager.IsArtistInDbById(spotifyId))
                {
                    _databaseManager.AddConnectionToDb(startingArtistNode.Name, name, similarArtistWeight);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Adding new connection between {startingArtistNode.Name} and {name} as the Spotify ID exists in database");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.ResetColor();
                    continue;
                }
            
                _databaseManager.AddArtistToDb(name, spotifyId);
                _databaseManager.AddConnectionToDb(startingArtistNode.Name, name, similarArtistWeight);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Added {name} to the database and added a new connection between {startingArtistNode.Name} and {name}");
                Console.WriteLine($"Total calls: {_totalCalls++}");
                Console.WriteLine($"Total artists: {_numberOfArtists++}");
                Console.WriteLine($"Popularity: {popularity}");
                Console.ResetColor();

                similarArtists.Add(new ArtistNode(name, spotifyId));
            }
            return similarArtists;
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
        
        public class SpotifyData
        {
            public string? SpotifyId { get; private set; }
            public int? Popularity { get; private set; }
            public string? Name { get; private set; }
            public string? LatestTopTrackReleaseDate { get; private set; }
            
            public bool _isLastFmTopTrackInTop5;
            private static HttpClient HttpClient = new HttpClient();

            
            public bool MeetsPopularityMinimum { get; private set; }
            
            private readonly string _accessToken;
            private List<string> _lastFmTopTracks;
            private IEnumerable<JsonElement> _topTracks;
            
            
            public SpotifyData(string artistName, string accessToken, List<string> lastFmTopTracks)
            {
                _accessToken = accessToken;
                _lastFmTopTracks = lastFmTopTracks;
                _isLastFmTopTrackInTop5 = false;
                
                GetArtistDetails(artistName);
                _topTracks = GetTop10Tracks();
                LatestTopTrackReleaseDate = GetLatestTopTrackReleaseDate();
                
                MeetsPopularityMinimum = CheckArtistMeetsPopularityMinimum();
                _isLastFmTopTrackInTop5 = CheckIfLastFmTopTrackInTop5();

            }

            private void GetArtistDetails(string artistName)
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                string url = $"https://api.spotify.com/v1/search?q=artist:\"{artistName}\"&type=artist&limit=1";

                HttpResponseMessage response = HttpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                    var artistData = document.RootElement
                        .GetProperty("artists")
                        .GetProperty("items")
                        .EnumerateArray()
                        .FirstOrDefault();

                    SpotifyId = artistData.GetProperty("id").GetString();
                    Name = artistData.GetProperty("name").GetString();
                    Popularity = artistData.GetProperty("popularity").GetInt32();
                }
                HttpClient.DefaultRequestHeaders.Authorization = null;

            }
            
            private IEnumerable<JsonElement> GetTop10Tracks()
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                string url = $"https://api.spotify.com/v1/artists/{SpotifyId}/top-tracks?market=US";

                HttpResponseMessage response = HttpClient.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                    _topTracks = document.RootElement.GetProperty("tracks").EnumerateArray().Take(10);
                }
                return _topTracks;
            }

            private string GetLatestTopTrackReleaseDate()
            {
                return _topTracks
                            .Select(track => track.GetProperty("album").GetProperty("release_date").GetString())
                            .OrderByDescending(date => date)
                            .FirstOrDefault();
            }

            private bool CheckIfLastFmTopTrackInTop5()
            {
                var topTrackNames = _topTracks.Select(track => track.GetProperty("name").GetString()).ToList();
                foreach (var lastFmTopTrack in _lastFmTopTracks)
                {
                    if (topTrackNames.Contains(lastFmTopTrack)) return true;
                }
                return false;
            }
            
            public bool CheckArtistMeetsPopularityMinimum()
            {
                if (LatestTopTrackReleaseDate == null || Popularity == null)
                    return false;

                if (!Regex.IsMatch(LatestTopTrackReleaseDate, @"^\d{4}-\d{2}-\d{2}$"))
                {
                    return false;
                }

                DateTime latestDate = DateTime.Parse(LatestTopTrackReleaseDate);

                TimeSpan timeSinceRelease = DateTime.UtcNow - latestDate;
                int yearsSinceRelease = (int)(timeSinceRelease.TotalDays / 365);

                int minPopularity = yearsSinceRelease switch
                {
                    < 1 => 50, 
                    <= 5 => 55,
                    <= 10 => 60,
                    _ => 65
                };

                return Popularity >= minPopularity;
            }
            public string? GetValidationError()
            {
                if (SpotifyId == null)
                    return $"has missing Spotify ID";
                if (Popularity == null)
                    return $"has missing popularity data";
                if (Name == null)
                    return $"has missing Spotify Name";
                if (!_isLastFmTopTrackInTop5)
                    return $"last fm tracks do not match spotify tracks";
                if (!MeetsPopularityMinimum)
                    return "artist does not meet popularity minimum";

                return null;
            }
        }
      public class LastFmData
        {
            private string LastFmApiKey = "00751a650c0182344603b9252c66d416";
            public bool isLastFmDataValid;
            public string lastFmDataInvalidReason;
            public string url = "http://ws.audioscrobbler.com/2.0/";
            public string ArtistName;
            private static HttpClient HttpClient = new HttpClient();


            public LastFmData(string artistName)
            {
                lastFmDataInvalidReason = "";
                isLastFmDataValid = true;
                ArtistName = artistName;
         

            }

            public Dictionary<string, double> GetSimilarArtists()
            {
                Dictionary<string, double> similarArtists = new Dictionary<string, double>();
                Dictionary<string, string> similarArtistParameters = new Dictionary<string, string>
                {
                    { "method", "artist.getSimilar" },
                    { "artist", ArtistName },
                    { "api_key", LastFmApiKey },
                    { "format", "json" },
                    { "limit", "5"}
                };
                HttpResponseMessage response = HttpClient.GetAsync(BuildUrlWithParams(url, similarArtistParameters)).Result;
                if (!response.IsSuccessStatusCode)
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = response.ReasonPhrase;
                    return similarArtists;
                }
                
                string responseContent = response.Content.ReadAsStringAsync().Result;
                
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = "Response content is empty";
                    return similarArtists;
                };
                
                JsonDocument document = JsonDocument.Parse(responseContent);
                if (!DoSimilarArtistsExist(document))
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = "artist has no similar artists";
                    return similarArtists;
                }

                foreach (JsonElement artistData in document.RootElement.GetProperty("similarartists")
                             .GetProperty("artist").EnumerateArray())
                {
                    if (!DoesLastFmArtistHaveNameAndMatch(artistData))
                    {
                        isLastFmDataValid = false;
                        lastFmDataInvalidReason = "artist does not have valid name and match";
                        continue;
                    }
                    
                    string foundArtist = artistData.GetProperty("name").GetString();
                    double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                    if (foundArtist.Contains('&')) continue;
                    similarArtists.Add(foundArtist, match);
                }

                return similarArtists;

            }

            public List<string> GetTopTracks()
            {
                var topTracks = new List<string>();
                var topTrackParameters = new Dictionary<string, string>
                {
                    { "method", "artist.getTopTracks" },
                    { "artist", ArtistName },
                    { "api_key", LastFmApiKey },
                    { "format", "json" },
                    { "limit", "10" }
                };

                HttpResponseMessage response = HttpClient.GetAsync(BuildUrlWithParams(url, topTrackParameters)).Result;
                if (!response.IsSuccessStatusCode)
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = response.ReasonPhrase;
                    return topTracks;
                }

                string responseContent = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = "Response content is empty";
                    return topTracks;
                }

                using JsonDocument document = JsonDocument.Parse(responseContent);
                if (!document.RootElement.TryGetProperty("toptracks", out JsonElement toptracks) ||
                    !toptracks.TryGetProperty("track", out JsonElement tracksArray))
                {
                    isLastFmDataValid = false;
                    lastFmDataInvalidReason = "Artist has no top tracks";
                    return topTracks;
                }

                topTracks = tracksArray.EnumerateArray()
                    .Select(track => track.GetProperty("name").GetString() ?? "Unknown Track")
                    .ToList();

                return topTracks;
            }

            
            
            
            private bool DoSimilarArtistsExist(JsonDocument document)
            {
                return document.RootElement.TryGetProperty("similarartists", out JsonElement similarArtistsElement) &&
                       similarArtistsElement.TryGetProperty("artist", out _);
            }
            private bool DoesLastFmArtistHaveNameAndMatch(JsonElement artistData)
            {
                return artistData.TryGetProperty("name", out _) && artistData.TryGetProperty("match", out _);
            }
           
            private string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
            {
                List<string> paramList = new List<string>();
                foreach (KeyValuePair<string, string> param in parameters)
                {
                    paramList.Add($"{param.Key}={param.Value}");
                }
                return $"{url}?{string.Join("&", paramList)}";
            }


            
        }
    }
}
