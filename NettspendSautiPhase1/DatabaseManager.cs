using Microsoft.Data.Sqlite;

namespace NettspendSautiPhase1;

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
 
        InitializeDatabase();
     
         
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
                    ArtistID TEXT PRIMARY KEY,
                    ArtistName TEXT NOT NULL UNIQUE
                );";
            using (var cmd = new SqliteCommand(createArtistTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Create Connections table
            string createConnectionsTableQuery = @"
                CREATE TABLE IF NOT EXISTS Connections (
                    ConnectionID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ArtistID1 TEXT NOT NULL,
                    ArtistID2 TEXT NOT NULL,
                    ConnectionStrength REAL NOT NULL,
                    Placeholder BOOLEAN NOT NULL,
                    FOREIGN KEY (ArtistID1) REFERENCES Artist(ArtistID),
                    FOREIGN KEY (ArtistID2) REFERENCES Artist(ArtistID)
                );";
            using (var cmd = new SqliteCommand(createConnectionsTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Database initialized: Tables 'Artist' and 'Connections' created.");
    }

    // Retrieve an artist by identifier (MBID)
    public ArtistNode GetArtistByIdentifier(string artistId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT ArtistID, ArtistName FROM Artist WHERE ArtistID = @artistId;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistId", artistId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ArtistNode(reader.GetString(0), reader.GetString(1));
                    }
                }
            }
        }
        return null; // Return null if artist is not found
    }

    // Retrieve artists by name (case-insensitive)
    public List<ArtistNode> GetArtistsByName(string artistName)
    {
        var artists = new List<ArtistNode>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT ArtistID, ArtistName FROM Artist WHERE ArtistName LIKE @artistName;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", "%" + artistName + "%");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ArtistNode artist = new ArtistNode(reader.GetString(0), reader.GetString(1));
                    }
                }
            }
        }
        return artists; // Return a list of artists
    }

    // Add a new artist to the database
    public void AddArtist(string artistId, string artistName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistID, ArtistName) VALUES (@artistId, @artistName);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistId", artistId);
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // Add a connection between two artists
    public void AddConnection(string artistId1, string artistId2, double strength, bool placeholder)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Connections (ArtistID1, ArtistID2, ConnectionStrength, Placeholder)
                VALUES (@artistId1, @artistId2, @strength, @placeholder);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistId1", artistId1);
                cmd.Parameters.AddWithValue("@artistId2", artistId2);
                cmd.Parameters.AddWithValue("@strength", strength);
                cmd.Parameters.AddWithValue("@placeholder", placeholder);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public bool IsPlaceholder(ArtistNode artistNode1, ArtistNode artistNode2)
    {
        return GetConnection(artistNode1.ArtistID , artistNode2.ArtistID).IsPlaceholder;
    }
    public void RemoveConnection(string artistId1, string artistId2)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string deleteQuery = @"
            DELETE FROM Connections
            WHERE (ArtistID1 = @artistId1 AND ArtistID2 = @artistId2)
            OR (ArtistID1 = @artistId2 AND ArtistID2 = @artistId1);";

            using (var cmd = new SqliteCommand(deleteQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistId1", artistId1);
                cmd.Parameters.AddWithValue("@artistId2", artistId2);
                cmd.ExecuteNonQuery();
            }
        }
    }
    // Retrieve a connection between two artists
    public ArtistEdge GetConnection(string artistId1, string artistId2)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
    
            string selectQuery = @"
                SELECT ArtistID1, ArtistID2, ConnectionStrength, Placeholder
                FROM Connections
                WHERE (ArtistID1 = @artistId1 AND ArtistID2 = @artistId2)
                OR (ArtistID1 = @artistId2 AND ArtistID2 = @artistId1);";
    
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistId1", artistId1);
                cmd.Parameters.AddWithValue("@artistId2", artistId2);
    
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Retrieve connection data
                        ArtistNode Artist1 = GetArtistByIdentifier(reader.GetString(0));
                        ArtistNode Artist2 = GetArtistByIdentifier(reader.GetString(1));
                        var strength = reader.GetDouble(2);
                        var placeholder = reader.GetBoolean(3);
    
                        // Create and return an ArtistConnection object
                        return new ArtistEdge(Artist1, Artist2, strength, placeholder);
                    }
                }
            }
        }
        return null;
    } 
    
   public List<ArtistNode> GetAllArtists()
   {
       var artists = new List<ArtistNode>();
   
       using (var connection = new SqliteConnection(_connectionString))
       {
           connection.Open();
   
           string selectQuery = "SELECT ArtistID, ArtistName FROM Artist;";
           using (var cmd = new SqliteCommand(selectQuery, connection))
           {
               using (var reader = cmd.ExecuteReader())
               {
                   while (reader.Read())
                   {
                       string artistId = reader.GetString(0);
                       string artistName = reader.GetString(1);
                       artists.Add(new ArtistNode(artistId, artistName));
                   }
               }
           }
       }
   
       return artists;
   }
   
   // Retrieve all connections from the database
   public List<ArtistEdge> GetAllConnections()
   {
       var connections = new List<ArtistEdge>();
   
       using (var connection = new SqliteConnection(_connectionString))
       {
           connection.Open();
   
           string selectQuery = @"
               SELECT ArtistID1, ArtistID2, ConnectionStrength, Placeholder
               FROM Connections;";
           using (var cmd = new SqliteCommand(selectQuery, connection))
           {
               using (var reader = cmd.ExecuteReader())
               {
                   while (reader.Read())
                   {
                       string artistId1 = reader.GetString(0);
                       string artistId2 = reader.GetString(1);
                       double strength = reader.GetDouble(2);
                       bool placeholder = reader.GetBoolean(3);
   
                       // Fetch ArtistNodes from database to create ArtistEdge
                       var artist1 = GetArtistByIdentifier(artistId1);
                       var artist2 = GetArtistByIdentifier(artistId2);
   
                       if (artist1 != null && artist2 != null)
                       {
                           connections.Add(new ArtistEdge(artist1, artist2, strength, placeholder));
                       }
                   }
               }
           }
       }
   
       return connections;
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
                   string artistId = reader.GetString(0); // ArtistID
                   string artistName = reader.GetString(1); // ArtistName
                   Console.WriteLine($"ID: {artistId}, Name: {artistName}");
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
                   string artistId1 = reader.GetString(1); // ArtistID1
                   string artistId2 = reader.GetString(2); // ArtistID2
                   double strength = reader.GetDouble(3); // ConnectionStrength
                   bool placeholder = reader.GetBoolean(4); // Placeholder
                   Console.WriteLine($"ConnectionID: {connectionId}, Artist1: {artistId1}, Artist2: {artistId2}, Strength: {strength}, Placeholder: {placeholder}");
               }
           }
       }
   }
   
       
     
}