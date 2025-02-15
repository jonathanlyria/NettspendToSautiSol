using System.Net.Http.Headers;
using System.Text.Json;
using NettspendToSautiSol;
using System.Text.RegularExpressions;

namespace nettspendtosautisolprototyping;

public class Program
{
    public static void Main(string[] args)
    {
        SpotifyClientCredentials authorizer = new SpotifyClientCredentials();
        var accessToken = authorizer.GetAccessTokenAsync().Result.AccessToken;
        while (true)
        {
            string artistName = Console.ReadLine();
            GetSpotifyDetails testArtist = new GetSpotifyDetails(artistName, accessToken);
            Console.WriteLine($"name : {testArtist.SpotifyName} id: {testArtist.SpotifyId} populairty: {testArtist.Popularity}, last popular track: {testArtist.LatestTopTrackReleaseDate}, meets popularity requirment {testArtist.MeetsPopularityRequirement()}");
        }

       
        
    
    }
}

public class GetSpotifyDetails
{
    public string? SpotifyId { get; private set; }
    public int? Popularity { get; private set; }
    public string? LatestTopTrackReleaseDate { get; private set; }
    
    // New property to return the Spotify artist name.
    public string? SpotifyName { get; private set; }
    
    private readonly string _accessToken;
    private static readonly HttpClient HttpClient = new HttpClient();

    public GetSpotifyDetails(string artistName, string accessToken)
    {
        _accessToken = accessToken;
        GetArtistDetails(artistName);
        if (SpotifyId != null)
        {
            GetLatestTopTrackReleaseDate();
        }
    }

    private void GetArtistDetails(string artistName)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        string url = $"https://api.spotify.com/v1/search?q=artist:\"{artistName}\"&type=artist&limit=1";

        HttpResponseMessage response = HttpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            JsonElement artistData = document.RootElement
                .GetProperty("artists")
                .GetProperty("items")
                .EnumerateArray()
                .FirstOrDefault();

            if (!artistData.Equals(default(JsonElement)))
            {
                SpotifyId = artistData.GetProperty("id").GetString();
                // Retrieve the Spotify artist name.
                SpotifyName = artistData.GetProperty("name").GetString();
                Popularity = artistData.GetProperty("popularity").GetInt32();
            }
        }
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    private void GetLatestTopTrackReleaseDate()
    {
        if (SpotifyId == null)
            return;

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        string url = $"https://api.spotify.com/v1/artists/{SpotifyId}/top-tracks?market=US";

        HttpResponseMessage response = HttpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            var tracks = document.RootElement
                .GetProperty("tracks")
                .EnumerateArray()
                .Take(5);

            LatestTopTrackReleaseDate = tracks
                .Select(track => track.GetProperty("album")
                                      .GetProperty("release_date")
                                      .GetString())
                .OrderByDescending(date => date)
                .FirstOrDefault();
        }
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    public bool MeetsPopularityRequirement()
    {
        if (LatestTopTrackReleaseDate == null || Popularity == null)
            return false;

        // Validate that LatestTopTrackReleaseDate is in the expected format "yyyy-MM-dd"
        if (!Regex.IsMatch(LatestTopTrackReleaseDate, @"^\d{4}-\d{2}-\d{2}$"))
        {
            return false; // Reject if the format is incorrect
        }
    
        DateTime latestDate = DateTime.Parse(LatestTopTrackReleaseDate);

        TimeSpan timeSinceRelease = DateTime.UtcNow - latestDate;
        int yearsSinceRelease = (int)(timeSinceRelease.TotalDays / 365);

        int minPopularity = yearsSinceRelease switch
        {
            < 1 => 45,
            >= 1 and <= 5 => 50,
            > 5 and <= 10 => 60,
            _ => 65
        };

        return Popularity >= minPopularity;
    }

}
