using Microsoft.Data.Sqlite;

namespace NettspendToSautiSol;

public class DatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;
    private readonly string _databasePath;

    public DatabaseRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = $"Data Source={databasePath}";
    }
    
    public void InitialiseDatabase()
    {
        if (!File.Exists(_databasePath)) 
        {
            File.Create(_databasePath).Close();
        }
        
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string createArtistTableQuery = @"
                CREATE TABLE IF NOT EXISTS Artist (
                    SpotifyId TEXT NOT NULL PRIMARY KEY,
                    ArtistName TEXT NOT NULL,
                    isExpanded BIT NOT NULL
                );";
            using (SqliteCommand cmd = new SqliteCommand(createArtistTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
            

            string createConnectionsTableQuery = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    SpotifyId1 TEXT NOT NULL REFERENCES Artist(SpotifyId),
                    SpotifyId2 TEXT NOT NULL REFERENCES Artist(SpotifyId),
                    Weight REAL NOT NULL
                );";
            using (SqliteCommand cmd = new SqliteCommand(createConnectionsTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
    public Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork()
    {
        Dictionary<ArtistNode, Dictionary<ArtistNode, double>> artistNetwork = new Dictionary<ArtistNode, Dictionary<ArtistNode, double>>();

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
            SELECT a1.ArtistName, c.SpotifyId1, a2.ArtistName, c.SpotifyId2, c.Weight
            FROM Connections c
            JOIN Artist a1 ON c.SpotifyId1 = a1.SpotifyId
            JOIN Artist a2 ON c.SpotifyId2 = a2.SpotifyId";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName1 = reader.GetString(0);
                        string spotifyId1 = reader.GetString(1);
                        ArtistNode artistNode1 = new ArtistNode(artistName1, spotifyId1);
                        
                        string artistName2 = reader.GetString(2);
                        string spotifyId2 = reader.GetString(3);
                        ArtistNode artistNode2 = new ArtistNode(artistName2, spotifyId2);
                        
                        double weight = reader.GetDouble(4);
                        if (!artistNetwork.ContainsKey(artistNode1))
                        {
                            artistNetwork.Add(artistNode1, new Dictionary<ArtistNode, double>());
                        }
                        if (!artistNetwork.ContainsKey(artistNode2))
                        {
                            artistNetwork.Add(artistNode2, new Dictionary<ArtistNode, double>());
                        }
                        artistNetwork[artistNode1][artistNode2] = weight;
                        artistNetwork[artistNode2][artistNode1] = weight;
                    }
                }
            }
        }

        return artistNetwork;
    }

}