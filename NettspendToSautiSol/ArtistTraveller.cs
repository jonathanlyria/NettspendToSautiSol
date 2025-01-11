namespace NettspendToSautiSol
{
    public class ArtistTraveller : Traveller
    {
        public ArtistTraveller(ArtistNode startArtistNode, ArtistNode endArtistNode, ArtistNetwork artistNetwork)
            : base(startArtistNode, endArtistNode, artistNetwork)
        {
        }

        public void PrintPath()
        {
            if (Path.Count == 0)
            {
                Console.WriteLine("No path found between the specified artists.");
                return;
            }

            Console.WriteLine("Shortest Path between artists:");
            foreach (var artist in Path)
            {
                Console.Write($"{artist.Name} -> ");
            }
            Console.WriteLine($"End (Total Cost: {Cost})");
        }
    }
}