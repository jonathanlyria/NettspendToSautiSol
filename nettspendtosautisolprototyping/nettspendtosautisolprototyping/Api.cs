using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NettspendToSautiSol;

namespace nettspendtosautisolprototyping;

public class PlaylistInfoTransferObject() // data transfer object as post requests can only recieve one variable from the body
{
    public string Name { get; set; }
    public List<string> Uris { get; set; }
    public string Token { get; set; } 
}

[ApiController]
[Route("api")]
public class Api : ControllerBase //inherits from ControllerBase which is part of  Microsoft.AspNetCore.Mvc
{
    private static readonly HttpClient Client = new HttpClient();

    [HttpGet("get-song-ids")]
    public IActionResult GetSongIds([FromQuery] List<string> songs)
    {
       
        SpotifyClientCredentials credentials = new SpotifyClientCredentials(); // uses client credentials flow as it is unscoped data; will be explained shortly
        var token = credentials.GetAccessTokenAsync().Result.AccessToken;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        List<string> songIds = new List<string>();
        foreach (string song in songs)
        {
            string url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(song)}&type=track&limit=1";
            HttpResponseMessage songResponse = Client.GetAsync(url).Result;
            string songResponseContent =  songResponse.Content.ReadAsStringAsync().Result;
            JsonDocument songDocument = JsonDocument.Parse(songResponseContent);

            var track = songDocument.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray().FirstOrDefault();
            songIds.Add("spotify:track:" + track.GetProperty("id").GetString());

        }

        return Ok(new { songIds }); // returns the status code 200 that confirms everything went well and teh list of songIds
    }
    
    [HttpPost("post-test-playlist")]
    public IActionResult PostTestPlaylist([FromBody] PlaylistInfoTransferObject playlistInfo) // uses the data transfer object
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", playlistInfo.Token); //uses token as authentication from user THIS IS NOT SAFE, WILL CHANGE FOR FINAL CODE
        HttpResponseMessage userIdResponse = Client.GetAsync("https://api.spotify.com/v1/me").Result; 
        string stringUserIdResponse = userIdResponse.Content.ReadAsStringAsync().Result;
        JsonDocument userIdDocument = JsonDocument.Parse(stringUserIdResponse);
        string userId = userIdDocument.RootElement.GetProperty("id").GetString();
        
        var payload = new
        {
            name = playlistInfo.Name, // from data transfer object
            @public = false
        };
        string jsonPayload = JsonSerializer.Serialize(payload);
        StringContent createPlaylist = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage playlistResponseMessage = Client.PostAsync($"https://api.spotify.com/v1/users/{userId}/playlists", createPlaylist).Result;
        JsonDocument document = JsonDocument.Parse(playlistResponseMessage.Content.ReadAsStringAsync().Result);
        string? playlistId = document.RootElement.GetProperty("id").GetString();
        
        var requestBody = new
        {
            uris = playlistInfo.Uris,
        };
        string jsonBody = JsonSerializer.Serialize(requestBody);
        StringContent addSongsToPlaylist = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        HttpResponseMessage addSongsResponseMessage = Client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", addSongsToPlaylist).Result;
        Console.WriteLine(addSongsResponseMessage.Content.ReadAsStringAsync().Result);
        return Ok(new
        {
            playlistLink = $"https://open.spotify.com/playlist/{playlistId}", // returns the playlistId
        });
    }

}