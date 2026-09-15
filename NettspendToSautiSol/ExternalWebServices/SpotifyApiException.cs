using System.Net;

namespace ExternalWebServices;

// Only application-authored messages and status codes are exposed to the caller.
// Never include token responses, credentials or raw provider error bodies.
public sealed class SpotifyApiException(string message) : Exception(message)
{
    public static void Check(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        string advice = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Sign in to Spotify again.",
            HttpStatusCode.Forbidden => "Check the app owner's Premium subscription, the app's allowed users and Spotify permissions.",
            HttpStatusCode.TooManyRequests => "Spotify is rate limiting requests. Wait before trying again.",
            _ => "Refresh the page and sign in again before retrying."
        };
        throw new SpotifyApiException($"Spotify rejected {operation} (HTTP {(int)response.StatusCode}). {advice}");
    }
}
