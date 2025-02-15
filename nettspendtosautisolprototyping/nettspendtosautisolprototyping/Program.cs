using System.Net.Http.Headers;
using System.Text.Json;
using NettspendToSautiSol;

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
            Console.WriteLine($"id: {testArtist.SpotifyId} populairty: {testArtist.Popularity}, last popular track: {testArtist.LatestTopTrackReleaseDate}");
        }

       
        
    
    }
}

public class GetSpotifyDetails
{
    public string? SpotifyId { get; private set; }
    public int? Popularity { get; private set; }
    private static readonly HttpClient HttpClient = new HttpClient();
    public string? LatestTopTrackReleaseDate { get; private set; }
    private readonly string _accessToken;

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
            JsonElement artistData = document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray().FirstOrDefault();

            if (!artistData.Equals(default(JsonElement)))
            {
                SpotifyId = artistData.GetProperty("id").GetString();
                Popularity = artistData.GetProperty("popularity").GetInt32();
            }
        }
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    private void GetLatestTopTrackReleaseDate()
    {
        if (SpotifyId == null) return;

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        string url = $"https://api.spotify.com/v1/artists/{SpotifyId}/top-tracks?market=US";

        HttpResponseMessage response = HttpClient.GetAsync(url).Result;
        if (response.IsSuccessStatusCode)
        {
            JsonDocument document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            var tracks = document.RootElement.GetProperty("tracks").EnumerateArray().Take(5);
    
            LatestTopTrackReleaseDate = tracks
                .Select(track => track.GetProperty("album").GetProperty("release_date").GetString())
                .OrderByDescending(date => date)
                .FirstOrDefault();
        }
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }
}
