using GlobalTypes;

namespace DatabaseServices.Interfaces;

public interface IArtistRepository 
{
    bool IsArtistInDbByName(string artistName); 
    bool IsArtistInDbById(string id);
    string GetIdFromName(string artistName);
    string GetNameFromId(string spotifyId);
    List<ArtistNode> GetAllArtistNodes();
    void AddArtistToDb(ArtistNode artist);
    void UpdateIsExpanded(string spotifyId);
    Queue<ArtistNode> GetSearchQueue();
}