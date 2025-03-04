namespace NettspendToSautiSol;

public interface IArtistNetworkDatabaseService
{
    Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork();
}