namespace expander;

public class ArtistVerificationService : IArtistVerificationService
{

    public void VerifyArtist(Dictionary<string, DateTime> spotifyTopTracks, List<string> lastFmTopTracks,
        int popularity)
    {
        DateTime latestTopTrackDate = spotifyTopTracks.Values.Max();
        if (!CheckLastFmAndSpotifyTopTracksMatch(spotifyTopTracks.Keys.ToList(), lastFmTopTracks))
        {
            throw new Exception("The last fm and spotify top tracks do not match up");

        }
        if (!CheckArtistMeetsPopularityMinimum(popularity, latestTopTrackDate))
        {
            throw new Exception(
                $"The popularity is too low: Popularity:{popularity}, Age:{(int)(DateTime.UtcNow - latestTopTrackDate).TotalDays / 365}.");

        }
    }
    
    
    private bool CheckArtistMeetsPopularityMinimum(int popularity, DateTime latestTopTrackDate)
    {
        TimeSpan timeSinceRelease = DateTime.UtcNow - latestTopTrackDate;
        int yearsSinceRelease = (int)(timeSinceRelease.TotalDays / 365);
        
        int minPopularity = yearsSinceRelease switch
        {
            < 1 => 50, 
            <= 5 => 55,
            <= 10 => 60,
            _ => 65
        };
        return popularity >= minPopularity;
    }

    private bool CheckLastFmAndSpotifyTopTracksMatch(List<string> spotifyTopTracks, List<string> lastFmTopTracks)
    {
        if (spotifyTopTracks.Any(lastFmTopTracks.Contains))
        {
            return true;
        }

        return false;
    }
}