using System.Net;
using System.Text.Json;
using ExternalWebServices;
using ExternalWebServices.Interfaces;
using GlobalTypes;

int passed = 0;
await Check("Current playlist endpoints, ordered 100-item batches and public scope", async () =>
{
    using var handler = new FakeHandler(r => Json(r.Uri.EndsWith("/me/playlists") ? new { id = "created" } : (object)new { snapshot_id = "ok" }));
    using var client = new HttpClient(handler);
    var ids = Enumerable.Range(0, 205).Select(i => i.ToString()).ToList();
    var link = await new CreatePlaylistService(client).CreatePlaylist(ids, "A", "B", "user-token");
    Equal(link, "https://open.spotify.com/playlist/created");
    Equal(handler.Requests.Count, 4);
    Equal(handler.Requests[0].Uri, "https://api.spotify.com/v1/me/playlists");
    using var body = JsonDocument.Parse(handler.Requests[0].Body);
    True(body.RootElement.GetProperty("public").GetBoolean());
    var actual = new List<string>();
    foreach (var request in handler.Requests.Skip(1))
    {
        Equal(request.Uri, "https://api.spotify.com/v1/playlists/created/items");
        using var document = JsonDocument.Parse(request.Body);
        var uris = document.RootElement.GetProperty("uris").EnumerateArray().Select(x => x.GetString()!).ToArray();
        True(uris.Length <= 100);
        actual.AddRange(uris);
    }
    True(actual.SequenceEqual(ids.Select(x => "spotify:track:" + x)));
    True(handler.Requests.All(r => r.Method == "POST" && r.Auth == "Bearer user-token"));
    True(client.DefaultRequestHeaders.Authorization is null);
});
await Check("Empty selection never creates a playlist", async () =>
{
    using var handler = new FakeHandler(_ => throw new Exception("Unexpected HTTP request"));
    using var client = new HttpClient(handler);
    await Fails(() => new CreatePlaylistService(client).CreatePlaylist([], "A", "B", "user"), "No playlist was created");
    Equal(handler.Requests.Count, 0);
});
await Check("Partial write reports the created playlist without retrying", async () =>
{
    using var handler = new FakeHandler(r => r.Uri.EndsWith("/me/playlists")
        ? Json(new { id = "partial" }) : Json(new { error = "private provider body" }, HttpStatusCode.Forbidden));
    using var client = new HttpClient(handler);
    var message = await Fails(() => new CreatePlaylistService(client).CreatePlaylist(["song"], "A", "B", "user"), "https://open.spotify.com/playlist/partial");
    True(!message.Contains("private provider body"));
    Equal(handler.Requests.Count, 2);
});
await Check("Search filters wrong artist IDs, duplicates and unplayable songs", async () =>
{
    using var handler = new FakeHandler(_ => Tracks(
        Track("one", "One", ["a"]), Track("wrong", "Wrong", ["same-name-different-id"]),
        Track("duplicate", "One (Remastered)", ["a"]), Track("blocked", "Blocked", ["a"], false)));
    using var client = new HttpClient(handler);
    var ids = await new GetPlaylistSongsService(new TokenStub(), client).GetPlaylistSongIds([new ArtistNode("A", "a")]);
    Equal(ids.Count, 1);
    True(ids[0] is "one" or "duplicate");
    True(handler.Requests[0].Uri.Contains("/v1/search?") && handler.Requests[0].Uri.Contains("limit=10"));
    Equal(handler.Requests[0].Auth, "Bearer catalogue-token");
    True(client.DefaultRequestHeaders.Authorization is null);
});
await Check("Collaboration appears between its two artists", async () =>
{
    int call = 0;
    using var handler = new FakeHandler(_ => ++call switch
    {
        1 => Tracks(Track("bridge", "Together", ["a", "b"])),
        2 => Tracks(Track("solo-a", "First", ["a"])),
        _ => Tracks(Track("solo-b", "Last", ["b"]))
    });
    using var client = new HttpClient(handler);
    var ids = await new GetPlaylistSongsService(new TokenStub(), client).GetPlaylistSongIds([new("A", "a"), new("B", "b")]);
    True(ids.SequenceEqual(new[] { "solo-a", "bridge", "solo-b" }));
});
await Check("Rate limits propagate instead of silently skipping artists", async () =>
{
    using var handler = new FakeHandler(_ => Json(new { error = "sensitive-body" }, HttpStatusCode.TooManyRequests));
    using var client = new HttpClient(handler);
    string message = await Fails(() => new GetPlaylistSongsService(new TokenStub(), client).GetPlaylistSongIds([new("A", "a")]), "HTTP 429");
    True(!message.Contains("sensitive-body"));
    Equal(handler.Requests.Count, 1);
});
await Check("No matching songs is an explicit error", async () =>
{
    using var handler = new FakeHandler(_ => Tracks());
    using var client = new HttpClient(handler);
    await Fails(() => new GetPlaylistSongsService(new TokenStub(), client).GetPlaylistSongIds([new("A", "a")]), "No playlist was created");
});
await Check("Client credentials do not leak into the PKCE exchange; states are single-use", async () =>
{
    using var handler = new FakeHandler(_ => Json(new { access_token = "test-token", expires_in = 3600 }));
    using var client = new HttpClient(handler);
    await new SpotifyClientCredentialAuthorizer("test-id", "test-secret", client).GetAccessToken();
    var pkce = new SpotifyPkceCodeAuthorizer(client, "test-id", "http://127.0.0.1:8080/callback.html");
    var auth = await pkce.GetAuthorizationUrl();
    Equal(await pkce.ExchangeCode("test-code", auth.State), "test-token");
    True(handler.Requests[0].Auth?.StartsWith("Basic ") == true);
    True(handler.Requests[1].Auth is null);
    True(client.DefaultRequestHeaders.Authorization is null);
    await Fails(() => pkce.ExchangeCode("test-code", auth.State), "already been used");
    Equal(handler.Requests.Count, 2);
});
await Check("Provider sign-in errors never expose response bodies", async () =>
{
    using var handler = new FakeHandler(_ => Json(new { error = "secret-token-response" }, HttpStatusCode.BadRequest));
    using var client = new HttpClient(handler);
    var pkce = new SpotifyPkceCodeAuthorizer(client, "test-id", "http://127.0.0.1:8080/callback.html");
    var auth = await pkce.GetAuthorizationUrl();
    var message = await Fails(() => pkce.ExchangeCode("code", auth.State), "HTTP 400");
    True(!message.Contains("secret-token-response"));
});
Console.WriteLine($"{passed} Spotify checks passed. No live Spotify account was modified.");

