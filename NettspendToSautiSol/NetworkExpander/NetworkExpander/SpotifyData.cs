namespace NettspendToSautiSol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
public class SpotifyData
{
    public string? SpotifyId { get; private set; }
    public int? Popularity { get; private set; }
    public string? Name { get; private set; }
    public string? LatestTopTrackReleaseDate { get; private set; }
    
    public bool _isLastFmTopTrackInTop5;
    private static HttpClient HttpClient = new HttpClient();
    public bool MeetsPopularityMinimum { get; private set; }
    
    private readonly string _accessToken;
    private List<string> _lastFmTopTracks;
    private IEnumerable<JsonElement> _topTracks;
    
    
    public SpotifyData(string artistName, string accessToken, List<string> lastFmTopTracks)
    {
        _accessToken = accessToken;
        _lastFmTopTracks = lastFmTopTracks;
        _isLastFmTopTrackInTop5 = false;
        
        GetArtistDetails(artistName);
        _topTracks = GetTop10Tracks();
        LatestTopTrackReleaseDate = GetLatestTopTrackReleaseDate();
        
        MeetsPopularityMinimum = CheckArtistMeetsPopularityMinimum();
        _isLastFmTopTrackInTop5 = CheckIfLastFmTopTrackInTop5();

    }

    private void GetArtistDetails(string artistName)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        string url = $"https://api.spotify.com/v1/search?q=artist:\"{artistName}\"&type=artist&limit=1";

        HttpResponseMessage response = HttpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            var artistData = document.RootElement
                .GetProperty("artists")
                .GetProperty("items")
                .EnumerateArray()
                .FirstOrDefault();

            SpotifyId = artistData.GetProperty("id").GetString();
            Name = artistData.GetProperty("name").GetString();
            Popularity = artistData.GetProperty("popularity").GetInt32();
        }
        HttpClient.DefaultRequestHeaders.Authorization = null;

    }
    
    private IEnumerable<JsonElement> GetTop10Tracks()
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        string url = $"https://api.spotify.com/v1/artists/{SpotifyId}/top-tracks?market=US";

        HttpResponseMessage response = HttpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            _topTracks = document.RootElement.GetProperty("tracks").EnumerateArray().Take(10);
        }
        return _topTracks;
    }

    private string GetLatestTopTrackReleaseDate()
    {
        return _topTracks
                    .Select(track => track.GetProperty("album").GetProperty("release_date").GetString())
                    .OrderByDescending(date => date)
                    .FirstOrDefault();
    }

    private bool CheckIfLastFmTopTrackInTop5()
    {
        var topTrackNames = _topTracks.Select(track => track.GetProperty("name").GetString()).ToList();
        foreach (var lastFmTopTrack in _lastFmTopTracks)
        {
            if (topTrackNames.Contains(lastFmTopTrack)) return true;
        }
        return false;
    }
    
    public bool CheckArtistMeetsPopularityMinimum()
    {
        if (LatestTopTrackReleaseDate == null || Popularity == null)
            return false;

        if (!Regex.IsMatch(LatestTopTrackReleaseDate, @"^\d{4}-\d{2}-\d{2}$"))
        {
            return false;
        }

        DateTime latestDate = DateTime.Parse(LatestTopTrackReleaseDate);

        TimeSpan timeSinceRelease = DateTime.UtcNow - latestDate;
        int yearsSinceRelease = (int)(timeSinceRelease.TotalDays / 365);

        int minPopularity = yearsSinceRelease switch
        {
            < 1 => 50, 
            <= 5 => 55,
            <= 10 => 60,
            _ => 65
        };

        return Popularity >= minPopularity;
    }
    public string? GetValidationError()
    {
        if (SpotifyId == null)
            return $"has missing Spotify ID";
        if (Popularity == null)
            return $"has missing popularity data";
        if (Name == null)
            return $"has missing Spotify Name";
        if (!_isLastFmTopTrackInTop5)
            return $"last fm tracks do not match spotify tracks";
        if (!MeetsPopularityMinimum)
            return "artist does not meet popularity minimum";

        return null;
    }
}