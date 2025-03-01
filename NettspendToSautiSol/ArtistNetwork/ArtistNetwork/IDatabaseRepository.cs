namespace NettspendToSautiSol;

public interface IDatabaseRepository
{
    Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork();
    
    void InitialiseDatabase(string databasePath);
}