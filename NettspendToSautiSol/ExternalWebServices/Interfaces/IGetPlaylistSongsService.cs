namespace NettspendToSautiSol;

public interface IGetPlaylistSongsService
{
    Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists);
}