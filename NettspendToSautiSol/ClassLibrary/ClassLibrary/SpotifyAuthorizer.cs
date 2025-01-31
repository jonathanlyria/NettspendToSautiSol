using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Collections.Concurrent;
using System.Text.Json;

public class SpotifyAuthorizer
{
    private readonly string _clientId = "c6ed8f690a15491f9deb29547c8447ff";
    private readonly string _redirectUri = "http://127.0.0.1:8080/website/callback.html"; 
    private readonly ConcurrentDictionary<string, string> _codeVerifiers = new();

    public (string AuthUrl, string State) GetAuthorizationUrl()
    {
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);
        string state = GenerateState();

        _codeVerifiers.TryAdd(state, codeVerifier);

        string authUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={_clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}&code_challenge_method=S256" +
                         $"&code_challenge={codeChallenge}&scope=playlist-modify-public&state={state}";

        return (authUrl, state);
    }

    public string ExchangeCode(string code, string state)
    {
        if (!_codeVerifiers.TryRemove(state, out string codeVerifier))
            throw new Exception("Invalid state or code verifier not found.");

        return ExchangeAuthorizationCodeForAccessToken(code, codeVerifier);
    }

    private string ExchangeAuthorizationCodeForAccessToken(string code, string codeVerifier)
    {
        using HttpClient client = new();
        HttpResponseMessage response = client.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            })).Result;

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to exchange authorization code. Status: {response.StatusCode}");

        string jsonResponse = response.Content.ReadAsStringAsync().Result;
        JsonDocument document = JsonDocument.Parse(jsonResponse);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private static string GenerateCodeVerifier()
    {
        byte[] bytes = new byte[32];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateState()
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