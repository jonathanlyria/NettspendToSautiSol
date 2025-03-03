using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

// Citation of Cors policy 
// Citation of adding singletons 
// Citation of Asp.Net core
namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath = args[0];
            string spotifyClientId = args[1];
            string spotifyClientSecret = args[2];
            string redirectUri = args[3];
            
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            // Add HttpClient as a singleton
            builder.Services.AddSingleton<HttpClient>();

            // Create repositories that will be injected into WebServerDatabaseService
            ArtistRepository artistRepository = new ArtistRepository(databasePath);
            DatabaseRepository databaseRepository = new DatabaseRepository(databasePath);
            ArtistNetworkDatabaseService artistNetworkDatabaseService = new ArtistNetworkDatabaseService(databaseRepository);
            
            // Register WebServerDatabaseService with repositories
            builder.Services.AddSingleton<IWebServerDatabaseService>(sp => 
                new WebServerDatabaseService(artistRepository, databaseRepository));
            
            // Register Spotify authorizers
            builder.Services.AddSingleton<ISpotifyClientCredentialAuthorizer>(sp => 
                new SpotifyClientCredentialAuthorizer(
                    spotifyClientId, 
                    spotifyClientSecret, 
                    sp.GetRequiredService<HttpClient>()));
                    
            builder.Services.AddSingleton<ISpotifyPkceCodeAuthorizer>(sp => 
                new SpotifyPkceCodeAuthorizer(
                    spotifyClientId, 
                    redirectUri));
            
            // Register playlist services
            builder.Services.AddSingleton<IGetPlaylistSongsService, GetPlaylistSongsService>();
            builder.Services.AddSingleton<ICreatePlaylistService, CreatePlaylistService>();
            
            // Register artist network with DatabaseManager dependency
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

            // Initialize the artist network
            IArtistNetwork artistNetwork = app.Services.GetRequiredService<IArtistNetwork>();
            Console.WriteLine("Waiting for the artist network to finish loading...");
            await Task.Run(() => artistNetwork.DisplayAllConnections()); 
            Console.WriteLine("Artist network loaded!");

            // Configure the HTTP request pipeline
            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");
            app.MapControllers();

            await app.RunAsync();
        }
    }
}