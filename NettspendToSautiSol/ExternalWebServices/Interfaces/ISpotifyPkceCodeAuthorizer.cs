namespace ExternalWebServices.Interfaces;

public interface ISpotifyPkceCodeAuthorizer
{
    Task<(string AuthUrl, string State)> GetAuthorizationUrl();
    Task<string> ExchangeCode(string code, string state);
}