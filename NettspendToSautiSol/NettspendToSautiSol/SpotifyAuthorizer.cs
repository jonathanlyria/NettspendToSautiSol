using System.Net;
using System.Text.Json;

using System.Security.Cryptography;
using System.Text;
using System.Web;

public class SpotifyAuthorizer
{
    private readonly string _clientId = "c6ed8f690a15491f9deb29547c8447ff";
    private readonly string clientSecret = "34009c71d59748a09bf3867de0f8869e";
    private readonly string _redirectUri = "http://localhost:53882/callback/";
    private string? _accessToken;
    public string Reason;
    
    public string GetAuthorizationPKCEAccessToken()
    {
        if (!string.IsNullOrEmpty(_accessToken))
            return _accessToken;

        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        using HttpListener listener = new();
        listener.Prefixes.Add(_redirectUri);
        listener.Start();

        // Open the browser for authorization
        OpenBrowserForAuthorization(codeChallenge);

        // Wait for the redirect with the authorization code
        var context = listener.GetContext();
        var query = context.Request.Url?.Query;
        listener.Stop();

        if (string.IsNullOrEmpty(query) || !query.Contains("code="))
        {
            return "Did not finish signing in";
        }
        else
        {
            string authorizationCode = HttpUtility.ParseQueryString(query).Get("code")!;
            return ExchangeAuthorizationCodeForAccessToken(authorizationCode, codeVerifier);
        }
      
    }

    private void OpenBrowserForAuthorization(string codeChallenge)
    {
        string authUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={_clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}&code_challenge_method=S256" +
                         $"&code_challenge={codeChallenge}&scope=playlist-modify-public";

        Console.WriteLine("Opening browser for authorization...");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true
        });
    }

    private string ExchangeAuthorizationCodeForAccessToken(string authorizationCode, string codeVerifier)
    {
        using HttpClient client = new();
        var response = client.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            })).Result;

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to exchange authorization code. Status: {response.StatusCode}");

        string jsonResponse = response.Content.ReadAsStringAsync().Result;
        var document = JsonDocument.Parse(jsonResponse);
        _accessToken = document.RootElement.GetProperty("access_token").GetString();

        return _accessToken!;
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
}