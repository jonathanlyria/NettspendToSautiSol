using System.Collections;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace NettspendToSautiSol;

public class ArtistExpander : Expander<ArtistNode, ArtistEdge>
{
    private readonly DatabaseManager _databaseManager;
    private readonly SpotifyClientCredentials _spotifyClientCredentials = new SpotifyClientCredentials();
    private string _accessToken;
    private DateTime _tokenExpiryTime;
    public ArtistExpander(string apikey, DatabaseManager databaseManager) 
    {
        ApiKey = apikey;
        _databaseManager = databaseManager;
        if (_databaseManager.GetExpanderQueue() == null)
        {
            Queue = new Queue<ArtistNode>();
            Queue.Enqueue(new ArtistNode("Drake"));
        }
        else
        {
            Queue = _databaseManager.GetExpanderQueue();
        }
        _accessToken = _spotifyClientCredentials.GetAccessTokenAsync().Result.AccessToken;
        _tokenExpiryTime = DateTime.UtcNow.AddSeconds(_spotifyClientCredentials.GetAccessTokenAsync().Result.ExpiresIn);
    }
    private async Task RefreshAccessToken()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Trying to Refreshing access token...");
        if (DateTime.UtcNow >= _tokenExpiryTime)
        {
            try
            {
                var tokenData = await _spotifyClientCredentials.GetAccessTokenAsync();
                _accessToken = tokenData.AccessToken;
                _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
               
                Console.WriteLine("Access token refreshed successfully.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing access token: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Access token is still valid for {_tokenExpiryTime - DateTime.UtcNow:c}.");
            Console.ResetColor();
        }
    }
    protected override List<ArtistNode> GetConnections(ArtistNode startingArtistNode)
    {
        RefreshAccessToken().Wait();
        Console.WriteLine($"Getting connections for {startingArtistNode.Name}");
        List<ArtistNode> similarArtistsList = new List<ArtistNode>();

        using (HttpClient client = new HttpClient())
        {
            string url = "http://ws.audioscrobbler.com/2.0/";
            var parameters = new Dictionary<string, string>
            {
                { "method", "artist.getSimilar" },
                { "artist", startingArtistNode.Name },
                { "api_key", ApiKey },
                { "format", "json" },
                { "limit", "20" }
            };

            var response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
               

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                   // Console.WriteLine($"Empty response for artist: {startingArtistNode.Name}");
                    return similarArtistsList;
                }
                
               // Console.WriteLine(response);
                string time = DateTime.UtcNow.ToString();
                _databaseManager.UpdateLastExpanded(startingArtistNode.Name, time);
                
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);
               // Console.WriteLine(jsonResponse);
              //  Console.WriteLine(document);
                
                if (!IsDataValid(document)) return similarArtistsList;
                
                
                var similarArtists = document.RootElement.GetProperty("similarartists")
                    .GetProperty("artist").EnumerateArray();
                

                foreach (var artistData in similarArtists)
                {
                    // Console.WriteLine(artistData);
                    if (!DoesArtistHaveNameAndMatch(artistData)) continue;
                    
                    string foundArtist = artistData.GetProperty("name").GetString();
                    if (foundArtist.Contains('&')) continue;
                    double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                    
                    string? spotifyId = GetSpotifyId(artistData.GetProperty("name").GetString());
                    
                    if (spotifyId == null)
                    {
                        Console.WriteLine($"No spotify id found for artist: {foundArtist}");
                        continue;
                    }

                    if (_databaseManager.DoesIdExist(spotifyId))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Skipping artist: {foundArtist}, because it already exists in the database.");
                        Console.ResetColor();
                        continue;
                    }
                    
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Adding similar artist: {foundArtist}");
                    Console.ResetColor();
                    
             
                    if (!_databaseManager.DoesArtistExist(foundArtist))
                    {
                        _databaseManager.AddArtist(foundArtist, spotifyId);
                    }

                    if (_databaseManager.DoesConnectionExist(startingArtistNode.Name, foundArtist))
                    {
                        _databaseManager.UpdateConnectionStrength(startingArtistNode.Name, foundArtist, match);
                    }
                    else
                    {
                        _databaseManager.AddConnection(startingArtistNode.Name, foundArtist, match);
                    }
                    
                    if (!similarArtistsList.Contains(new ArtistNode(foundArtist)))
                    {
                        similarArtistsList.Add(new ArtistNode(foundArtist));
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        return similarArtistsList;
    }
    private bool ShouldSkipArtist(string name)
    {
        List<ArtistNode> similarArtists = _databaseManager.GetAllSimilarArtistNames(name);
        foreach (ArtistNode artist in similarArtists)
        {
            Console.WriteLine($"Checking {name} against {artist.Name}");
            if (name.IndexOf(artist.Name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"Skipping similar artist: {name}, because it contains {artist.Name}.");
                return true;
            }
        }

        return false;
    }

   private string? GetSpotifyId(string artistName)
    {
        string? spotifyId;
        using (HttpClient client = new HttpClient())
        { 
            Console.WriteLine($"Searching for id for {artistName}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                
            string query = $"q=artist:\"{artistName}\"&type=artist&limit=1";
            string url = $"https://api.spotify.com/v1/search?{query}";
      //      Console.WriteLine(url);
                
            var response = client.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine("Error: " + response.StatusCode);
                return null;
            }
            string jsonResponse = response.Content.ReadAsStringAsync().Result;
            var document = JsonDocument.Parse(jsonResponse); 
            Console.ForegroundColor = ConsoleColor.Green;
            // Console.WriteLine(jsonResponse);
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
          //  Console.WriteLine(document);
            Console.ResetColor();
            if (!document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray().Any())
            {
                Console.WriteLine($"No spotify id found for artist: {artistName}");
                return null;

            }
            
            spotifyId = document.RootElement.GetProperty("artists").GetProperty("items").EnumerateArray().First().GetProperty("id").GetString();
        }

        return spotifyId;
    }

    private bool IsDataValid(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("similarartists", out var similarArtistsElement) || 
            !similarArtistsElement.TryGetProperty("artist", out var artistArrayElement))
        {
            Console.WriteLine("The response does not contain valid 'similarartists' or 'artist' data.");
            return false;
        }

        return true;
    }

    private bool DoesArtistHaveNameAndMatch(JsonElement artistData)
    {
        string? checkArtist = artistData.GetProperty("name").GetString();
        string? checkMatch = artistData.GetProperty("match").GetString();
        return (checkMatch != null || checkArtist != null);
    }
}