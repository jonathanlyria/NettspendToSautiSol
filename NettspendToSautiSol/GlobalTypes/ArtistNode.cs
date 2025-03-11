namespace GlobalTypes
{
    public class ArtistNode(string name, string spotifyId)
    {
        public readonly string Name = name;
        public readonly string SpotifyId = spotifyId;

        public override bool Equals(object? obj) // needed so it can act 
        {
            if (obj is ArtistNode otherNode)
            {
                return SpotifyId == otherNode.SpotifyId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return SpotifyId.GetHashCode();
        }
    }
}