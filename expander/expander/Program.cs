
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var databasePath =
               @"C:\Users\jl154125\source\repos\jonathanlyria\NettspendToSautiSol\database.db";
            DatabaseManager databaseManager = new DatabaseManager(databasePath);
            ArtistExpander artistExpander = new(databaseManager);
        }
    }
}
