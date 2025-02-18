using NettspendToSautiSol;
public class ArtistConnection
{
    public ArtistNode Node1 { get; set; }
    public ArtistNode Node2 { get; set; }
    public double Weight { get; set; }
    

    public ArtistConnection(ArtistNode node1, ArtistNode node2, double weight)
    {
        Node1 = node1;
        Node2 = node2;
        Weight = weight;
    }
  
}