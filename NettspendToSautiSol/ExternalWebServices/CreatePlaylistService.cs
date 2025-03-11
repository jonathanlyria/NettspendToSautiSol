using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExternalWebServices.Interfaces;

namespace ExternalWebServices;

public class CreatePlaylistService: ICreatePlaylistService
{
    private readonly HttpClient _httpClient;
    public CreatePlaylistService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    // used by server to create playlist once sonAgs have been found
    public async Task<string> CreatePlaylist(List<string> songIds, string firstArtist, string lastArtist, string accessToken) 
    {
        try
        {
            string playlistId = await AddPlaylistToUserLibrary(firstArtist, lastArtist, accessToken);
            await AddSongsToPlaylist(songIds, playlistId, accessToken);
            return $"https://open.spotify.com/playlist/{playlistId}";
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Spotify API JSON response.", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid date format or value: {ex.Message}", ex);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }        
    }

    private async Task<string> AddPlaylistToUserLibrary(string firstArtist, string lastArtist, string accessToken)
    {
        string userId = await GetUserId(accessToken);
        
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, 
                $"https://api.spotify.com/v1/users/{userId}/playlists"
            );
            
            var content = new
            {
                name = $"from {firstArtist} to {lastArtist}",
                description = $"Creates a playlist that transitions between" +
                              $" {firstArtist} and {lastArtist}.",
                @public = true
            };
            
            request.Content = new StringContent(
                JsonSerializer.Serialize(content), 
                Encoding.UTF8, 
                "application/json"
            );
    
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error creating playlist: {response.StatusCode}");
            }

            string jsonResponse = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Playlist created successfully: {jsonResponse}");
            
            JsonDocument document = JsonDocument.Parse(jsonResponse);
            var playlistId = document.RootElement.GetProperty("id").GetString();
            
            Console.WriteLine($"Playlist ID: {playlistId}");
            return playlistId;
            
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Spotify API JSON response.", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid date format or value: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Something went wrong: {ex.Message}", ex);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }        
    }

    private async Task AddSongsToPlaylist(List<string> songIds, string playlistId, string accessToken)
    {
       
        var request = new HttpRequestMessage(
            HttpMethod.Post, 
            $"https://api.spotify.com/v1/playlists/{playlistId}/tracks"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var requestBody = new
        {
            uris = songIds.Select(id => $"spotify:track:{id}").ToArray()
        };
        
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody), 
            Encoding.UTF8, 
            "application/json"
        );
        
        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to add tracks to the playlist {response.StatusCode}: {response.ReasonPhrase}.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to send Spotify playlist response: json error", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid date format or value: {ex.Message}", ex);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }        
        
        

       

    }
    

    private async Task<string> GetUserId(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                $"https://api.spotify.com/v1/me"
            );
        
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get user ID. Status: {response.StatusCode}. Response: {errorContent}");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonDocument document = JsonDocument.Parse(jsonResponse);
            if (document.RootElement.GetProperty("id").GetString() == null)
                throw new Exception("Failed to retrieve user ID.");
        
            return document.RootElement.GetProperty("id").GetString();
            
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Spotify API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to send Spotify playlist response: json error", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid Format {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve user ID: {ex.Message}");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }   
        
        
     
    }

}