using DatabaseServices;
using DatabaseServices.Interfaces;
using ExternalWebServices;
using ExternalWebServices.Interfaces;
using GlobalTypes;

// Citation of Cors policy 
// Citation of adding singletons 
// Citation of Asp.Net core
namespace webserver
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath = EnvironmentConfiguration.Require("ARTIST_DATABASE_PATH");
            string spotifyClientId = EnvironmentConfiguration.Require("SPOTIFY_CLIENT_ID");
            string spotifyClientSecret = EnvironmentConfiguration.Require("SPOTIFY_CLIENT_SECRET");
            string redirectUri = EnvironmentConfiguration.Require("SPOTIFY_REDIRECT_URI");

            if (!File.Exists(databasePath))
                throw new FileNotFoundException("The configured artist database does not exist.", databasePath);
            
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<HttpClient>();

            ArtistRepository artistRepository = new ArtistRepository(databasePath);
            DatabaseRepository databaseRepository = new DatabaseRepository(databasePath);
            ArtistNetworkDatabaseService artistNetworkDatabaseService = new ArtistNetworkDatabaseService(databaseRepository);
            
            builder.Services.AddSingleton<IWebServerDatabaseService>(sp => 
                new WebServerDatabaseService(artistRepository, databaseRepository));
            
            builder.Services.AddSingleton<ISpotifyClientCredentialAuthorizer>(sp => 
                new SpotifyClientCredentialAuthorizer(
                    spotifyClientId, 
                    spotifyClientSecret, 
                    sp.GetRequiredService<HttpClient>()));
                    
            builder.Services.AddSingleton<ISpotifyPkceCodeAuthorizer>(sp => 
                new SpotifyPkceCodeAuthorizer(
                    sp.GetRequiredService<HttpClient>(), spotifyClientId, 
                    redirectUri));
            
            builder.Services.AddSingleton<IGetPlaylistSongsService, GetPlaylistSongsService>();
            builder.Services.AddSingleton<ICreatePlaylistService, CreatePlaylistService>();
            
            builder.Services.AddSingleton<IArtistNetwork>(sp =>
            {
                var artistNetwork = new ArtistNetwork(artistNetworkDatabaseService);
                return artistNetwork;
            });

            builder.Services.AddControllers();
            
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("http://localhost", "http://127.0.0.1:8080")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            
            WebApplication app = builder.Build();

            Console.WriteLine("Starting the application...");

            IArtistNetwork artistNetwork = app.Services.GetRequiredService<IArtistNetwork>();
            Console.WriteLine("Waiting for the artist network to finish loading...");
            Console.WriteLine("Artist network loaded!");

            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
