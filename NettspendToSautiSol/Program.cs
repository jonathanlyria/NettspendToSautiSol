using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var databasePath =
                "/Users/jonathan/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/NettspendToSautiSol.db";
            DatabaseManager database = new(databasePath);

            // Register services
            builder.Services.AddControllers();
            builder.Services.AddSingleton(new DatabaseManager(databasePath));
            builder.Services.AddSingleton<ArtistNetwork>(sp =>
            {
                var dbManager = sp.GetRequiredService<DatabaseManager>();
                var artistNetwork = new ArtistNetwork(dbManager);
                artistNetwork.LoadNetwork(); // Ensure loading happens synchronously here
                return artistNetwork;
            });

            var app = builder.Build();

            Console.WriteLine("Starting the application...");

            // Wait for the network to load before running the server
            var artistNetwork = app.Services.GetRequiredService<ArtistNetwork>();
            Console.WriteLine("Waiting for the artist network to finish loading...");
            await Task.Run(() => artistNetwork.LoadNetwork()); // LoadNetwork must be thread-safe if called again
            Console.WriteLine("Artist network loaded!");

            app.UseRouting();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}