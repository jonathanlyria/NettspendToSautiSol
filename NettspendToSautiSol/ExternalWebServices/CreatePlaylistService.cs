
using System.Text;
using System.Text.Json;

namespace NettspendToSautiSol;

public class CreatePlaylistService: ICreatePlaylistService
{
    private readonly HttpClient _httpClient;
    public CreatePlaylistService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CreatePlaylist(List<string> songIds, string firstArtist, string lastArtist, string accessToken)
    {
        string playlistId = await AddPlaylistToUserLibrary(firstArtist, lastArtist, accessToken);
        await AddSongsToPlaylist(songIds, playlistId, accessToken);
        return $"https://open.spotify.com/playlist/{playlistId}";
    }

    private async Task<string> AddPlaylistToUserLibrary(string firstArtist, string lastArtist, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        string playlistId;
        string userId = await GetUserId(accessToken);

        var payload = new
        {
            name = $"from {firstArtist} to {lastArtist}",
            description = $"tool created by,  creates a playlist that transitions between" +
                          $" {firstArtist} and {lastArtist}.",
            @public = true
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        StringContent content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error creating playlist: {response.StatusCode}");
            }

            string jsonResponse = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Playlist created successfully: {jsonResponse}");
            JsonDocument document = JsonDocument.Parse(jsonResponse);
            playlistId = document.RootElement.GetProperty("id").GetString();
            Console.WriteLine($"Playlist ID: {playlistId}");
            return playlistId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating playlist: {ex.Message}");
        }
    }

    private async Task AddSongsToPlaylist(List<string> songIds, string playlistId, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        string url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";

        var requestBody = new
        {
            uris = songIds.Select(trackId => $"spotify:track:{trackId}").ToArray()
        };
                
        string jsonBody = JsonSerializer.Serialize(requestBody);

        StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to add tracks to the playlist {response.StatusCode}: {response.ReasonPhrase}.");
        }

    }
    

    private async Task<string> GetUserId(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("https://api.spotify.com/v1/me");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve user ID: {response.StatusCode} - {response.ReasonPhrase}");
            }
            
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve user ID: {ex.Message}");
        }
        
        string jsonResponse = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(jsonResponse);
        if (document.RootElement.GetProperty("id").GetString() == null)
            throw new Exception("Failed to retrieve user ID.");
        
        return document.RootElement.GetProperty("id").GetString();
    }

}