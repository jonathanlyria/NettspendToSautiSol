using System;
using System.Collections.Generic;
using System.Linq;

namespace NettspendSautiPhase1
{
    class Program
    {
        private static void Main(string[] args)
        { 
            string databasePath = "/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendSautiPhase1";
            string databaseDirectory = Path.GetDirectoryName(databasePath);
            if (!Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
                Console.WriteLine($"Directory created: {databaseDirectory}");
            }
            DatabaseManager database = new DatabaseManager("/Users/jonathan/RiderProjects/NettspendSautiPhase1/NettspendSautiPhase1");

            /*string apiKey = "00751a650c0182344603b9252c66d416";
            
            ArtistExpander expander = new ArtistExpander(apiKey, database);
            
            ArtistNode starter = new ArtistNode("9fff2f8a-21e6-47de-a2b8-7f449929d43f","Drake");
            
            expander.Expand(starter, 4);

            database.PrintDatabaseToTerminal();*/


        }
    }
}