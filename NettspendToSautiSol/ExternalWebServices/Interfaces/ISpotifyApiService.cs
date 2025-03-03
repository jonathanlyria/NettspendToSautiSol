namespace NettspendToSautiSol;

public interface ISpotifyApiService
{
    Task<KeyValuePair<(string, string), int>> GetArtistDetails(string artistName);
    Task<Dictionary<string, DateTime>> GetTopTracks(string spotifyId);
}