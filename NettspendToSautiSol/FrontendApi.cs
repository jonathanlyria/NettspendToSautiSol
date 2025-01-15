using Microsoft.AspNetCore.Mvc;
using NettspendToSautiSol;

namespace NettspendToSautiSol.Controllers
{
    public class UserSessionState
    {
        public string PkceToken { get; set; }
        public List<ArtistNode> Path { get; set; } = new();
    }

    [ApiController]
    [Route("[controller]")]
    public class ArtistController : ControllerBase
    {
        private readonly DatabaseManager _database;
        private readonly UserSessionState _sessionState;

        public ArtistController(DatabaseManager database, UserSessionState sessionState)
        {
            _database = database;
            _sessionState = sessionState;
        }

        [HttpGet("find-path")]
        public IActionResult FindPath([FromQuery] string artist1, [FromQuery] string artist2)
        {
            try
            {
                var artistNetwork = new ArtistNetwork(_database);
                var artist1Node = new ArtistNode(artist1);
                var artist2Node = new ArtistNode(artist2);

                var traveller = new ArtistTraveller(artist1Node, artist2Node, artistNetwork);
                traveller.Traverse();
                _sessionState.Path = traveller.Path;

                return Ok(new
                {
                    Path = _sessionState.Path.Select(a => a.Name).ToList(),
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
                _sessionState.PkceToken = spotifyAuthorizer.GetAuthorizationPKCEAccessToken();

                if (_sessionState.PkceToken == "Did not finish signing in")
                {
                    return BadRequest(new { Error = "Did not sign in" });
                }

                return Ok(new { Authenticated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred during authentication." });
            }
        }

        [HttpPost("create-playlist")]
        public IActionResult CreatePlaylist()
        {
            try
            {
                if (_sessionState.Path == null || !_sessionState.Path.Any())
                {
                    return BadRequest(new { Error = "Path is empty" });
                }

                var playlistCreator = new PlaylistCreator(
                    _sessionState.Path, 
                    3, 
                    true, 
                    _sessionState.Path.First().Name, 
                    _sessionState.Path.Last().Name, 
                    _sessionState.PkceToken
                );

                var songIds = playlistCreator.GetSongs();
                playlistCreator.AddToPlaylist(songIds);

                return Ok(new { Message = "Playlist created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while creating the playlist." });
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
    }
}
