namespace NettspendToSautiSol;

public interface INetworkExpanderDatabaseService
{
    Queue<ArtistNode> GetSearchQueue();
    void UpdateIsExpanded(string spotifyId);
    void AddArtistAndConnectionToDb(ArtistNode artist1, ArtistNode artist2, double weight);
    void AddArtistToDb(ArtistNode artist);
    
    bool IsArtistInDbById(string spotifyId);
}