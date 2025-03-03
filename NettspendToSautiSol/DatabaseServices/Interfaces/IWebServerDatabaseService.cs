namespace NettspendToSautiSol;

public interface IWebServerDatabaseService
{
    string GetIdFromName(string name);
    string GetNameFromId(string spotifyId);
    bool IsArtistInDbByName(string name);
    List<ArtistNode> GetAllArtistNodes();

}