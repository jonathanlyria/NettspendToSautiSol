
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var databasePath =
               @"/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/expander/expander/database.db";
            DatabaseManager databaseManager = new(databasePath);
            ArtistExpander artistExpander = new(databaseManager);
            artistExpander.Expand();
        }
    }
}
