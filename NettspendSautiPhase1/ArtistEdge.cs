namespace NettspendSautiPhase1
{
    public class ArtistEdge : Edge
    {
        public ArtistEdge(ArtistNode node1, ArtistNode node2, double weight, bool isPlaceholder = false)
            : base(node1, node2, weight, isPlaceholder)
        {
        }
    }
}