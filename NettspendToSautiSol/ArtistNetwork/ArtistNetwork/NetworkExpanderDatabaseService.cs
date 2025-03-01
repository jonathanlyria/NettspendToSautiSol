namespace NettspendToSautiSol;

public class NetworkExpanderDatabaseService 
{
    private readonly IArtistRepository _artistRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IDatabaseRepository _databaseRepository;

    public NetworkExpanderDatabaseService(IArtistRepository artistRepository, IConnectionRepository connectionRepository, IDatabaseRepository databaseRepository, string databasePath)
    {
        _databaseRepository = databaseRepository;
        _artistRepository = artistRepository;
        _connectionRepository = connectionRepository;
        
        _databaseRepository.InitialiseDatabase(databasePath);
    }

    public Queue<ArtistNode>? GetSearchQueue()
    {
        return _artistRepository.GetSearchQueue();
    }

    public bool IsArtistInDbById(string spotifyId)
    {
        return _artistRepository.IsArtistInDbById(spotifyId);
    }

    public bool IsConnectionInDb(string spotifyId1, string spotifyId2)
    {
        return _connectionRepository.IsConnectionInDb(spotifyId1, spotifyId2);
    }

    public void AddArtistToDb(ArtistNode artistNode)
    {
        _artistRepository.AddArtistToDb(artistNode);
    }

    public void AddConnectionToDb(string spotifyId1, string spotifyId2, double weight)
    {
        _connectionRepository.AddConnectionToDb(spotifyId1, spotifyId2, weight);
    }

    public void UpdateIsExpanded(string spotifyId)
    {
        _artistRepository.UpdateIsExpanded(spotifyId);
    }
}