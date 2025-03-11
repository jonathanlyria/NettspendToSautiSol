namespace ExternalWebServices.Interfaces;

public interface ICreatePlaylistService
{
    Task<string> CreatePlaylist(List<string> songIds, string firstArtist, string lastArtist, string accessToken);

}