using System.Text.Json;
using System.Text.RegularExpressions;
using ExternalWebServices.Interfaces;

namespace ExternalWebServices;

public class LastFmApiService : ILastFmApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public LastFmApiService(string apiKey, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }
    
    public async Task<Dictionary<string, double>> GetSimilarArtists(string artistName)
    {
        var similarArtists = new Dictionary<string, double>();
        var similarArtistParameters = new Dictionary<string, string>
        {
            { "method", "artist.getSimilar" },
            { "artist", artistName },
            { "api_key", _apiKey },
            { "format", "json" },
            { "limit", "5" }
        };

        string requestUrl = BuildUrlWithParams("https://ws.audioscrobbler.com/2.0/", similarArtistParameters);

        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new InvalidOperationException("Response content is empty.");

            using var document = JsonDocument.Parse(responseContent);
            if (!DoSimilarArtistsExist(document))
                throw new InvalidOperationException("Similar artists data is missing.");

            foreach (var artistData in document.RootElement.GetProperty("similarartists")
                         .GetProperty("artist").EnumerateArray())
            {
                if (!DoesLastFmArtistHaveNameAndMatch(artistData))
                    throw new InvalidOperationException("Artist does not have name and match.");

                string foundArtist = artistData.GetProperty("name").GetString()!;
                double match = double.Parse(artistData.GetProperty("match").GetString()!);
                if (!foundArtist.Contains('&'))
                    similarArtists[foundArtist] = match;
            }

            return similarArtists;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse JSON response.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error {ex.Message}", ex);
        }
    }

    public async Task<List<string>> GetTopTracks(string artistName)
    {
        var topTracks = new List<string>();
        var topTrackParameters = new Dictionary<string, string>
        {
            { "method", "artist.getTopTracks" },
            { "artist", artistName },
            { "api_key", _apiKey },
            { "format", "json" },
        };
        string requestUrl = BuildUrlWithParams("https://ws.audioscrobbler.com/2.0/", topTrackParameters);

        try
        { 
            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new InvalidOperationException("Response content is empty.");

            using var document = JsonDocument.Parse(responseContent);
            if (!document.RootElement.TryGetProperty("toptracks", out var toptracks) ||
                !toptracks.TryGetProperty("track", out var tracksArray))
                throw new InvalidOperationException("Artist has no top tracks.");

            foreach (var track in tracksArray.EnumerateArray())
            {
                string topTrack = track.GetProperty("name").GetString() ?? "Unknown Track";
                if (topTrack == "Unknown Track")
                    throw new InvalidOperationException("Top track name is unknown.");
                topTrack = Regex.Replace(topTrack, @"\s*[\(\[].*?[\)\]]\s*", "").Trim();
                
                topTracks.Add(topTrack.ToLower());
            }

            return topTracks;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse JSON response.", ex);
        }
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
    private bool DoSimilarArtistsExist(JsonDocument document)
    {
        return document.RootElement.TryGetProperty("similarartists", out JsonElement similarArtistsElement) &&
               similarArtistsElement.TryGetProperty("artist", out _);
    }
    private bool DoesLastFmArtistHaveNameAndMatch(JsonElement artistData)
    {
        return artistData.TryGetProperty("name", out _) && artistData.TryGetProperty("match", out _);
    }
}