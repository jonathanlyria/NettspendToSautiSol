namespace NettspendToSautiSol;

public interface ISpotifyExpanderService
{
    Task<KeyValuePair<(string, string), int>> GetArtistDetails(string artistName);
    Task<Dictionary<string, DateTime>> GetTopTracks(string spotifyId);
}