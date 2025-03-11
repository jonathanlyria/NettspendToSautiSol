using GlobalTypes;

namespace webserver;

public interface IArtistNetwork
{
    List<ArtistNode> FindPathWithDijkstras(ArtistNode startArtistNode, ArtistNode endArtistNode);
    void DisplayAllConnections();
}