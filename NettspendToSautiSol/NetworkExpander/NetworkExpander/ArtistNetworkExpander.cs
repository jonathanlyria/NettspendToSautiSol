namespace NettspendToSautiSol
{
    public class ArtistNetworkExpander
    {
        private readonly SpotifyClientCredentials _spotifyClientCredentials;
        private string _accessToken;
        private DateTime _tokenExpiryTime;
        private readonly NetworkExpanderDatabaseService _networkExpanderDatabaseService;

        private int _wastedCalls;
        private int _totalCalls;
        private int _numberOfArtists;
        private Queue<ArtistNode>? _queue;

        private static readonly HttpClient HttpClient = new HttpClient();

        public ArtistNetworkExpander(NetworkExpanderDatabaseService networkExpanderDatabaseService)
        {
            
            _spotifyClientCredentials = new SpotifyClientCredentials();
            _wastedCalls = 0;
            _totalCalls = 0;
            _queue = new Queue<ArtistNode>();
            _networkExpanderDatabaseService = networkExpanderDatabaseService;
            
            if (_networkExpanderDatabaseService.GetSearchQueue() == null)
            {
                ArtistNode startingArtistNode = new ArtistNode("Drake", "3TVXtAsR1Inumwj472S9r4");
                _queue.Enqueue(startingArtistNode);
                _networkExpanderDatabaseService.AddArtistToDb(startingArtistNode);
            }
            else
            {
                _queue = _networkExpanderDatabaseService.GetSearchQueue();
            }

            _accessToken = _spotifyClientCredentials.GetAccessTokenAsync().Result.AccessToken;
            _tokenExpiryTime =
                DateTime.UtcNow.AddSeconds(_spotifyClientCredentials.GetAccessTokenAsync().Result.ExpiresIn);
        }

        public void SearchForArtists()
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
                    _networkExpanderDatabaseService.UpdateIsExpanded(currentNode.SpotifyId);
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
            if (!startingArtistLastFmData.IsLastFmDataValid)
            {
                Console.WriteLine($"{startingArtistNode.Name} last fm data is not valid, {startingArtistLastFmData.LastFmDataInvalidReason}");
                return similarArtists;
            }
            Dictionary<string, double> lastFmSimilarArtists = startingArtistLastFmData.GetSimilarArtists();
            
            foreach (var similarArtist in lastFmSimilarArtists)
            {
                string similarArtistName = similarArtist.Key;
                double similarArtistWeight = similarArtist.Value;
                
                LastFmData similarArtistLastFmData = new LastFmData(similarArtistName);
                List<string> lastFmTopTracks = similarArtistLastFmData.GetTopTracks();
                if (!similarArtistLastFmData.IsLastFmDataValid)
                {
                    Console.WriteLine($"{startingArtistNode.Name} last fm data is not valid, {startingArtistLastFmData.LastFmDataInvalidReason}");
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
                
                if (_networkExpanderDatabaseService.IsArtistInDbById(spotifyId) && _networkExpanderDatabaseService.IsConnectionInDb(spotifyId, startingArtistNode.SpotifyId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{name} is in artistNetworkDb and connection exists for {name} -> {startingArtistNode.Name}");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.WriteLine($"Wasted calls: {_wastedCalls++}");
                    Console.ResetColor();
                    continue;
                }
            
                if (_networkExpanderDatabaseService.IsArtistInDbById(spotifyId))
                {
                    _networkExpanderDatabaseService.AddConnectionToDb(spotifyId, startingArtistNode.SpotifyId, similarArtistWeight);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Adding new connection between {startingArtistNode.Name} and {name} as the Spotify ID exists in artistNetworkDb");
                    Console.WriteLine($"Total calls: {_totalCalls++}");
                    Console.ResetColor();
                    continue;
                }
            
                _networkExpanderDatabaseService.AddArtistToDb(new ArtistNode(similarArtistName, spotifyId));
                _networkExpanderDatabaseService.AddConnectionToDb(spotifyId, startingArtistNode.SpotifyId, similarArtistWeight);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Added {name} to the artistNetworkDb and added a new connection between {startingArtistNode.Name} and {name}");
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
            if (DateTime.UtcNow >= _tokenExpiryTime + TimeSpan.FromMinutes(1))
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
        
    }
}
