namespace ExternalWebServices.Interfaces;

public interface ISpotifyClientCredentialAuthorizer
{
    Task<(string AccessToken, int ExpiresIn)> GetAccessToken();

}