using System.Text.Json;

namespace NettspendSautiPhase1;

public class ArtistExpander : Expander<ArtistNode, ArtistEdge>
{
    private readonly DatabaseManager _databaseManager;

    public ArtistExpander(string apikey, DatabaseManager databaseManager) 
    {
        ApiKey = apikey;
        _databaseManager = databaseManager;
    }

    protected override List<ArtistNode> GetConnections(ArtistNode startingArtistNode)
    {
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
                { "limit", "5" }
            };

            var response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                string time = DateTime.UtcNow.ToString();
                _databaseManager.UpdateLastExpanded(startingArtistNode.Name, time);
                
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);
                Console.WriteLine(jsonResponse);
                Console.WriteLine(document);
                
                if (!IsDataValid(document)) return similarArtistsList;
                
                var similarArtists = document.RootElement.GetProperty("similarartists")
                    .GetProperty("artist").EnumerateArray();
                

                foreach (var artistData in similarArtists)
                {
                    Console.WriteLine(artistData);
                    if (!DoesArtistHaveNameAndMatch(artistData)) continue;
                    
                    string foundArtist = artistData.GetProperty("name").GetString();
                    double match = Convert.ToDouble(artistData.GetProperty("match").GetString());
                    
                    ArtistNode foundArtistNode = new ArtistNode(foundArtist);
                    if (ShouldSkipArtist(foundArtist)) continue;

                    Console.WriteLine($"Adding similar artist: {foundArtist}");
                    
             
                    if (!_databaseManager.DoesArtistExist(foundArtist))
                    {
                        _databaseManager.AddArtist(foundArtist);
                    }

                    if (_databaseManager.DoesConnectionExist(startingArtistNode.Name, foundArtist))
                    {
                        _databaseManager.UpdateConnectionStrength(startingArtistNode.Name, foundArtistNode.Name, match);
                    }
                    else
                    {
                        _databaseManager.AddConnection(startingArtistNode.Name, foundArtistNode.Name, match);
                    }
                    
                    if (!similarArtistsList.Contains(foundArtistNode))
                    {
                        similarArtistsList.Add(foundArtistNode);
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
    protected override void AddConnection(ArtistNode artist1, ArtistNode artist2, double weight)
    {
          
    }
    protected override void AddNode(ArtistNode artist)
    {
     
    }
    private bool ShouldSkipArtist(string name)
    {
        List<ArtistNode> similarArtists = _databaseManager.GetAllSimilarArtistNames(name);
        foreach (ArtistNode artist in similarArtists)
        {
            if (name.IndexOf(artist.Name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"Skipping similar artist: {name}, because it contains {artist.Name}.");
                return true;
            }
        }

        return false;
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