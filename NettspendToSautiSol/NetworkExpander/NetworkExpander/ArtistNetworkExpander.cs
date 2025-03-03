using System.Text.Json;

namespace NettspendToSautiSol
{
    public class ArtistNetworkExpander
    {
        private readonly INetworkExpanderDatabaseService _networkExpanderDatabaseService;
        private readonly IArtistVerificationService _artistVerificationService;
        private readonly ISpotifyApiService _spotifyApiService;
        private readonly ILastFmApiService _lastFmApiService;
        
        public ArtistNetworkExpander(INetworkExpanderDatabaseService networkExpanderDatabaseService, 
            ILastFmApiService lastFmApiService, ISpotifyApiService spotifyApiService, IArtistVerificationService artistVerificationService)
        {
            _lastFmApiService = lastFmApiService;
            _spotifyApiService = spotifyApiService;
            _networkExpanderDatabaseService = networkExpanderDatabaseService;
            _artistVerificationService = artistVerificationService;
        }

        public async Task SearchForArtists()
        {
            Queue<ArtistNode> queue; 
            try
            {
                queue = _networkExpanderDatabaseService.GetSearchQueue();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                queue = new Queue<ArtistNode>();
                ArtistNode startingArtistNode = new ArtistNode("Drake", "3TVXtAsR1Inumwj472S9r4");
                queue.Enqueue(startingArtistNode);
                _networkExpanderDatabaseService.AddArtistToDb(startingArtistNode);
                queue = _networkExpanderDatabaseService.GetSearchQueue();
            }
            int callCount = 0;
            while (callCount < 2000 && queue.Count > 0)            
            {
                int queueLength = queue.Count;
                for (int i = 0; i < queueLength; i++)
                {
                    ArtistNode currentArtist = queue.Dequeue();
                    _networkExpanderDatabaseService.UpdateIsExpanded(currentArtist.SpotifyId);
                    Dictionary<ArtistNode, double> similarArtists = await GetSimilarArtists(currentArtist);

                    callCount++;
                    Console.WriteLine($"{callCount} API calls made");

                    foreach (KeyValuePair<ArtistNode, double> similarArtist in similarArtists)
                    {
                        Console.WriteLine($"FOUND: {similarArtist.Key.SpotifyId} - {similarArtist.Key.Name}");
                        if (!_networkExpanderDatabaseService.IsArtistInDbById(similarArtist.Key.SpotifyId))
                        {
                            queue.Enqueue(similarArtist.Key);
                        }
                        _networkExpanderDatabaseService.AddArtistAndConnectionToDb(currentArtist, similarArtist.Key, similarArtist.Value);
                     
                    }
                }
            }
        }
        
        private async Task<Dictionary<ArtistNode, double>> GetSimilarArtists(ArtistNode startingArtistNode)
        {
            Dictionary<ArtistNode, double> similarArtists = new Dictionary<ArtistNode, double>();

            try
            { 
                Dictionary<string, double> lastFmSimilarArtists = await _lastFmApiService.GetSimilarArtists(startingArtistNode.Name);

                foreach (var similarArtist in lastFmSimilarArtists)
                {
                    string similarArtistName = similarArtist.Key;
                    double similarArtistWeight = similarArtist.Value;

                    try
                    {
                        List<string> lastFmTopTracks = await _lastFmApiService.GetTopTracks(similarArtistName);

                        KeyValuePair<(string, string), int> spotifyArtistDetails = await _spotifyApiService.GetArtistDetails(similarArtistName);

                        string spotifyId = spotifyArtistDetails.Key.Item1;
                        string name = spotifyArtistDetails.Key.Item2;
                        int popularity = spotifyArtistDetails.Value;

                        Dictionary<string, DateTime> spotifyTopTracks = await _spotifyApiService.GetTopTracks(spotifyId);

                        string artistVerificationStatus = _artistVerificationService.VerifyArtist(spotifyTopTracks, lastFmTopTracks, popularity);

                        if (artistVerificationStatus != "Valid Artist")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{similarArtistName} is invalid: {artistVerificationStatus}");
                            Console.ResetColor();
                            continue;
                        }

                        ArtistNode foundArtist = new ArtistNode(name, spotifyId);
                        similarArtists[foundArtist] = similarArtistWeight;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"HTTP request failed for artist {similarArtistName}: {ex.Message}");
                        Console.ResetColor();
                    }
                    catch (JsonException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"JSON parsing failed for artist {similarArtistName}: {ex.Message}");
                        Console.ResetColor();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Invalid operation for artist {similarArtistName}: {ex.Message}");
                        Console.ResetColor();
                    }
                    catch (FormatException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Invalid date format for artist {similarArtistName}: {ex.Message}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Unexpected error processing artist {similarArtistName}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to fetch similar artists from Last.fm: {ex.Message}");
                Console.ResetColor();
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to parse Last.fm API response: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unexpected error in GetSimilarArtists: {ex.Message}");
                Console.ResetColor();
            }

            return similarArtists;
        }
    }
}
