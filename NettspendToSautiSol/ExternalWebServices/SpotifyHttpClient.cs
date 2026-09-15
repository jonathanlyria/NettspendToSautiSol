namespace ExternalWebServices;

public static class SpotifyHttpClient
{
    public static HttpClient Create() => new(new HttpClientHandler
    {
        // Spotify's API uses explicit tokens, not cookies. This also avoids
        // CookieContainer's domain-name lookup failing in restricted macOS hosts.
        UseCookies = false
    });
}
