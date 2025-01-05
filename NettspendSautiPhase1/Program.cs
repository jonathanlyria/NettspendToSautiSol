using System;
using System.Collections.Generic;
using System.Linq;

namespace NettspendSautiPhase1
{
    class Program
    {
        private static void Main(string[] args)
        {
            string databasePath = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendSautiPhase1/database.db";
            DatabaseManager database = new DatabaseManager(databasePath);
            // InitializeDatabase(database);
            database.PrintDatabaseToTerminal();

            var travellerPath = TestTraveller(database);
            string firstArtist = travellerPath.First().Name;
            string lastArtist = travellerPath.Last().Name;

            PlaylistCreator testPlaylist = new PlaylistCreator(travellerPath, 2, true, firstArtist, lastArtist);
         

        }
        
        private static void InitializeDatabase(DatabaseManager database)
        {
            string apiKey = "00751a650c0182344603b9252c66d416";
            
            ArtistExpander expander = new ArtistExpander(apiKey, database);
            
            ArtistNode starter = new ArtistNode("Drake");
            database.AddArtist("Drake");
            
            expander.Expand(starter, 8);
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