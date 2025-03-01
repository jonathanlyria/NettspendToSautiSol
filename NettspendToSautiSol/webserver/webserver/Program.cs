using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
// Citation of Cors policy 
// Citation of addign signletons 
// Citation of Asp.Net core
namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath =
               @"/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/NetworkExpander/NetworkExpander/database.db";
            
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddControllers();
            builder.Services.AddSingleton<SpotifyAuthorizeWithPKCEAuthenticator>();

            builder.Services.AddSingleton<ArtistNetwork>(sp =>
            {
                ArtistNetwork artistNetwork = new ArtistNetwork();
                return artistNetwork;
            });

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

            ArtistNetwork artistNetwork = app.Services.GetRequiredService<ArtistNetwork>();
            Console.WriteLine("Waiting for the artist network to finish loading...");
            await Task.Run(() => artistNetwork.DisplayAllConnections()); 
            Console.WriteLine("Artist network loaded!");


            app.UseRouting();
            
            app.UseCors("AllowSpecificOrigin");

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
