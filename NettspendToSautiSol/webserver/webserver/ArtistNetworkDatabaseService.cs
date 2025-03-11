using DatabaseServices.Interfaces;
using GlobalTypes;

namespace webserver;


public class ArtistNetworkDatabaseService : IArtistNetworkDatabaseService
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