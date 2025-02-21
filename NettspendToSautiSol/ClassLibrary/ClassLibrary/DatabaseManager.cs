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
                    ArtistName TEXT NOT NULL PRIMARY KEY,
                    SpotifyId TEXT NOT NULL,
                    Expanded BIT NOT NULL
                );";
            using (SqliteCommand cmd = new SqliteCommand(createArtistTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
            

            string createConnectionsTableQuery = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    ConnectionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ArtistName1 TEXT NOT NULL,
                    ArtistName2 TEXT NOT NULL,
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

    public bool DoesConnectionExistInDb(string artistName1, string artistName2)
    {
        using SqliteConnection connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string selectQuery = @"
                SELECT * FROM Connections
                WHERE (ArtistName1 = @artistName1 AND ArtistName2 = @artistName2)
                OR (ArtistName1 = @artistName2 AND ArtistName2 = @artistName1);";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
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

            string selectQuery = "SELECT ArtistName, SpotifyId, Expanded FROM Artist;";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName = reader.GetString(0);
                        string spotifyId = reader.GetString(1);
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
               SELECT ArtistName1, ArtistName2, ConnectionStrength
               FROM Connections";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName1 = reader.GetString(0);
                        string artistId1 = GetIdFromName(artistName1);
                        string artistName2 = reader.GetString(1);
                        string artistId2 = GetIdFromName(artistName2);
                        double strength = reader.GetDouble(2);
                        ArtistNode artist1 = new ArtistNode(artistName1, artistId1);
                        ArtistNode artist2 = new ArtistNode(artistName2, artistId2);

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

    public void AddConnectionToDb(string artistName1, string artistName2, double strength)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Connections (ArtistName1, ArtistName2, ConnectionStrength)
                VALUES (@artistName1, @artistName2, @strength);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
                cmd.Parameters.AddWithValue("@strength", strength);
                cmd.ExecuteNonQuery();
            }
        }

    }

    public Dictionary<ArtistNode, > GetConnectedArtistsArray()
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            string selectQuery = @"
                SELECT a.ArtistName, 
                       GROUP_CONCAT(DISTINCT CASE 
                                               WHEN c.ArtistName1 = a.ArtistName THEN c.ArtistName2 
                                               ELSE c.ArtistName1 
                                             END) AS ConnectedArtistsArray, 
                       a.SpotifyId
                FROM Artist a
                JOIN Connections c ON a.ArtistName = c.ArtistName1 OR a.ArtistName = c.ArtistName2
                GROUP BY a.ArtistName, a.SpotifyId;
            ";
            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                    }
                    
                }
            }
        }
    }
}