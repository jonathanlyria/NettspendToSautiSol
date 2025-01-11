using NettspendToSautiSol;
public class ArtistEdge : Edge<ArtistNode>
{
    public ArtistEdge(ArtistNode node1, ArtistNode node2, double weight)
        : base(node1, node2, weight)
    {
    }
}