using Microsoft.Data.Sqlite;
namespace NettspendToSautiSol;

public class WebServerDatabaseService : IWebServerDatabaseService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IDatabaseRepository _databaseRepository;

    public WebServerDatabaseService(IArtistRepository artistRepository, IDatabaseRepository databaseRepository)
    {
        _artistRepository = artistRepository;
        _databaseRepository = databaseRepository;
        _databaseRepository.InitialiseDatabase();
    }

    public string GetIdFromName(string name)
    {
        return _artistRepository.GetIdFromName(name);
    }

    public string GetNameFromId(string spotifyId)
    {
        return _artistRepository.GetNameFromId(spotifyId);
    }

    public bool IsArtistInDbByName(string name)
    {
        return _artistRepository.IsArtistInDbByName(name);
    }

    public List<ArtistNode> GetAllArtistNodes()
    {
        return _artistRepository.GetAllArtistNodes();
    }

}