namespace expander;

public interface IArtistVerificationService
{
    void VerifyArtist(Dictionary<string, DateTime> spotifyTopTracks, List<string> lastFmTopTracks,
        int popularity);
    
}