async Task Check(string name, Func<Task> test) { await test(); passed++; Console.WriteLine($"PASS {name}"); }
static void True(bool value) { if (!value) throw new Exception("Assertion failed"); }
static void Equal<T>(T actual, T expected) { if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new Exception($"Expected {expected}, got {actual}"); }
static async Task<string> Fails(Func<Task> action, string expected)
{
    try { await action(); } catch (SpotifyApiException e) { True(e.Message.Contains(expected)); return e.Message; }
    throw new Exception("Expected a SpotifyApiException");
}
static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(JsonSerializer.Serialize(body)) };
static object Track(string id, string name, string[] artists, bool playable = true) => new { id, name, artists = artists.Select(id => new { id, name = "deliberately-identical-name" }), is_playable = playable };
static HttpResponseMessage Tracks(params object[] tracks) => Json(new { tracks = new { items = tracks } });
record Request(string Method, string Uri, string? Auth, string Body);
class FakeHandler(Func<Request, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<Request> Requests { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var captured = new Request(request.Method.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        Requests.Add(captured);
        return respond(captured);
    }
}
class TokenStub : ISpotifyClientCredentialAuthorizer
{
    public Task<(string AccessToken, int ExpiresIn)> GetAccessToken() => Task.FromResult(("catalogue-token", 3600));
}
