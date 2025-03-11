using DatabaseServices.Interfaces;
using ExternalWebServices.Interfaces;
using GlobalTypes;
using Microsoft.AspNetCore.Mvc;

namespace webserver
{

    public class PlaylistRequest
    {
        public string Code { get; set; }
        public List<string> Path { get; set; }
        public string State { get; set; }
    }


    [ApiController]
    [Route("api")]
    public class Api(
        IArtistNetwork artistNetwork,
        IWebServerDatabaseService webServerDatabaseService,
        ISpotifyPkceCodeAuthorizer spotifyPkceCodeAuthorizer,
        ISpotifyClientCredentialAuthorizer spotifyClientCredentials,
        IGetPlaylistSongsService getPlaylistSongsService,
        ICreatePlaylistService createPlaylistService)
        : ControllerBase
    {
        
        [HttpGet("artists-exist")]
        public IActionResult ArtistsExists([FromQuery] string artistName)
        {
            try
            {
                bool exists = webServerDatabaseService.IsArtistInDbByName(artistName);
                return Ok(new { ArtistExist = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while checking the artist." });
            }
        }
 
        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            try
            {
                ArtistNode artist1Node = new ArtistNode(artist1, webServerDatabaseService.GetIdFromName(artist1));
                ArtistNode artist2Node = new ArtistNode(artist2, webServerDatabaseService.GetIdFromName(artist2));
               
                List<ArtistNode> path = artistNetwork.FindPathWithDijkstras(artist1Node, artist2Node);
                Console.WriteLine($"TRYING TO TRAVEL BETWEEN {artist1Node.Name} and {artist2Node.Name}");
                foreach (string id in path.Select(a => a.SpotifyId).ToList())
                {
                    Console.WriteLine(id);
                }
                
                return Ok(new
                {
                    PathId = path.Select(a => a.SpotifyId).ToList(),
                    PathName = path.Select(a => a.Name).ToList(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while finding the path." });
            }
        }
        
        [HttpGet("authenticate-user")]
        public async Task<IActionResult> Authenticate()
        {
            try
            {
                (string authUrl, string state) = await spotifyPkceCodeAuthorizer.GetAuthorizationUrl();
                return Ok(new { AuthUrl = authUrl, State = state });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Authentication initialization failed" });
            }
        }
        

        [HttpPost("create-playlist")]
        public async Task<IActionResult> CreatePlaylist([FromBody] PlaylistRequest request)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                string accessToken = await spotifyPkceCodeAuthorizer.ExchangeCode(request.Code, request.State);
                Console.WriteLine($"PKCE TOKEN: {accessToken}");
                Console.ResetColor();

                if (!request.Path.Any())
                {
                    Console.WriteLine("No path provided");
                    return BadRequest(new { Error = "Path is empty" });
                }

                List<ArtistNode> artists = new();

                foreach (string spotifyId in request.Path)
                {
                    string artistName = webServerDatabaseService.GetNameFromId(spotifyId);
                    artists.Add(new ArtistNode(artistName, spotifyId));
                }
                
                List<string> songIds = await getPlaylistSongsService.GetPlaylistSongIds(artists);
                Console.WriteLine("Creating playlist");
                foreach (string songId in songIds)
                    Console.WriteLine(songId);
                string playlistLink = await createPlaylistService.CreatePlaylist(songIds, artists.First().Name, 
                    artists.Last().Name, accessToken);
                
                return Ok(new { Message = "Playlist created successfully.", PlaylistLink = playlistLink});
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { Error = "An error occurred while creating the playlist.", Details = ex.Message });
            }
        } 


        [HttpGet("report-issue")]
        public IActionResult ReportIssue([FromQuery] string issue)
        {
            try
            {
                if (string.IsNullOrEmpty(issue))
                {
                    return BadRequest("Artist name cannot be empty.");
                }

                string path = "/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/webserver/reportissue.txt";

                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose();  
                }

                System.IO.File.AppendAllText(path, $"{issue}{Environment.NewLine}");

                return Ok($"Issue reported for artist: {issue}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("get-all-artists")]
        public IActionResult GetAllArtists()
        {
            try
            {
                var artists = webServerDatabaseService.GetAllArtistNodes();
                return Ok(new { 
                    Artists = artists.Select(a => new {
                        name = a.Name,
                        spotifyId = a.SpotifyId
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    Error = "Failed to retrieve artists", 
                    Details = ex.Message 
                });
            }
        }


    }
}
