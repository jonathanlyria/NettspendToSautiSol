using Microsoft.Data.Sqlite;

namespace NettspendToSautiSol;


public class ArtistNetworkDatabaseService
{
    private readonly IDatabaseRepository _databaseRepository;
    
    public ArtistNetworkDatabaseService(IDatabaseRepository databaseRepository, string databasePath)
    {
        _databaseRepository = databaseRepository;
        databaseRepository.InitialiseDatabase(databasePath);
    }
    
    public Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork()
    {
        return _databaseRepository.GetNetwork();
    }

}