using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace NettspendToSautiSol;

public class ArtistVerificationService : IArtistVerificationService
{

    public string VerifyArtist(Dictionary<string, DateTime> spotifyTopTracks, List<string> lastFmTopTracks,
        int popularity)
    {
        string artistVerificationStatus = "Valid Artist";
        DateTime latestTopTrackDate = spotifyTopTracks.Values.Max();
        if (!CheckLastFmAndSpotifyTopTracksMatch(spotifyTopTracks.Keys.ToList(), lastFmTopTracks))
        {
            artistVerificationStatus = "The last fm and spotify top tracks do not match up";

        }
        else if (!CheckArtistMeetsPopularityMinimum(popularity, latestTopTrackDate))
        {
            artistVerificationStatus = $"The popularity is too low: Popularity:{popularity}, Age:{(int)(DateTime.UtcNow - latestTopTrackDate).TotalDays/365}.";

        }
        return artistVerificationStatus;
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