using System.Text;
using System.Text.Json;
using ExternalWebServices.Interfaces;

// Citation of spotify Api Client Credentials 
namespace ExternalWebServices;

public class SpotifyClientCredentialAuthorizer : ISpotifyClientCredentialAuthorizer
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private const string TokenUrl = "https://accounts.spotify.com/api/token";
    private readonly HttpClient _httpClient;

    public SpotifyClientCredentialAuthorizer(string clientId, string clientSecret, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public async Task<(string AccessToken, int ExpiresIn)> GetAccessToken()
    {
        string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        StringContent requestContent = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
        HttpResponseMessage response = await _httpClient.PostAsync(TokenUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to retrieve access token. Status code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        JsonElement tokenData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

        string? accessToken = tokenData.GetProperty("access_token").GetString();
        int expiresIn = tokenData.GetProperty("expires_in").GetInt32();

        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("Access token not found in response.");

        return (accessToken, expiresIn);
    }
}
