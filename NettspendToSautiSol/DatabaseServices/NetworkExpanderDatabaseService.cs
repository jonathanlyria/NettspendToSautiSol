namespace NettspendToSautiSol;

public class NetworkExpanderDatabaseService: INetworkExpanderDatabaseService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IDatabaseRepository _databaseRepository;

    public NetworkExpanderDatabaseService(IArtistRepository artistRepository, IConnectionRepository connectionRepository, IDatabaseRepository databaseRepository)
    {
        _databaseRepository = databaseRepository;
        _artistRepository = artistRepository;
        _connectionRepository = connectionRepository;
        
        _databaseRepository.InitialiseDatabase();
    }
    
    public Queue<ArtistNode> GetSearchQueue()
    {
        return _artistRepository.GetSearchQueue();
    }
    
    public void AddArtistAndConnectionToDb(ArtistNode artist1, ArtistNode artist2, double weight)
    {
        if (IsArtistInDbById(artist1.SpotifyId) && IsConnectionInDb(artist1.SpotifyId, artist2.SpotifyId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{artist2.Name} is in artistNetworkDb and connection exists for {artist1.Name} -> {artist2.Name}");
            Console.ResetColor();
        }
        else if (IsArtistInDbById(artist2.SpotifyId))
        {
            // Check if artist1 exists, if not, add it
            if (!IsArtistInDbById(artist1.SpotifyId))
            {
                AddArtistToDb(artist1);
            }
            try
            {
                AddConnectionToDb(artist1.SpotifyId, artist2.SpotifyId, weight);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Added connection between {artist1.Name} and {artist2.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        else
        {
            AddArtistToDb(artist2);
            AddConnectionToDb(artist1.SpotifyId, artist2.SpotifyId, weight);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Added {artist2.Name} to the artistNetworkDb and added a new connection between {artist1.Name} and {artist2.Name}");
            Console.ResetColor();
        }
    }
    
    public void AddArtistToDb(ArtistNode artistNode)
    {
        _artistRepository.AddArtistToDb(artistNode);
    }
    
    public void UpdateIsExpanded(string spotifyId)
    {
        _artistRepository.UpdateIsExpanded(spotifyId);
    }

    public bool IsArtistInDbById(string spotifyId)
    {
        return _artistRepository.IsArtistInDbById(spotifyId);
    }

    private bool IsConnectionInDb(string spotifyId1, string spotifyId2)
    {
        return _connectionRepository.IsConnectionInDb(spotifyId1, spotifyId2);
    }
    
    private void AddConnectionToDb(string spotifyId1, string spotifyId2, double weight)
    {
        _connectionRepository.AddConnectionToDb(spotifyId1, spotifyId2, weight);
    }

    
}