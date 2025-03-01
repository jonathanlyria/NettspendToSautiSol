using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace TestFrontend;

public class SpotifyPkce
{
    private readonly string _clientId = "4b48d508580749dab1ec05cbe16e51e5"; // this is my application id, it does not need to be kept secret
    private readonly string _redirectUri = "http://localhost:8888/callback/"; // this is my callback uri which i specified in my 
    private string? _accessToken; // final access token for the user

    public string GetAuthoizationPKCEAccessToken() // returns pkce access token
    {
        if (!string.IsNullOrEmpty(_accessToken))
            return _accessToken;
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);
        using HttpListener listener = new(); // listens on redirect uri for callback from user logging in
        listener.Prefixes.Add(_redirectUri);
        listener.Start();
        OpenBrowserForAuthorization(codeChallenge); // opens browser so user can login to spotify

        var context = listener.GetContext();
        var query = context.Request.Url?.Query;
        listener.Stop();
        if (string.IsNullOrEmpty(query) || !query.Contains("code=")) // if user rejects to login
            throw new Exception("Authorization failed. No code received.");
        string authorizationCode = HttpUtility.ParseQueryString(query).Get("code")!;
        return ExchangeAuthorizationCodeForAccessToken(authorizationCode, codeVerifier);
    }

    private void OpenBrowserForAuthorization(string codeChallenge) 
    {
        // sends the code challenge which is just a hased random number to spotify
        string authUrl = 
            $"https://accounts.spotify.com/authorize?response_type=code&client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&code_challenge_method=S256&code_challenge={codeChallenge}&scope=playlist-modify-private";
            
        Console.WriteLine("Opening browser for authorization...");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true,
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
            })).Result;  // sends the authorization code recieved from spotify, the client id, the redirect url and the
                         // original code verifier to spotify, in exchange for the access token
        if (!response.IsSuccessStatusCode) 
            throw new Exception($"Failed to exchange authorization code. Status: {response.StatusCode}");
        string jsonResponse = response.Content.ReadAsStringAsync().Result;
        var document = JsonDocument.Parse(jsonResponse);
        _accessToken = document.RootElement.GetProperty("access_token").GetString();
        return _accessToken!;
    }
    private static string GenerateCodeVerifier() // generates a random string
    {
        byte[] bytes = new byte[32];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
    private static string GenerateCodeChallenge(string codeVerifier) // hashes the code verifier
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}