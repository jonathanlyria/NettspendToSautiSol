
using System.IO;

namespace NettspendToSautiSol
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string databasePath = args[0];
            ArtistNetworkExpander artistExpander = new ArtistNetworkExpander(databasePath);
            artistExpander.SearchForArtists();
        }
    }
}
