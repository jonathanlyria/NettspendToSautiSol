namespace NettspendToSautiSol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

public class LastFmData
{
    private const string LastFmApiKey = "00751a650c0182344603b9252c66d416";
    public bool IsLastFmDataValid;
    public string LastFmDataInvalidReason;
    public string url = "http://ws.audioscrobbler.com/2.0/";
    public string ArtistName;
    private static HttpClient HttpClient = new HttpClient();


    public LastFmData(string artistName)
    {
        LastFmDataInvalidReason = "";
        IsLastFmDataValid = true;
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
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = response.ReasonPhrase;
            return similarArtists;
        }
        
        string responseContent = response.Content.ReadAsStringAsync().Result;
        
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = "Response content is empty";
            return similarArtists;
        };
        
        JsonDocument document = JsonDocument.Parse(responseContent);
        if (!DoSimilarArtistsExist(document))
        {
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = "artist has no similar artists";
            return similarArtists;
        }

        foreach (JsonElement artistData in document.RootElement.GetProperty("similarartists")
                     .GetProperty("artist").EnumerateArray())
        {
            if (!DoesLastFmArtistHaveNameAndMatch(artistData))
            {
                IsLastFmDataValid = false;
                LastFmDataInvalidReason = "artist does not have valid name and match";
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
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = response.ReasonPhrase;
            return topTracks;
        }

        string responseContent = response.Content.ReadAsStringAsync().Result;
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = "Response content is empty";
            return topTracks;
        }

        using JsonDocument document = JsonDocument.Parse(responseContent);
        if (!document.RootElement.TryGetProperty("toptracks", out JsonElement toptracks) ||
            !toptracks.TryGetProperty("track", out JsonElement tracksArray))
        {
            IsLastFmDataValid = false;
            LastFmDataInvalidReason = "Artist has no top tracks";
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