using System.Security.Cryptography;
using Microsoft.Data.Sqlite; //cite******************

namespace NettspendToSautiSol;
public class DatabaseManager
{
    private readonly string _connectionString;
    
    public DatabaseManager(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";

        if (!File.Exists(databasePath)) 
        {
            File.Create(databasePath).Close();
            InitializeDatabase();
        }
    }

    private void InitializeDatabase()
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string createArtistTableQuery = @"
                CREATE TABLE IF NOT EXISTS Artist (
                    SpotifyId TEXT NOT NULL PRIMARY KEY,
                    ArtistName TEXT NOT NULL,
                    Expanded BIT NOT NULL
                );";
            using (SqliteCommand cmd = new SqliteCommand(createArtistTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
            

            string createConnectionsTableQuery = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    ConnectionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    SpotifyId1 TEXT NOT NULL,
                    SpotifyId2 TEXT NOT NULL,
                    ConnectionStrength REAL NOT NULL
                );";
            using (SqliteCommand cmd = new SqliteCommand(createConnectionsTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Database initialized: Tables 'Artist' and 'Connections' created.");
    }


    public bool IsArtistInDbByName(string artistName)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string selectQuery = "SELECT ArtistName FROM Artist WHERE LOWER(ArtistName) = LOWER(@artistName);";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
    
    //change usage

    public bool DoesConnectionExistInDb(string spotifyId1, string spotifyId2)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string selectQuery = @"
                SELECT * FROM Connections
                WHERE (SpotifyId1 = @spotifyId1 AND SpotifyId2 = @spotifyId2)
                OR (SpotifyId1 = @spotifyId2 AND SpotifyId2 = @spotifyId1);";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId1", spotifyId1);
                cmd.Parameters.AddWithValue("@spotifyId2", spotifyId2);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return true;
                    }
                    return false;
                }

            }
        }
    }

    public void UpdateExpanded(string spotifyId)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string updateQuery = "UPDATE Artist SET Expanded = 1 WHERE SpotifyId = @spotifyId;";
            using (SqliteCommand cmd = new SqliteCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                cmd.ExecuteNonQuery();
            }
        }
    }


    public List<ArtistNode> GetAllArtistNodesInDb()
    {
        List<ArtistNode> artists = new List<ArtistNode>();

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT SpotifyId, ArtistName, Expanded FROM Artist;";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string spotifyId = reader.GetString(0);
                        string artistName = reader.GetString(1);
                        artists.Add(new ArtistNode(artistName, spotifyId));
                    }
                }
            }
        }

        return artists;
    }

    public bool IsArtistInDbById(string id)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT SpotifyID FROM Artist WHERE SpotifyID = @id;";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public string GetIdFromName(string artistName)
    {
        string spotifyId = "";

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"SELECT SpotifyId FROM Artist WHERE ArtistName = @artistName COLLATE NOCASE";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        spotifyId = reader.GetString(0);
                    }

                }
            }
        }
        return spotifyId;
    }

    public string GetNameFromId(string spotifyId)
    {
        string name = "";
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"SELECT ArtistName FROM Artist WHERE SpotifyId = @spotifyId COLLATE NOCASE";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        name = reader.GetString(0);
                    }

                }
            }
        }
        return name;
    }
    public List<ArtistConnection> GetAllConnectionsInDb()
    {
        List<ArtistConnection> connections = new List<ArtistConnection>();

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
            SELECT a1.ArtistName, c.SpotifyId1, a2.ArtistName, c.SpotifyId2, c.ConnectionStrength
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
                        string artistName2 = reader.GetString(2);
                        string spotifyId2 = reader.GetString(3);
                        double strength = reader.GetDouble(4);

                        ArtistNode artist1 = new ArtistNode(artistName1, spotifyId1);
                        ArtistNode artist2 = new ArtistNode(artistName2, spotifyId2);

                        connections.Add(new ArtistConnection(artist1, artist2, strength));
                    }
                }
            }
        }

        return connections;
    }


    public Queue<ArtistNode>? GetExpanderQueueFromDb()
    {
        Queue<ArtistNode> queue = new Queue<ArtistNode>();
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
           SELECT ArtistName, SpotifyID FROM Artist WHERE Expanded = 0";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows) return null;
                    while (reader.Read())
                    {
                        string artistName = reader.GetString(0);
                        string spotifyId = reader.GetString(1);
                        
                        queue.Enqueue(new ArtistNode(artistName, spotifyId));
                    }
                }
            }
        }
        return queue;
    }

    public void AddArtistToDb(string artistName, string spotifyId)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistName, Expanded, SpotifyID) VALUES (@artistName, 0, @spotifyId);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void AddConnectionToDb(string spotifyId1, string spotifyId2, double strength)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Connections (SpotifyId1, SpotifyId2, ConnectionStrength)
                VALUES (@spotifyId1, @spotifyId2, @strength);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId1", spotifyId1);
                cmd.Parameters.AddWithValue("@spotifyId2", spotifyId2);
                cmd.Parameters.AddWithValue("@strength", strength);
                cmd.ExecuteNonQuery();
            }
        }

    }

    public Dictionary<ArtistNode, List<ArtistNode>> GetArtistAndListOfConnections()
    {
        Dictionary<ArtistNode, List<ArtistNode>> artistConnections = new Dictionary<ArtistNode, List<ArtistNode>>();

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            string selectQuery = @"
                SELECT a.ArtistName, 
                       a.SpotifyId, 
                       GROUP_CONCAT(DISTINCT CASE 
                                               WHEN c.SpotifyId1 = a.SpotifyId THEN c.SpotifyId2 
                                               ELSE c.SpotifyId1 
                                             END) AS ConnectedSpotifyIds
                FROM Artist a
                JOIN Connections c ON a.SpotifyId = c.SpotifyId1 OR a.SpotifyId = c.SpotifyId2
                GROUP BY a.ArtistName, a.SpotifyId;
            ";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName = reader.GetString(0);
                        string artistSpotifyId = reader.GetString(1);
                        string connectedSpotifyIds = reader.IsDBNull(2) ? "" : reader.GetString(2);

                        ArtistNode artistNode = new ArtistNode(artistName, artistSpotifyId);
                        List<ArtistNode> connectedArtists = new List<ArtistNode>();

                        if (!string.IsNullOrEmpty(connectedSpotifyIds))
                        {
                            string[] spotifyIds = connectedSpotifyIds.Split(',');

                            foreach (string connectedId in spotifyIds)
                            {
                                string connectedNameQuery = "SELECT ArtistName FROM Artist WHERE SpotifyId = @SpotifyId";
                                using (SqliteCommand nameCmd = new SqliteCommand(connectedNameQuery, connection))
                                {
                                    nameCmd.Parameters.AddWithValue("@SpotifyId", connectedId);
                                    string connectedArtistName = nameCmd.ExecuteScalar()?.ToString();

                                    if (!string.IsNullOrEmpty(connectedArtistName))
                                    {
                                        connectedArtists.Add(new ArtistNode(connectedArtistName, connectedId));
                                    }
                                }
                            }
                        }

                        artistConnections[artistNode] = connectedArtists;
                    }
                }
            }
        }

        return artistConnections;
    }

}