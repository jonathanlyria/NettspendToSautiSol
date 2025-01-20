using Microsoft.Data.Sqlite;

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

    // Initialize the database with required tables
    private void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Create Artist table
            string createArtistTableQuery = @"
                CREATE TABLE IF NOT EXISTS Artist (
                    ArtistName TEXT NOT NULL PRIMARY KEY,
                    SpotifyID TEXT NOT NULL,
                    Expanded BIT NOT NULL, 
                    Priority REAL NOT NULL
                );";
            using (var cmd = new SqliteCommand(createArtistTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Create Connections table
            string createConnectionsTableQuery = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    ConnectionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ArtistName1 TEXT NOT NULL,
                    ArtistName2 TEXT NOT NULL,
                    ConnectionStrength REAL NOT NULL
                );";
            using (var cmd = new SqliteCommand(createConnectionsTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Database initialized: Tables 'Artist' and 'Connections' created.");
    }

    // Retrieve artists by name (case-insensitive)

    public bool DoesArtistExist(string artistName)
    {
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string selectQuery = "SELECT ArtistName FROM Artist WHERE LOWER(ArtistName) = LOWER(@artistName);";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                using (var reader = cmd.ExecuteReader())
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

    public bool DoesConnectionExist(string artistName1, string artistName2)
    {
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string selectQuery = @"
                SELECT * FROM Connections
                WHERE (ArtistName1 = @artistName1 AND ArtistName2 = @artistName2)
                OR (ArtistName1 = @artistName2 AND ArtistName2 = @artistName1);";

            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
                using (var reader = cmd.ExecuteReader())
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
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();

            string updateQuery = "UPDATE Artist SET Expanded = 1 WHERE SpotifyId = @spotifyId;";
            using (var cmd = new SqliteCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                cmd.ExecuteNonQuery();
            }
        }
    }


    public List<ArtistNode> GetAllArtists()
    {
        var artists = new List<ArtistNode>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT ArtistName, SpotifyId, Expanded FROM Artist;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
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

    public bool DoesIdExist(string id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT SpotifyID FROM Artist WHERE SpotifyID = @id;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
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

    public List<ArtistNode> GetAllSimilarArtistNames(string artistName)
    {
        var artists = new List<ArtistNode>();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
            SELECT ArtistName, SpotifyId
            FROM Artist
            WHERE LOWER(ArtistName) != LOWER(@artistName)
            OR LOWER(ArtistName) LIKE LOWER(@artistName) || ' %'
            AND ArtistName IS NOT NULL;";

            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var spotifyId = reader.GetString(1);
                        artists.Add(new ArtistNode(name, spotifyId));
                    }
                }
            }
        }
        return artists;
    }

    public string GetIdFromName(string artistName)
    {
        string spotifyId = "";

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"SELECT SpotifyId FROM Artist WHERE ArtistName = @artistName COLLATE NOCASE";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                using (var reader = cmd.ExecuteReader())
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
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"SELECT ArtistName FROM Artist WHERE SpotifyId = @spotifyId COLLATE NOCASE";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                using (var reader = cmd.ExecuteReader())
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

    // Retrieve all connections from the database
    public List<ArtistEdge> GetAllConnections()
    {
        var connections = new List<ArtistEdge>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
               SELECT ArtistName1, ArtistName2, ConnectionStrength
               FROM Connections";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName1 = reader.GetString(0);
                        string artistId1 = GetIdFromName(artistName1);
                        string artistName2 = reader.GetString(1);
                        string artistId2 = GetIdFromName(artistName2);
                        double strength = reader.GetDouble(2);
                        // Fetch ArtistNodes from database to create ArtistEdge
                        ArtistNode artist1 = new ArtistNode(artistName1, artistId1);
                        ArtistNode artist2 = new ArtistNode(artistName2, artistId2);

                        connections.Add(new ArtistEdge(artist1, artist2, strength));

                    }
                }
            }
        }

        return connections;
    }

    public PriorityQueue<ArtistNode, double>? GetExpanderQueue()
    {
        var queue = new PriorityQueue<ArtistNode, double>();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
           SELECT ArtistName, SpotifyID, Priority FROM Artist WHERE Expanded = 0";

            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows) return null;
                    while (reader.Read())
                    {
                        string artistName = reader.GetString(0);
                        string spotifyId = reader.GetString(1);
                        double priority = reader.GetDouble(2);
                        queue.Enqueue(new ArtistNode(artistName, spotifyId), priority);
                    }
                }
            }
        }
        return queue;
    }

    public void AddArtist(string artistName, string spotifyId, double priority)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistName, Expanded, SpotifyID, Priority) VALUES (@artistName, 0, @spotifyId, @priority);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                cmd.Parameters.AddWithValue("@priority", priority);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // Add a connection between two artists
    public void AddConnection(string artistName1, string artistName2, double strength)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Connections (ArtistName1, ArtistName2, ConnectionStrength)
                VALUES (@artistName1, @artistName2, @strength);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
                cmd.Parameters.AddWithValue("@strength", strength);
                cmd.ExecuteNonQuery();
            }
        }
    }



    public void PrintDatabaseToTerminal()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            Console.WriteLine("Artists:");
            string fetchArtistsQuery = "SELECT * FROM Artist;";
            using (var cmd = new SqliteCommand(fetchArtistsQuery, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string artistName = reader.GetString(0); // ArtistName
                    string spotifyId = reader.GetString(2); // SpotifyID
                    string lastExpanded = reader.IsDBNull(1) ? "NULL" : reader.GetString(1); // LastExpanded
                    Console.WriteLine($"ArtistName: {artistName}, SpotifyID: {spotifyId} LastExpanded: {lastExpanded}");
                }
            }

            Console.WriteLine("\nConnections:");
            string fetchConnectionsQuery = "SELECT * FROM Connections;";
            using (var cmd = new SqliteCommand(fetchConnectionsQuery, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int connectionId = reader.GetInt32(0); // ConnectionID
                    string artistName1 = reader.GetString(1);
                    string artistName2 = reader.GetString(2);
                    double strength = reader.GetDouble(3); // ConnectionStrength
                    Console.WriteLine($"ConnectionID: {connectionId}, Artist1: {artistName1}, Artist2: {artistName2}, Strength: {strength}");
                }
            }
        }
    }




}