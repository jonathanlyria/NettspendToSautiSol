using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestFrontend
{
    public class Expander
    {
        private static readonly string ApiKey = "00751a650c0182344603b9252c66d416"; //last.fm api call
        public Queue<string> ArtistQueue = new Queue<string>(); // queue of artists for breadth-first search

        private Dictionary<string, Dictionary<string, double>> Network =
            new Dictionary<string, Dictionary<string, double>>(); // 2D dictionary which represents the network

        public void Expand(string startingArtist, int numIterations) //breadth-first search 
        {
            Network.Add(startingArtist, new Dictionary<string, double>());
            ArtistQueue.Enqueue(startingArtist);
            for (int iteration = 0; iteration < numIterations && ArtistQueue.Count > 0; iteration++) // finds the artists within numIterations degrees of seperataion of startingArtist
            {
                int queueLength = ArtistQueue.Count;
                for (int i = 0; i < queueLength; i++)
                {
                    string currentArtist = ArtistQueue.Dequeue();
                    var connections = GetSimilarArtists(currentArtist); //Returns a list of strings and doubles

                    foreach (var (name, match) in connections)
                    {
                        ArtistQueue.Enqueue(name); //enqueues the connections
                    }
                }
            }
        }

        private List<(string, double)> GetSimilarArtists(string artist)
        {
            List<(string, double)> similarArtistsList = new();
            using HttpClient client = new();
            string url = "http://ws.audioscrobbler.com/2.0/"; //lastfm base url
            var parameters = new Dictionary<string, string>
            {
                { "method", "artist.getSimilar" },
                { "artist", artist },
                { "api_key", ApiKey },
                { "format", "json" },
                { "limit", "5" }
            }; 

            HttpResponseMessage response = client.GetAsync(BuildUrlWithParams(url, parameters)).Result; 
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return similarArtistsList;
            }

            string jsonResponse = response.Content.ReadAsStringAsync().Result;
            JsonDocument document = JsonDocument.Parse(jsonResponse);

            if (!document.RootElement.TryGetProperty("similarartists", out JsonElement similarArtistsElement) ||
                !similarArtistsElement.TryGetProperty("artist", out JsonElement artistArray)) // if the resposne does not have
                                                                                              // the properties similarartists, or if the property similar artists does not have artists
            {
                return similarArtistsList;
            }

            foreach (var artistData in artistArray.EnumerateArray())
            {
                string name = artistData.GetProperty("name").GetString();
                double match = Convert.ToDouble(artistData.GetProperty("match").GetString());

                if (string.IsNullOrWhiteSpace(name)) continue;

                bool isDuplicate = false;
                if (Network.TryGetValue(artist, out var connections))
                {
                    if (connections.ContainsKey(name))
                    {
                        Console.WriteLine($"Skipping {name}, already exists in network."); 
                        isDuplicate = true;
                    }
                }

                if (isDuplicate) continue;

                Console.WriteLine($"Adding {name}");

                similarArtistsList.Add((name, match));
                
                if (!Network.ContainsKey(name))
                    Network[name] = new Dictionary<string, double>(); 

                Network[artist][name] = match;
                Network[name][artist] = match;
            }

            return similarArtistsList;
        }

        public void PrintNetwork()
        {
            Console.WriteLine("Artist Network:");
            foreach (var artist in Network)
            {
                Console.WriteLine($"{artist.Key}:");
                foreach (var connection in artist.Value)
                {
                    Console.WriteLine($"  -> {connection.Key} (Match: {connection.Value:F2})");
                }
            }
        }

        private static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            return $"{url}?{string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))}";
        }
    }
}
