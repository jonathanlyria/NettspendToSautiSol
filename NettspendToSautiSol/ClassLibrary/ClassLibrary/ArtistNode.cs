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
            if (obj is ArtistNode otherNode)
            {
                return SpotifyId == otherNode.SpotifyId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return SpotifyId != null ? SpotifyId.GetHashCode() : 0;
        }
    }
}