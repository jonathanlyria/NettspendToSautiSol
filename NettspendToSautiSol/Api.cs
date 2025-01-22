using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NettspendToSautiSol;
using Newtonsoft.Json;

namespace NettspendToSautiSol
{
    public class PlaylistRequest
    {
        public List<string> Path { get; set; }
        public bool LookForFeatures { get; set; }
        public int TracksPerArtist { get; set; }
        public string PkceToken { get; set; }
    }


    [ApiController]
    [Route("api")]
    public class Api : ControllerBase
    {
        private readonly DatabaseManager _database;
        private ArtistNetwork _artistNetwork;

        public Api(DatabaseManager database, ArtistNetwork artistNetwork)
        {
            _database = database;
            _artistNetwork = artistNetwork;
        }

        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            try
            {
                var artist1Node = new ArtistNode(artist1, _database.GetIdFromName(artist1));
                Console.WriteLine(_database.GetIdFromName(artist1));
                var artist2Node = new ArtistNode(artist2, _database.GetIdFromName(artist2));
                Console.WriteLine(_database.GetIdFromName(artist2));

                var traveller = new ArtistTraveller(artist1Node, artist2Node, _artistNetwork);
                foreach (var id in traveller.Path.Select(a => a.SpotifyId).ToList())
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
                var spotifyAuthorizer = new SpotifyAuthorizer();
                string pkceToken = spotifyAuthorizer.GetAuthorizationPKCEAccessToken();

                if (pkceToken == "Did not finish signing in")
                {
                    return BadRequest(new { Error = "Did not sign in" });
                }

                return Ok(new {PkceToken = pkceToken});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred during authentication." });
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

                foreach (var spotifyId in request.Path)
                {
                    var artistName = _database.GetNameFromId(spotifyId);
                    artists.Add(new ArtistNode(artistName, spotifyId));
                }

                var playlistCreator = new PlaylistCreator(
                    artists,
                    request.TracksPerArtist,
                    request.LookForFeatures,
                    request.PkceToken
                );

                var songIds = playlistCreator.GetSongs();
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
                var exists = _database.DoesArtistExist(artistName);
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

                string path = "C:\\Users\\jl154125\\Source\\Repos\\jonathanlyria\\NettspendToSautiSol\\NettspendToSautiSol\\Issues.txt";

                // Create the file if it doesn't exist
                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose(); // Ensure the file handle is closed
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

                string path = "C:\\Users\\jl154125\\Source\\Repos\\jonathanlyria\\NettspendToSautiSol\\NettspendToSautiSol\\RequestedArtists.txt";

                // Create the file if it doesn't exist
                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path).Dispose(); // Ensure the file handle is closed
                }

                System.IO.File.AppendAllText(path, $"{artistName}{Environment.NewLine}");

                return Ok($"Artist request received for: {artistName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
