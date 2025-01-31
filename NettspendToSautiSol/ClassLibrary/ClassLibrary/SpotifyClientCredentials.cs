using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NettspendToSautiSol
{
    public class SpotifyClientCredentials
    {
        private readonly string _clientId = "c6ed8f690a15491f9deb29547c8447ff";
        private readonly string _clientSecret = "34009c71d59748a09bf3867de0f8869e";
        private const string TokenUrl = "https://accounts.spotify.com/api/token";

        public async Task<(string AccessToken, int ExpiresIn)> GetAccessTokenAsync()
        {
            using HttpClient httpClient = new HttpClient();
            string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            StringContent requestContent = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await httpClient.PostAsync(TokenUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to retrieve access token. Status code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonElement tokenData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            string? accessToken = tokenData.GetProperty("access_token").GetString();
            int expiresIn = tokenData.GetProperty("expires_in").GetInt32();

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("Access token not found in response.");

            return (accessToken, expiresIn);
        }
    }
}