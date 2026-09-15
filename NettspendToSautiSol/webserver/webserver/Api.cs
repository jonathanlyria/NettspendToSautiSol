using DatabaseServices.Interfaces;
using ExternalWebServices.Interfaces;
using ExternalWebServices;
using GlobalTypes;
using Microsoft.AspNetCore.Mvc;

namespace webserver
{

    public class PlaylistRequest
    {
        public string Code { get; set; } = string.Empty;
        public List<string> Path { get; set; } = [];
        public string State { get; set; } = string.Empty;
    }


    [ApiController]
    [Route("api")]
    public class Api(
        IArtistNetwork artistNetwork,
        IWebServerDatabaseService webServerDatabaseService,
        ISpotifyPkceCodeAuthorizer spotifyPkceCodeAuthorizer,
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
            catch (Exception)
            {
                return StatusCode(500, new { Error = "An error occurred while checking the artist." });
            }
        }
 
        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            if (string.IsNullOrWhiteSpace(artist1) || string.IsNullOrWhiteSpace(artist2))
                return BadRequest(new { Error = "Provide both artist names." });

            try
            {
                if (!webServerDatabaseService.IsArtistInDbByName(artist1) ||
                    !webServerDatabaseService.IsArtistInDbByName(artist2))
                    return NotFound(new { Error = "One or both artists are absent from the graph." });

                ArtistNode artist1Node = new ArtistNode(artist1, webServerDatabaseService.GetIdFromName(artist1));
                ArtistNode artist2Node = new ArtistNode(artist2, webServerDatabaseService.GetIdFromName(artist2));
               
                List<ArtistNode> path = artistNetwork.FindPathWithDijkstras(artist1Node, artist2Node);
                if (path.Count == 0)
                    return NotFound(new { Error = "No route connects these artists." });
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
            catch (Exception)
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
            catch (Exception)
            {
                return StatusCode(500, new { Error = "Authentication initialization failed" });
            }
        }
        

        [HttpPost("create-playlist")]
        public async Task<IActionResult> CreatePlaylist([FromBody] PlaylistRequest request)
        {
            string stage = "verifying Spotify sign-in";
            try
            {
                if (request.Path is null || request.Path.Count == 0)
                {
                    Console.WriteLine("No path provided");
                    return BadRequest(new { Error = "Path is empty" });
                }

                if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
                    return BadRequest(new { Error = "A Spotify authorisation code and state are required." });

                string accessToken = await spotifyPkceCodeAuthorizer.ExchangeCode(request.Code, request.State);

                List<ArtistNode> artists = new();

                foreach (string spotifyId in request.Path)
                {
                    string artistName = webServerDatabaseService.GetNameFromId(spotifyId);
                    artists.Add(new ArtistNode(artistName, spotifyId));
                }
                
                stage = "selecting songs";
                List<string> songIds = await getPlaylistSongsService.GetPlaylistSongIds(artists);
                stage = "creating the playlist";
                string playlistLink = await createPlaylistService.CreatePlaylist(songIds, artists.First().Name, 
                    artists.Last().Name, accessToken);
                
                return Ok(new { Message = "Playlist created successfully.", PlaylistLink = playlistLink});
            }
            catch (SpotifyApiException ex)
            {
                Console.WriteLine($"Playlist failed while {stage}: {ex.Message}");
                return StatusCode(502, new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Playlist failed while {stage}: {ex.GetType().Name}; cause: {ex.GetBaseException().GetType().Name}");
                string advice = stage == "creating the playlist"
                    ? "Check your Spotify library before retrying: a playlist may already have been created."
                    : "Refresh the page and sign in to Spotify again before retrying.";
                return StatusCode(500, new { Error = $"An error occurred while {stage}. {advice}" });
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

                string path = Environment.GetEnvironmentVariable("ARTIST_REPORT_PATH")
                    ?? System.IO.Path.Combine(AppContext.BaseDirectory, "reports", "artist-issues.txt");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);

                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose();  
                }

                System.IO.File.AppendAllText(path, $"{issue}{Environment.NewLine}");

                return Ok($"Issue reported for artist: {issue}");
            }
            catch (Exception)
            {
                return StatusCode(500, "The issue could not be recorded.");
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
            catch (Exception)
            {
                return StatusCode(500, new { 
                    Error = "Failed to retrieve artists"
                });
            }
        }


    }
}
