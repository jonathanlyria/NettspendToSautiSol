using Microsoft.Data.Sqlite;
namespace NettspendToSautiSol;

public class WebServerDatabaseService : DatabaseService
{
    private readonly string _databasePath;
    public WebServerDatabaseService(string databasePath) : base(databasePath)
    {
        ArtistRepository = artistRepository;
        ConnectionRepository = connectionRepository;
        _databasePath = databasePath;
        _connection = new SqliteConnection("Data Source=" + _databasePath);
        InitialiseDatabase();
    }
    
}