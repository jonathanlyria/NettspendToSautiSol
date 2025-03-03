
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath = args[0];
            string lastFmApiKey = args[1];
            string spotifyClientId = args[2];
            string spotifyClientSecret = args[3];
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"database path: {databasePath} last fm {lastFmApiKey} spotfy {spotifyClientId} || {spotifyClientSecret}");
            Console.ResetColor();
            
            HttpClient client = new HttpClient();
            
            DatabaseRepository databaseRepository = new DatabaseRepository(databasePath);
            ArtistRepository artistRepository = new ArtistRepository(databasePath);
            ConnectionRepository connectionRepository = new ConnectionRepository(databasePath);
            
            SpotifyClientCredentialAuthorizer spotifyClientCredentialAuthorizer = new SpotifyClientCredentialAuthorizer(spotifyClientId, spotifyClientSecret, client);
            
            LastFmApiService lastFmApiService = new LastFmApiService(lastFmApiKey, client);
            SpotifyApiService spotifyApiService = new SpotifyApiService(spotifyClientCredentialAuthorizer, client);
            ArtistVerificationService artistVerificationService= new ArtistVerificationService();
            
            NetworkExpanderDatabaseService artistNetworkDatabaseService = new NetworkExpanderDatabaseService(artistRepository, connectionRepository, databaseRepository);
            ArtistNetworkExpander artistExpander = new ArtistNetworkExpander(artistNetworkDatabaseService, lastFmApiService, spotifyApiService, artistVerificationService);
            
            await artistExpander.SearchForArtists();
            
        }
    }
}
