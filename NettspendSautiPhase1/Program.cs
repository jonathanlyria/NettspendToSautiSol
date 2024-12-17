using System;
using System.Collections.Generic;
using System.Linq;

namespace NettspendSautiPhase1
{
    class Program
    {
        private static void Main(string[] args)
        {
            ArtistNetwork artistNetwork = new ArtistNetwork();

            string apiKey = "00751a650c0182344603b9252c66d416";
            
            ArtistExpander expander = new ArtistExpander(artistNetwork, apiKey);
            
            ArtistNode starter = new ArtistNode("Drake");
            
            expander.Expand(starter, 7);
            
            Console.WriteLine("Artists found in the network:");
            artistNetwork.PrintAdjacencyMatrix();

            while (true)
            {
                Console.WriteLine("Enter the name of the first artist:");
                string startArtistName = Console.ReadLine();
                Console.WriteLine("Enter the name of the second artist:");
                string endArtistName = Console.ReadLine();
            
                
                ArtistNode startArtistNode = artistNetwork.AdjacencyMatrix.Keys.FirstOrDefault(a => a.Name.Equals(startArtistName, StringComparison.OrdinalIgnoreCase));
                ArtistNode endArtistNode = artistNetwork.AdjacencyMatrix.Keys.FirstOrDefault(a => a.Name.Equals(endArtistName, StringComparison.OrdinalIgnoreCase));

                if (startArtistNode == null || endArtistNode == null)
                {
                    Console.WriteLine("One or both artists not found in the network.");
                    continue;
                }

                Traveller traveller = new Traveller(startArtistNode, endArtistNode, artistNetwork);
                traveller.Traverse();


                Console.WriteLine("Shortest path from " + startArtistNode.Name + " to " + endArtistNode.Name + ":");
                foreach (var artist in traveller.Path)
                {
                    Console.WriteLine(artist.Name);
                }
                Console.WriteLine($"Total similarity score: {traveller.Cost}");
            }
            
        }
    }
}