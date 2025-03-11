using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExternalWebServices.Interfaces;

// Citation on PKCE BASICS
// Citation on Hashing
// Citation on states
// Citation on Code challenges and code verifiers
namespace ExternalWebServices;

public class SpotifyPkceCodeAuthorizer : ISpotifyPkceCodeAuthorizer
{
    private readonly string _clientId;
    private readonly string _redirectUri;
    private readonly ConcurrentDictionary<string, string> _codeVerifiers = new();
    private readonly HttpClient _httpClient;


    public SpotifyPkceCodeAuthorizer(HttpClient httpClient, string clientId, string redirectUri)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _redirectUri = redirectUri;
    }

    public async Task<(string AuthUrl, string State)> GetAuthorizationUrl()
    {
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);
        string state = GenerateState();

        _codeVerifiers.TryAdd(state, codeVerifier);

        string authUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={_clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}&code_challenge_method=S256" +
                         $"&code_challenge={codeChallenge}&scope=playlist-modify-public&state={state}";
        //  $"https://accounts.spotify.com/authorize?response_type=code&client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&code_challenge_method=S256code_challenge={codeChallenge}&scope=playlist-modify-public&state={state}";

        return (authUrl, state);
    }

    public async Task<string> ExchangeCode(string code, string state)
    {
        if (!_codeVerifiers.TryRemove(state, out string codeVerifier))
            throw new Exception("Invalid state or code verifier not found.");

        return await ExchangeAuthorizationCodeForAccessToken(code, codeVerifier);
    }

    private async Task<string> ExchangeAuthorizationCodeForAccessToken(string code, string codeVerifier)
    {
        HttpResponseMessage response = await _httpClient.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            }));

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to exchange authorization code. Status: {response.StatusCode}");

        string jsonResponse = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(jsonResponse);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private string GenerateCodeVerifier()
    {
        byte[] bytes = new byte[32];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string GenerateState()
    {
        byte[] bytes = new byte[32];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}