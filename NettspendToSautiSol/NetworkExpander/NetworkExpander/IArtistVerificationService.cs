namespace NettspendToSautiSol;

public interface IArtistVerificationService
{
    string VerifyArtist(Dictionary<string, DateTime> spotifyTopTracks, List<string> lastFmTopTracks,
        int popularity);
    
}