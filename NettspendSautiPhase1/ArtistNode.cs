namespace NettspendSautiPhase1
{
    public class ArtistNode : Node
    {
        public string ArtistID { get; set; } // Unique ID for the artist
        public string Name { get; set; } // Artist name

        public ArtistNode(string artistId, string name)
            : base(artistId) // Use ArtistID as the base identifier
        {
            ArtistID = artistId;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj is ArtistNode other)
            {
                return ArtistID == other.ArtistID; // Compare by unique ID
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ArtistID.GetHashCode(); // Use ArtistID for hash code
        }
    }
}