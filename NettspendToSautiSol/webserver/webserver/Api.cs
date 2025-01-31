using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NettspendToSautiSol;

namespace NettspendToSautiSol
{
    public class PlaylistRequest
    {
        public List<string> Path { get; set; }
        public bool LookForFeatures { get; set; }
        public int TracksPerArtist { get; set; }
        public string PkceToken { get; set; }
    }
    
    public class ExchangeCodeRequest
    {
        public string Code { get; set; }
        public string State { get; set; }
    }


    [ApiController]
    [Route("api")]
    public class Api : ControllerBase
    {
        private readonly DatabaseManager _database;
        private readonly ArtistNetwork _artistNetwork;
        private readonly SpotifyAuthorizer _spotifyAuthorizer;

        public Api(DatabaseManager database, ArtistNetwork artistNetwork, SpotifyAuthorizer spotifyAuthorizer)
        {
            _database = database;
            _artistNetwork = artistNetwork;
            _spotifyAuthorizer = spotifyAuthorizer;
        }

        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            try
            {
                ArtistNode artist1Node = new ArtistNode(artist1, _database.GetIdFromName(artist1));
                Console.WriteLine(_database.GetIdFromName(artist1));
                ArtistNode artist2Node = new ArtistNode(artist2, _database.GetIdFromName(artist2));
                Console.WriteLine(_database.GetIdFromName(artist2));

                ArtistTraveller traveller = new ArtistTraveller(artist1Node, artist2Node, _artistNetwork);
                foreach (string id in traveller.Path.Select(a => a.SpotifyId).ToList())
                {
                    Console.WriteLine(id);
                }
                
                return Ok(new
                {
                    PathId = traveller.Path.Select(a => a.SpotifyId).ToList(),
                    PathName = traveller.Path.Select(a => a.Name).ToList(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while finding the path." });
            }
        }


        [HttpGet("authenticate-user")]
        public IActionResult Authenticate()
        {
            try
            {
                (string authUrl, string state) = _spotifyAuthorizer.GetAuthorizationUrl();
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
                string accessToken = _spotifyAuthorizer.ExchangeCode(request.Code, request.State);
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
                    string artistName = _database.GetNameFromId(spotifyId);
                    artists.Add(new ArtistNode(artistName, spotifyId));
                }

                PlaylistCreator playlistCreator = new PlaylistCreator(
                    artists,
                    request.TracksPerArtist,
                    request.LookForFeatures,
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
                bool exists = _database.DoesArtistExist(artistName);
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
                var artists = _database.GetAllArtists();
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
