
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var databasePath =
               @"/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/NetworkExpander/NetworkExpander/database.db";
            DatabaseManager databaseManager = new DatabaseManager(databasePath);
            NetworkExpander artistExpander = new NetworkExpander(databaseManager);
            artistExpander.SearchForArtists();
        }
    }
}
