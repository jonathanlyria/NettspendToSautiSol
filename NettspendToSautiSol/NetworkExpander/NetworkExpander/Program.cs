using DatabaseServices;
using ExternalWebServices;
using GlobalTypes;

namespace expander
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
            Console.WriteLine($"database path: {databasePath} last fm {lastFmApiKey} spotify {spotifyClientId} || {spotifyClientSecret}");
            Console.ResetColor();
            
            HttpClient client = new HttpClient();
            
            DatabaseRepository databaseRepository = new DatabaseRepository(databasePath);
            ArtistRepository artistRepository = new ArtistRepository(databasePath);
            ConnectionRepository connectionRepository = new ConnectionRepository(databasePath);
            
            SpotifyClientCredentialAuthorizer spotifyClientCredentialAuthorizer = new SpotifyClientCredentialAuthorizer(spotifyClientId, spotifyClientSecret, client);
            
            LastFmApiService lastFmApiService = new LastFmApiService(lastFmApiKey, client);
            SpotifyExpanderService spotifyExpanderService = new SpotifyExpanderService(spotifyClientCredentialAuthorizer, client);
            ArtistVerificationService artistVerificationService= new ArtistVerificationService();
            
            NetworkExpanderDatabaseService artistNetworkDatabaseService = new NetworkExpanderDatabaseService(artistRepository, connectionRepository, databaseRepository);
            ArtistNetworkExpander artistExpander = new ArtistNetworkExpander(artistNetworkDatabaseService, lastFmApiService, spotifyExpanderService, artistVerificationService);
            
            ArtistNode startingArtistNode = new ArtistNode("Drake", "3TVXtAsR1Inumwj472S9r4");

            await artistExpander.SearchForArtists(startingArtistNode);
            
        }
    }
}
