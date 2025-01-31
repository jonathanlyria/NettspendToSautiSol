using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath =
               @"/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/expander/expander/database.db";
            
            var dbManager = new DatabaseManager(databasePath);
            // ArtistExpander artistExpander = new(databaseManager);
            
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddControllers();
            builder.Services.AddSingleton(new DatabaseManager(databasePath));
            builder.Services.AddSingleton<SpotifyAuthorizer>();

            builder.Services.AddSingleton<ArtistNetwork>(sp =>
            {
                DatabaseManager dbManager = sp.GetRequiredService<DatabaseManager>();
                ArtistNetwork artistNetwork = new ArtistNetwork(dbManager);
                artistNetwork.LoadNetwork(); 
                return artistNetwork;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policy =>
                {
                    policy.WithOrigins("http://localhost", "http://127.0.0.1")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            WebApplication app = builder.Build();

            Console.WriteLine("Starting the application...");

            ArtistNetwork artistNetwork = app.Services.GetRequiredService<ArtistNetwork>();
            Console.WriteLine("Waiting for the artist network to finish loading...");
            await Task.Run(() => artistNetwork.LoadNetwork()); 
            Console.WriteLine("Artist network loaded!");

            app.UseCors("AllowSpecificOrigin");

            app.UseRouting();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
