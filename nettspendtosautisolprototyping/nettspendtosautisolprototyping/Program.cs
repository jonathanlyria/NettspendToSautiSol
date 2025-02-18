using System.Net.Http.Headers;
using System.Text.Json;
using NettspendToSautiSol;
using System.Text.RegularExpressions;

namespace nettspendtosautisolprototyping;

public class Program
{
    public static async Task Main(string[] args)
    {
        SpotifyClientCredentials authorizer = new SpotifyClientCredentials();
        var accessToken = authorizer.GetAccessTokenAsync().Result.AccessToken;
        while (true)
        {
            Console.WriteLine("Please enter the name of the artist:");
            string artistName = Console.ReadLine();
            LastFmData lastFmData = new LastFmData(artistName);
            List<string> lastFmTopTracks = lastFmData.GetTopTracks();
            if (!lastFmData.isLastFmDataValid)
            {
                Console.WriteLine(lastFmData.lastFmDataInvalidReason);
            }
            foreach (var topTrack in lastFmTopTracks)
            {
                Console.WriteLine(topTrack);
            }
            
            SpotifyData spotifyData = new SpotifyData(artistName, accessToken, lastFmTopTracks);
            Console.WriteLine($"name : {spotifyData.Name} id: {spotifyData.SpotifyId} populairty: {spotifyData.Popularity}, last popular track: {spotifyData.LatestTopTrackReleaseDate}, meets popularity requirment {spotifyData.MeetsPopularityMinimum}, do last fmtoptracks match in spotify top 5 {spotifyData._isLastFmTopTrackInTop5}");
        }
    }
}

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
        return null;
    }
}
public class LastFmData
{
    private string LastFmApiKey = "00751a650c0182344603b9252c66d416";
    public bool isLastFmDataValid;
    public string lastFmDataInvalidReason;
    public string url = "http://ws.audioscrobbler.com/2.0/";
    public string ArtistName;
    private static HttpClient HttpClient = new HttpClient();


    public LastFmData(string artistName)
    {
        lastFmDataInvalidReason = "";
        isLastFmDataValid = true;
        ArtistName = artistName;
 

    }

    public Dictionary<string, double> GetSimilarArtists()
    {
        Dictionary<string, double> similarArtists = new Dictionary<string, double>();
        Dictionary<string, string> similarArtistParameters = new Dictionary<string, string>
        {
            { "method", "artist.getSimilar" },
            { "artist", ArtistName },
            { "api_key", LastFmApiKey },
            { "format", "json" },
            { "limit", "5"}
        };
        HttpResponseMessage response = HttpClient.GetAsync(BuildUrlWithParams(url, similarArtistParameters)).Result;
        if (!response.IsSuccessStatusCode)
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = response.ReasonPhrase;
            return similarArtists;
        }
        
        string responseContent = response.Content.ReadAsStringAsync().Result;
        
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = "Response content is empty";
            return similarArtists;
        };
        
        JsonDocument document = JsonDocument.Parse(responseContent);
        if (!DoSimilarArtistsExist(document))
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = "artist has no similar artists";
            return similarArtists;
        }

        foreach (JsonElement artistData in document.RootElement.GetProperty("similarartists")
                     .GetProperty("artist").EnumerateArray())
        {
            if (!DoesLastFmArtistHaveNameAndMatch(artistData))
            {
                isLastFmDataValid = false;
                lastFmDataInvalidReason = "artist does not have valid name and match";
                continue;
            }
            
            string foundArtist = artistData.GetProperty("name").GetString();
            double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
            if (foundArtist.Contains('&')) continue;
            similarArtists.Add(foundArtist, match);
        }

        return similarArtists;

    }

    public List<string> GetTopTracks()
    {
        var topTracks = new List<string>();
        var topTrackParameters = new Dictionary<string, string>
        {
            { "method", "artist.getTopTracks" },
            { "artist", ArtistName },
            { "api_key", LastFmApiKey },
            { "format", "json" },
            { "limit", "10" }
        };

        HttpResponseMessage response = HttpClient.GetAsync(BuildUrlWithParams(url, topTrackParameters)).Result;
        if (!response.IsSuccessStatusCode)
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = response.ReasonPhrase;
            return topTracks;
        }

        string responseContent = response.Content.ReadAsStringAsync().Result;
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = "Response content is empty";
            return topTracks;
        }

        using JsonDocument document = JsonDocument.Parse(responseContent);
        if (!document.RootElement.TryGetProperty("toptracks", out JsonElement toptracks) ||
            !toptracks.TryGetProperty("track", out JsonElement tracksArray))
        {
            isLastFmDataValid = false;
            lastFmDataInvalidReason = "Artist has no top tracks";
            return topTracks;
        }

        topTracks = tracksArray.EnumerateArray()
            .Select(track => track.GetProperty("name").GetString() ?? "Unknown Track")
            .ToList();

        return topTracks;
    }

    
    
    
    private bool DoSimilarArtistsExist(JsonDocument document)
    {
        return document.RootElement.TryGetProperty("similarartists", out JsonElement similarArtistsElement) &&
               similarArtistsElement.TryGetProperty("artist", out _);
    }
    private bool DoesLastFmArtistHaveNameAndMatch(JsonElement artistData)
    {
        return artistData.TryGetProperty("name", out _) && artistData.TryGetProperty("match", out _);
    }
   
    private string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
    {
        List<string> paramList = new List<string>();
        foreach (KeyValuePair<string, string> param in parameters)
        {
            paramList.Add($"{param.Key}={param.Value}");
        }
        return $"{url}?{string.Join("&", paramList)}";
    }


    
}