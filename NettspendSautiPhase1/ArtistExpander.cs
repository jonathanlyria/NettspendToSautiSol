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
                string jsonResponse = response.Content.ReadAsStringAsync().Result;
                JsonDocument document = JsonDocument.Parse(jsonResponse);

                var similarArtists = document.RootElement.GetProperty("similarartists")
                    .GetProperty("artist").EnumerateArray();

                foreach (var artistData in similarArtists)
                {
                    string foundArtist = artistData.GetProperty("name").GetString();
                    string foundIdentifier = artistData.GetProperty("mbid").GetString();
                    double foundMatch = Convert.ToDouble(artistData.GetProperty("match").GetString());
                    
                    ArtistNode foundArtistNode = new ArtistNode(foundIdentifier, foundArtist);

                    // Skip duplicates and self-references
                    if (ShouldSkipArtist(foundArtist, foundIdentifier)) continue;

                    Console.WriteLine($"Adding similar artist: {foundArtist}");

                    double match = Convert.ToDouble(artistData.GetProperty("match").GetString());

                    // Save to database
                    _databaseManager.AddArtist(startingArtistNode.Identifier, startingArtistNode.Name);
                    _databaseManager.AddArtist(foundIdentifier, foundArtist);
                    AddConnection(startingArtistNode, foundArtistNode, match); 
                    
                    // Add to the result list
                    var similarArtistNode = new ArtistNode(foundIdentifier, foundArtist);
                    if (!similarArtistsList.Contains(similarArtistNode))
                    {
                        similarArtistsList.Add(similarArtistNode);
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

    protected override void AddNode(ArtistNode node)
    {
        if (!ShouldSkipArtist(node.Name, node.Identifier))
        {
            _databaseManager.AddArtist(node.Identifier, node.Name);
        }
    } 
 
      protected override void AddConnection(ArtistNode artist1, ArtistNode artist2, double weight)
       {
           if (artist1 == null || artist2 == null || _databaseManager == null)
               return;
       
           // Add the connection to the database with placeholder logic
           var forwardConnection = _databaseManager.GetConnection(artist1.Identifier, artist2.Identifier);
           var backwardConnection = _databaseManager.GetConnection(artist2.Identifier, artist1.Identifier);
       
           // Check if the forward connection is a placeholder
           bool isForwardConnectionPlaceholder = forwardConnection != null && _databaseManager.IsPlaceholder(artist1, artist2);
       
           // Check if the backward connection is a placeholder
       
           // Case 1: Both forward and backward connections do not exist
           if (forwardConnection == null && backwardConnection == null)
           {
               // Add the forward and backward connections
               _databaseManager.AddConnection(artist1.Identifier, artist2.Identifier, weight, false);
               _databaseManager.AddConnection(artist2.Identifier, artist1.Identifier, weight, true);
           }
           // Case 2: Only the backward connection exists
           else if (forwardConnection == null && backwardConnection != null)
           {
               // Add the forward connection
               _databaseManager.AddConnection(artist1.Identifier, artist2.Identifier, weight, false);
           }
           // Case 3: Only the forward connection exists
           else if (forwardConnection != null && backwardConnection == null)
           {
               // Add the backward connection
               _databaseManager.AddConnection(artist2.Identifier, artist1.Identifier, weight, true);
           }
           // Case 4: Forward connection exists and is a placeholder
           else if (forwardConnection != null && isForwardConnectionPlaceholder)
           {
               _databaseManager.RemoveConnection(artist1.Identifier, artist2.Identifier);
               _databaseManager.AddConnection(artist1.Identifier, artist2.Identifier, weight, false);
           }
       }

   
    private bool ShouldSkipArtist(string name, string identifier)
    {
        // Check the database for existing artist based on name and identifier
        var existingArtist = _databaseManager.GetArtistByIdentifier(identifier);
        if (existingArtist != null)
        {
            Console.WriteLine($"Skipping artist: {name}, because it already exists in the database with identifier {identifier}.");
            return true;
        }
        
        List<ArtistNode> similarArtists = _databaseManager.GetArtistsByName(name);
        foreach (ArtistNode artist in similarArtists)
        {
            if (name.IndexOf(artist.Name, StringComparison.OrdinalIgnoreCase) >= 0 && !name.Contains('&'))
            {
                Console.WriteLine($"Skipping similar artist: {name}, because it contains {artist.Name}.");
                return true;
            }
        }

        return false;
    }
}