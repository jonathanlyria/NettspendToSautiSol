namespace NettspendToSautiSol
{
    // Citation on changing class equals
    public class ArtistNode 
    {
        public string Name;
        public string SpotifyId; 
        public ArtistNode(string name, string spotifyId) 
        {
            Name = name;
            SpotifyId = spotifyId;
        }

        public override bool Equals(object obj)
        {
            return obj is ArtistNode other && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
        {
            return Name.ToLowerInvariant().GetHashCode();
        }
    }
}