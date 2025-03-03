namespace NettspendToSautiSol;

public interface ILastFmApiService
{
    Task<Dictionary<string, double>>? GetSimilarArtists(string artist);
    Task<List<string>> GetTopTracks(string artist);
}