using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExternalWebServices.Interfaces;

namespace ExternalWebServices;

public class CreatePlaylistService(HttpClient httpClient) : ICreatePlaylistService
{
    public async Task<string> CreatePlaylist(List<string> songIds, string firstArtist, string lastArtist, string accessToken)
    {
        // Resolve songs before creating anything in the user's library.
        if (songIds.Count == 0)
            throw new SpotifyApiException("No suitable songs were found. No playlist was created.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.spotify.com/v1/me/playlists");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            name = $"from {firstArtist} to {lastArtist}",
            description = $"A playlist that transitions between {firstArtist} and {lastArtist}.",
            @public = true
        });
        using var response = await httpClient.SendAsync(request);
        SpotifyApiException.Check(response, "playlist creation");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string playlistId = document.RootElement.GetProperty("id").GetString()
            ?? throw new SpotifyApiException("Spotify created a playlist but returned no playlist ID. Check your library before retrying.");
        string link = $"https://open.spotify.com/playlist/{playlistId}";

        try
        {
            // Spotify permits at most 100 items per request; preserve route order.
            foreach (var batch in songIds.Chunk(100))
            {
                using var add = new HttpRequestMessage(HttpMethod.Post,
                    $"https://api.spotify.com/v1/playlists/{Uri.EscapeDataString(playlistId)}/items");
                add.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                add.Content = JsonContent.Create(new { uris = batch.Select(id => $"spotify:track:{id}").ToArray() });
                using var added = await httpClient.SendAsync(add);
                SpotifyApiException.Check(added, "adding songs");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SpotifyApiException)
        {
            // Never automatically retry writes: a timeout can follow a successful write.
            throw new SpotifyApiException($"The playlist was created, but adding all songs could not be confirmed. Check {link} before trying again.");
        }
        return link;
    }
}
