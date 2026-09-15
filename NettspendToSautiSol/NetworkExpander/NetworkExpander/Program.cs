using DatabaseServices;
using ExternalWebServices;
using GlobalTypes;

namespace expander
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath = EnvironmentConfiguration.Require("ARTIST_DATABASE_PATH");
            string lastFmApiKey = EnvironmentConfiguration.Require("LASTFM_API_KEY");
            string spotifyClientId = EnvironmentConfiguration.Require("SPOTIFY_CLIENT_ID");
            string spotifyClientSecret = EnvironmentConfiguration.Require("SPOTIFY_CLIENT_SECRET");
            
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
