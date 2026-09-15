using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExternalWebServices.Interfaces;
using GlobalTypes;

namespace ExternalWebServices;

public class GetPlaylistSongsService(ISpotifyClientCredentialAuthorizer clientCredentialAuthorizer, HttpClient httpClient)
    : IGetPlaylistSongsService
{
    public async Task<List<string>> GetPlaylistSongIds(List<ArtistNode> pathOfArtists)
    {
        string accessToken = (await clientCredentialAuthorizer.GetAccessToken()).AccessToken;
        var ids = new List<string>();
        var usedIds = new HashSet<string>();
        var usedNames = new HashSet<string>();
        int[] allowance = pathOfArtists.Select(_ => 3).ToArray();

        void Add(Track track)
        {
            ids.Add(track.Id);
            usedIds.Add(track.Id);
            usedNames.Add(NormalizeSongName(track.Name));
        }
        bool Unused(Track track) => !usedIds.Contains(track.Id) && !usedNames.Contains(NormalizeSongName(track.Name));

        for (int i = 0; i < pathOfArtists.Count; i++)
        {
            ArtistNode artist = pathOfArtists[i];
            Track? feature = null;
            if (i + 1 < pathOfArtists.Count)
            {
                ArtistNode next = pathOfArtists[i + 1];
                var candidates = await Search($"{artist.Name} {next.Name}", accessToken);
                feature = candidates.FirstOrDefault(t => t.ArtistIds.Length == 2 &&
                    t.ArtistIds.Contains(artist.SpotifyId) && t.ArtistIds.Contains(next.SpotifyId) && Unused(t));
                if (feature is not null)
                {
                    allowance[i]--;
                    allowance[i + 1]--;
                }
            }

            // Development mode no longer provides artist top-tracks. Search is a
            // candidate pool, not a popularity ranking; verify Spotify IDs to
            // avoid selecting another artist with the same name.
            string artistQuery = "artist:\"" + artist.Name.Replace("\"", "") + "\"";
            var songs = await Search(artistQuery, accessToken);
            foreach (Track song in songs.Where(t => t.ArtistIds.Length == 1 &&
                         t.ArtistIds[0] == artist.SpotifyId).OrderBy(_ => Random.Shared.Next()))
            {
                if (allowance[i] <= 0) break;
                if (!Unused(song)) continue;
                Add(song);
                allowance[i]--;
            }
            if (feature is not null) Add(feature);
        }
        if (ids.Count == 0)
            throw new SpotifyApiException("No suitable songs were found for this route. No playlist was created.");
        return ids;
    }

    private async Task<List<Track>> Search(string query, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=10&market=GB");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request);
        // Do not turn an API failure into an apparently successful empty playlist.
        SpotifyApiException.Check(response, "song search");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = new List<Track>();
        foreach (var item in document.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            string? id = item.GetProperty("id").GetString();
            string? name = item.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name)) continue;
            if (item.TryGetProperty("is_playable", out var playable) && playable.ValueKind == JsonValueKind.False) continue;
            result.Add(new Track(id, name, item.GetProperty("artists").EnumerateArray()
                .Select(a => a.GetProperty("id").GetString() ?? "").ToArray()));
        }
        return result;
    }

    private static string NormalizeSongName(string name)
    {
        string result = Regex.Replace(name.ToLowerInvariant(), @"\s*-\s*.*|\s*\(.*?\)", "");
        result = Regex.Replace(result, @"[^\w\s]", "");
        return Regex.Replace(result, @"\s{2,}", " ").Trim();
    }

    private sealed record Track(string Id, string Name, string[] ArtistIds);
}
