using System;
using System.Linq;

namespace NettspendToSautiSol
{
    class Program
    {
        private static void Main(string[] args)
        {
            string apiKey = "00751a650c0182344603b9252c66d416";
            /*
            string databasePath20 = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendToSautiSol/database20.db";
            DatabaseManager database20 = new DatabaseManager(databasePath20);
            ArtistExpander expander20 = new ArtistExpander(apiKey, database20, 20);
            string debug20 = expander20.Expand();


            string databasePath15 = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendToSautiSol/database15.db";
            DatabaseManager database15 = new DatabaseManager(databasePath15);
            ArtistExpander expander15 = new ArtistExpander(apiKey, database15, 15);
            string debug15 = expander15.Expand();

            string databasePath10 = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendToSautiSol/database10.db";
            DatabaseManager database10 = new DatabaseManager(databasePath10);
            ArtistExpander expander10 = new ArtistExpander(apiKey, database10, 10);
            string debug10 = expander10.Expand();
            
            string databasePath5 = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendToSautiSol/database5.db";
            DatabaseManager database5 = new DatabaseManager(databasePath5);
            ArtistExpander expander5 = new ArtistExpander(apiKey, database5, 5);
            string debug5 = expander5.Expand();*/
            /*
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(debug20);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(debug15);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(debug10);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(debug5);
            Console.ResetColor();*/
            
            string databasePath3 = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendToSautiSol/NettspendToSautiSol.db";
            DatabaseManager database = new DatabaseManager(databasePath3); 
            ArtistExpander expander = new ArtistExpander(apiKey, database);
            expander.Expand();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(expander.DebugLine);
            Console.ResetColor();





        }
        
        private static void InitializeDatabase(DatabaseManager database)
        {
            string apiKey = "00751a650c0182344603b9252c66d416";
            
            ArtistExpander expander = new ArtistExpander(apiKey, database);
            
           // ArtistNode starter = new ArtistNode("Drake");
           // database.AddArtist("Drake");
            
            expander.Expand();
            Console.WriteLine(DateTime.UtcNow.ToString());
            database.PrintDatabaseToTerminal();

        }

        private static List<ArtistNode> TestTraveller(DatabaseManager database)
        {
            ArtistNetwork artistNetwork = new ArtistNetwork(database);
            artistNetwork.PrintMatrix();
            
            Console.WriteLine("Enter artist 1: ");
            string artist1 = Console.ReadLine();
            ArtistNode artist1Node = new ArtistNode(artist1);
            Console.WriteLine("Enter artist 2: ");
            string artist2 = Console.ReadLine();
            ArtistNode artist2Node = new ArtistNode(artist2);
            
            ArtistTraveller traveller = new ArtistTraveller(artist1Node, artist2Node, artistNetwork);
            traveller.Traverse();
            traveller.PrintPath();

            return traveller.Path;
            
            
        }
    }
}