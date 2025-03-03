using Microsoft.Data.Sqlite;

namespace NettspendToSautiSol;


public class ArtistNetworkDatabaseService
{
    private readonly IDatabaseRepository _databaseRepository;
    
    public ArtistNetworkDatabaseService(IDatabaseRepository databaseRepository)
    {
        _databaseRepository = databaseRepository;
        databaseRepository.InitialiseDatabase();
    }
    
    public Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork()
    {
        return _databaseRepository.GetNetwork();
    }

}