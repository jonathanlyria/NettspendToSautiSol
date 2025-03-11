using GlobalTypes;

namespace DatabaseServices.Interfaces;

public interface IDatabaseRepository 
{
    Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork(); 
    
    void InitialiseDatabase();
}