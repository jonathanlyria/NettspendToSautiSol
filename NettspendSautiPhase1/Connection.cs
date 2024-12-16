namespace NettspendSautiPhase1;

public class Connection
{
    public Artist Artist1 { get; set; }
    public Artist Artist2 { get; set; }
    public double Weight { get; set; }
    public bool IsPlaceholder { get; set; } // Indicates if this connection is a placeholder

    public Connection(Artist artist1, Artist artist2, double weight, bool isPlaceholder = false)
    {
        Artist1 = artist1;
        Artist2 = artist2;
        Weight = weight;
        IsPlaceholder = isPlaceholder;
    }
}