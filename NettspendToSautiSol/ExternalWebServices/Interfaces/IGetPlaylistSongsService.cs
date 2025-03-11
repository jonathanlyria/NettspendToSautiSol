using GlobalTypes;

namespace ExternalWebServices.Interfaces;

public interface IGetPlaylistSongsService
{
    Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists);
}