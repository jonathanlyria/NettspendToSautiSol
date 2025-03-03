namespace NettspendToSautiSol;

public interface ISpotifyClientCredentialAuthorizer
{
    Task<(string AccessToken, int ExpiresIn)> GetAccessToken();

}