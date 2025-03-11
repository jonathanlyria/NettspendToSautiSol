namespace DatabaseServices.Interfaces;

public interface IConnectionRepository 
{
    bool IsConnectionInDb(string spotifyId1, string spotifyId2);
    void AddConnectionToDb(string spotifyId1, string spotifyId2, double weight);
}