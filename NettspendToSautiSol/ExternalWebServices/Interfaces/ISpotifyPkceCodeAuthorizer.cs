namespace NettspendToSautiSol;

public interface ISpotifyPkceCodeAuthorizer
{
    (string AuthUrl, string State) GetAuthorizationUrl();
    string ExchangeCode(string code, string state);
}