using GlobalTypes;

namespace webserver;

public interface IArtistNetworkDatabaseService
{
    Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork();
}