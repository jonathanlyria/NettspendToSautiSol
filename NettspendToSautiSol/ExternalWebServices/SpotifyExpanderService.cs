using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExternalWebServices.Interfaces;

namespace ExternalWebServices;

public class SpotifyExpanderService : ISpotifyExpanderService
{
    private readonly ISpotifyClientCredentialAuthorizer _spotifyClientCredentialAuthorizer;
    private readonly HttpClient _httpClient;
    private string _accessToken;
    private DateTime _tokenExpiryTime;

    public SpotifyExpanderService(ISpotifyClientCredentialAuthorizer spotifyClientCredentialAuthorizer, HttpClient httpClient)
    {
        _spotifyClientCredentialAuthorizer = spotifyClientCredentialAuthorizer;
        _httpClient = httpClient;
        _accessToken = string.Empty; 
        _tokenExpiryTime = DateTime.UtcNow;
    }

    private async Task RefreshAccessToken()
    {
        if (DateTime.UtcNow >= _tokenExpiryTime.AddMinutes(-1))
        {
            try
            {
                (string AccessToken, int ExpiresIn) tokenData = await _spotifyClientCredentialAuthorizer.GetAccessToken();
                _accessToken = tokenData.AccessToken;
                _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Access token refreshed successfully.");
                Console.ResetColor();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing access token: {ex.Message}");
                throw new Exception("Failed to refresh access token.", ex);
            }
        }
    }

    public async Task<KeyValuePair<(string, string), int>> GetArtistDetails(string artistName)
    {
        await RefreshAccessToken();


        try
        {
            string url = $"https://api.spotify.com/v1/search?q=artist:\"{Uri.EscapeDataString(artistName)}\"&type=artist&limit=1";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new InvalidOperationException("Spotify API returned an empty response.");

            using var document = JsonDocument.Parse(responseContent);
            var artistData = document.RootElement
                .GetProperty("artists")
                .GetProperty("items")
                .EnumerateArray()
                .FirstOrDefault();

            if (artistData.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException("No artist found for the given name.");
            try
            {
                string spotifyId = artistData.GetProperty("id").GetString() ?? throw new InvalidOperationException();
                string name = artistData.GetProperty("name").GetString()  ?? throw new InvalidOperationException();
                int popularity = artistData.GetProperty("popularity").GetInt32();
                return new KeyValuePair<(string, string), int>((spotifyId, name), popularity);
            }
            catch (NullReferenceException ex)
            {
                throw new InvalidOperationException("Artist does not have valid data.");
            }
            
          
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Spotify API JSON response.", ex);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<Dictionary<string, DateTime>> GetTopTracks(string spotifyId)
    {
        await RefreshAccessToken();
        Console.WriteLine($"{spotifyId}");

        var topTracks = new Dictionary<string, DateTime>();

        try
        {
            string url = $"https://api.spotify.com/v1/artists/{spotifyId}/top-tracks";

            var response = await _httpClient.GetAsync(url);

            string responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new InvalidOperationException("Spotify API returned an empty response.");

            var document = JsonDocument.Parse(responseContent);
            foreach (var topTrack in document.RootElement.GetProperty("tracks").EnumerateArray())
            {
                string topTrackName = topTrack.GetProperty("name").GetString()!;
                topTrackName = Regex.Replace(topTrackName, @"\s*[\(\[].*?[\)\]]\s*", "").Trim();
                string releaseDateStr = topTrack.GetProperty("album").GetProperty("release_date").GetString()!;

                if (!Regex.IsMatch(releaseDateStr, @"^\d{4}(-\d{2}(-\d{2})?)?$"))
                {
                    throw new FormatException($"Invalid date format: {releaseDateStr}");
                }

                string[] parts = releaseDateStr.Split('-');
                int year = int.Parse(parts[0]);
                int month = parts.Length >= 2 ? int.Parse(parts[1]) : 1;
                int day = parts.Length >= 3 ? int.Parse(parts[2]) : 1;

                DateTime topTrackDate;
                try
                {
                    topTrackDate = new DateTime(year, month, day);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new FormatException($"Invalid date values in: {releaseDateStr}", ex);
                }

                topTracks[topTrackName.ToLower()] = topTrackDate;
            }
            return topTracks;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Spotify API JSON response.", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid date format or value: {ex.Message}", ex);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
