using System;
using System.Collections.Generic;
using System.Linq;

namespace NettspendSautiPhase1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a network of artists
            NetworkOfArtists network = new NetworkOfArtists();

            // Create an Expander with the network
            Expander expander = new Expander(network);

            // Create a Drake artist to start the expansion
            Artist drake = new Artist("Drake");

            // Expand the network 3 times starting from Drake
            expander.Expand(drake, 6);

            // Display all the artists found in the network
            Console.WriteLine("Artists found in the network:");
            network.PrintAdjacencyMatrix();

            // Prompt user for the two artists they want to find the shortest path between
            Console.WriteLine("Enter the name of the first artist:");
            string startArtistName = Console.ReadLine();
            Console.WriteLine("Enter the name of the second artist:");
            string endArtistName = Console.ReadLine();

            // Find the artists in the network
            Artist startArtist = network.AdjacencyMatrix.Keys.FirstOrDefault(a => a.Name.Equals(startArtistName, StringComparison.OrdinalIgnoreCase));
            Artist endArtist = network.AdjacencyMatrix.Keys.FirstOrDefault(a => a.Name.Equals(endArtistName, StringComparison.OrdinalIgnoreCase));

            if (startArtist == null || endArtist == null)
            {
                Console.WriteLine("One or both artists not found in the network.");
                return;
            }

            // Create a Traveller to find the shortest path between the two artists
            Traveller traveller = new Traveller(startArtist, endArtist, network);
            traveller.Traverse();

            // Output the shortest path and the total similarity score
            Console.WriteLine("Shortest path from " + startArtist.Name + " to " + endArtist.Name + ":");
            foreach (var artist in traveller.Path)
            {
                Console.WriteLine(artist.Name);
            }
            Console.WriteLine($"Total similarity score: {traveller.Cost}");
        }
    }
}