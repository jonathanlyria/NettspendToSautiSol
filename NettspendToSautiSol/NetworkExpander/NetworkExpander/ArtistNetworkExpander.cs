using System.Net;
using System.Text.Json;
using DatabaseServices.Interfaces;
using ExternalWebServices.Interfaces;
using GlobalTypes;

namespace expander
{
    public class ArtistNetworkExpander
    {
        private readonly INetworkExpanderDatabaseService _networkExpanderDatabaseService;
        private readonly IArtistVerificationService _artistVerificationService;
        private readonly ISpotifyExpanderService _spotifyExpanderService;
        private readonly ILastFmApiService _lastFmApiService;
        
        public ArtistNetworkExpander(INetworkExpanderDatabaseService networkExpanderDatabaseService, 
            ILastFmApiService lastFmApiService, ISpotifyExpanderService spotifyExpanderService, IArtistVerificationService artistVerificationService)
        {
            _lastFmApiService = lastFmApiService;
            _spotifyExpanderService = spotifyExpanderService;
            _networkExpanderDatabaseService = networkExpanderDatabaseService;
            _artistVerificationService = artistVerificationService;
        }

        public async Task SearchForArtists(ArtistNode startingArtistNode)
        {
            Queue<ArtistNode> queue;
            try
            {
                queue = _networkExpanderDatabaseService.GetSearchQueue();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine(ex.Message);
                queue = new Queue<ArtistNode>();
                queue.Enqueue(startingArtistNode);
                _networkExpanderDatabaseService.AddArtistToDb(startingArtistNode);
                queue = _networkExpanderDatabaseService.GetSearchQueue();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }
            while (queue.Count > 0)            
            {
                int queueLength = queue.Count;
                for (int i = 0; i < queueLength; i++)
                {
                    Dictionary<ArtistNode, double> similarArtists = new Dictionary<ArtistNode, double>();
                    ArtistNode currentArtist = queue.Dequeue();

                    try
                    {
                        similarArtists = await FindSimilarArtists(currentArtist);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Timed out, Run again in 36-48 hrs");
                        break;
                    }
                    _networkExpanderDatabaseService.UpdateIsExpanded(currentArtist.SpotifyId);
                    
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
        
        private async Task<Dictionary<ArtistNode, double>> FindSimilarArtists(ArtistNode startingArtistNode)
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

                        KeyValuePair<(string, string), int> spotifyArtistDetails = await _spotifyExpanderService.GetArtistDetails(similarArtistName);

                        string spotifyId = spotifyArtistDetails.Key.Item1;
                        string name = spotifyArtistDetails.Key.Item2;
                        int popularity = spotifyArtistDetails.Value;

                        Dictionary<string, DateTime> spotifyTopTracks = await _spotifyExpanderService.GetTopTracks(spotifyId);

                        try
                        {
                            _artistVerificationService.VerifyArtist(spotifyTopTracks, lastFmTopTracks, popularity);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
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
                        throw new HttpRequestException(ex.Message);
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
                throw new HttpRequestException(ex.Message);
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
                Console.WriteLine($"Unexpected error in FindSimilarArtists: {ex.Message}");
                Console.ResetColor();
            }

            return similarArtists;
        }
    }
}
