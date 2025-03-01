using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NettspendToSautiSol;
// Citation of ASP.NET core 
// Citation of ApiController
namespace NettspendToSautiSol
{
    public class PlaylistRequest
    {
        public List<string> Path { get; set; }
        public string PkceToken { get; set; }
    }
    
    public class ExchangeCodeRequest
    {
        public string Code { get; set; }
        public string State { get; set; }
    }


    [ApiController]
    [Route("api")]
    public class Api(
        ArtistNetwork artistNetwork,
        SpotifyAuthorizeWithPKCEAuthenticator spotifyAuthorizeWithPkceAuthenticator)
        : ControllerBase
    {
        
        [HttpGet("authenticate-user")]
        public IActionResult Authenticate()
        {
            try
            {
                (string authUrl, string state) = spotifyAuthorizeWithPkceAuthenticator.GetAuthorizationUrl();
                return Ok(new { AuthUrl = authUrl, State = state });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Authentication initialization failed" });
            }
        }

        [HttpPost("exchange-code")]
        public IActionResult ExchangeCode([FromBody] ExchangeCodeRequest request)
        {
            try
            {
                string accessToken = spotifyAuthorizeWithPkceAuthenticator.ExchangeCode(request.Code, request.State);
                return Ok(new { PkceToken = accessToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    Error = "Failed to exchange authorization code",
                    Details = ex.Message 
                });
            }
        }
        
        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            try
            {
                Console.WriteLine("Finding path from " + artist1 + " to " + artist2);
                ArtistNode artist1Node = new ArtistNode(artist1, ArtistNetworkDatabaseManager.GetIdFromName(artist1));
                Console.WriteLine(ArtistNetworkDatabaseManager.GetIdFromName(artist1));
                ArtistNode artist2Node = new ArtistNode(artist2, ArtistNetworkDatabaseManager.GetIdFromName(artist2));
                Console.WriteLine(ArtistNetworkDatabaseManager.GetIdFromName(artist2));
                Console.WriteLine($"b4 pathFinder");
                artistNetwork.DisplayAllConnections();

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


        [HttpPost("create-playlist")]
        public IActionResult CreatePlaylist([FromBody] PlaylistRequest request)
        {
            try
            {
                if (request.Path == null || !request.Path.Any())
                {
                    return BadRequest(new { Error = "Path is empty" });
                }

                List<ArtistNode> artists = new();

                foreach (string spotifyId in request.Path)
                {
                    string artistName = ArtistNetworkDatabaseManager.GetNameFromId(spotifyId);
                    artists.Add(new ArtistNode(artistName, spotifyId));
                }

                PlaylistCreator playlistCreator = new PlaylistCreator(
                    artists,
                    request.PkceToken
                );

                List<string> songIds = playlistCreator.GetSongs();
                playlistCreator.AddToPlaylist(songIds);
                string playlistLink = playlistCreator.GetPlaylistLink();
                return Ok(new { Message = "Playlist created successfully.", PlaylistLink = playlistLink});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while creating the playlist.", Details = ex.Message });
            }
        } 

        [HttpGet("artists-exist")]
        public IActionResult ArtistsExists([FromQuery] string artistName)
        {
            try
            {
                bool exists = ArtistNetworkDatabaseManager.IsArtistInDbByName(artistName);
                return Ok(new { ArtistExist = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while checking the artist." });
            }
        }

        [HttpGet("report-issue")]
        public IActionResult ReportIssue([FromQuery] string artistName)
        {
            try
            {
                if (string.IsNullOrEmpty(artistName))
                {
                    return BadRequest("Artist name cannot be empty.");
                }

                string path = "/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/webserver/reportissue.txt";

                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose();  
                }

                System.IO.File.AppendAllText(path, $"{artistName}{Environment.NewLine}");

                return Ok($"Issue reported for artist: {artistName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("request-artist")]
        public IActionResult RequestArtist([FromQuery] string artistName)
        {
            try
            {
                if (string.IsNullOrEmpty(artistName))
                {
                    return BadRequest("Artist name cannot be empty.");
                }

                string path = "/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/webserver/requestartists.txt";

                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose(); 
                }

                System.IO.File.AppendAllText(path, $"{artistName}{Environment.NewLine}");

                return Ok($"Artist request received for: {artistName}");
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
                var artists = ArtistNetworkDatabaseManager.GetAllArtistNodes();
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
