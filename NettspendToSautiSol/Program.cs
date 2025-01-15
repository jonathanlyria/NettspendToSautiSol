using System.Text.Json;
namespace NettspendToSautiSolClient
{
    class Program
    {
        private static readonly HttpClient Client = new();
        

        private const string
            BaseUrl = "http://localhost:8888/callback/"; 

        static async Task Main(string[] args)
        {
            string artist1;
            string artist2;
            Console.WriteLine("NettspendToSautiSol");
            await AuthenticateUser();
            do
            {
                Console.WriteLine("Enter Artist 1");
                artist1 = Console.ReadLine();
            }while (await CheckArtistExists(artist1));
            
            do
            {
                Console.WriteLine("Enter Artist 2");
                artist2 = Console.ReadLine();
            }while (await CheckArtistExists(artist2));

            FindArtistPath(artist1, artist2);
            CreatePlaylistAsync();



        }
        
        static async Task AuthenticateUser()
        {
            HttpResponseMessage response = await Client.PostAsync($"{BaseUrl}/authenticate-user", null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("User authenticated successfully.");
            }
            else
            {
                Console.WriteLine($"Error authenticating user: {response.StatusCode}");
            }
        }

        static async Task<bool> CheckArtistExists(string artistName)
        {
            var response = await Client.GetAsync($"{BaseUrl}/artists-exist?artistName={Uri.EscapeDataString(artistName)}");
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseBody);
                if (result.RootElement.GetProperty("ArtistExist").GetBoolean())
                {
                    Console.WriteLine("The artist exists in the database.");
                    return true;
                }
                Console.WriteLine("The artist does not exist in the database.");
            }
            else
            {
                Console.WriteLine($"Error checking artist: {response.StatusCode}");
            }

            return false;
        }

        static async Task FindArtistPath(string artist1, string artist2)
        {
            HttpResponseMessage response = await Client.GetAsync($"{BaseUrl}/find-path?artist1={Uri.EscapeDataString(artist1)}&artist2={Uri.EscapeDataString(artist2)}");
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseBody);

                Console.WriteLine("Path found:");
                foreach (var artistName in result.RootElement.GetProperty("path").EnumerateArray())
                {
                    Console.WriteLine(artistName);
                }
            }
            else
            {
                Console.WriteLine($"Error finding path: {response.StatusCode}");
            }
        }

        public static async Task<bool> CreatePlaylistAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUrl = $"{BaseUrl}/create-playlist";
                    
                    var response = await client.GetAsync(requestUrl);
            
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Playlist created successfully: " + responseBody);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create playlist. Status: {response.StatusCode}");
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return false;
        }

    }
}
       