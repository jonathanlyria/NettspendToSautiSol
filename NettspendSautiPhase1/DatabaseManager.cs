using Microsoft.Data.Sqlite;

namespace NettspendSautiPhase1;

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
                    LastExpanded TEXT NULL                                   
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
                    ConnectionStrength REAL NOT NULL,
                    FOREIGN KEY (ArtistName1) REFERENCES Artist(ArtistName),
                    FOREIGN KEY (ArtistName2) REFERENCES Artist(ArtistName)
                );";
            using (var cmd = new SqliteCommand(createConnectionsTableQuery, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Database initialized: Tables 'Artist' and 'Connections' created.");
    }
    
    // Retrieve artists by name (case-insensitive)
    public ArtistNode GetArtistsByName(string artistName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT ArtistName FROM Artist WHERE ArtistName = @artistName;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", "%" + artistName + "%");
                using (var reader = cmd.ExecuteReader())
                {
                    return new ArtistNode(reader.GetString(0));
                }
            }
      
        }
    }
    public ArtistEdge GetConnection(string artistName1, string artistName2)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
    
            string selectQuery = @"
                SELECT ArtistName1, ArtistName2, ConnectionStrength
                FROM Connections
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
                        // Retrieve connection data
                        var artist1 = GetArtistsByName(reader.GetString(0));
                        var artist2 = GetArtistsByName(reader.GetString(1));
                        var strength = reader.GetDouble(2);
    
                        // Create and return an ArtistConnection object
                        return new ArtistEdge(artist1, artist2, strength);
                    }
                }
            }
        }
        return null;
    }

    public bool DoesArtistExist(string artistName)
    {
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();
            
            string selectQuery = "SELECT ArtistName FROM Artist WHERE ArtistName = @artistName;";
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

    public void UpdateLastExpanded(string artistName, string lastExpanded)
    {
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();
        
            string updateQuery = "UPDATE Artist SET LastExpanded = @lastExpanded WHERE ArtistName = @artistName;";
            using (var cmd = new SqliteCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.Parameters.AddWithValue("@lastExpanded", lastExpanded);
            
                cmd.ExecuteNonQuery();
            }
        }
    }


    public void UpdateConnectionStrength(string artistName1, string artistName2, double strength)
    {
        using var connection = new SqliteConnection(_connectionString);
        {
            connection.Open();
            
            string updateQuery = @"
                UPDATE Connections
                SET ConnectionStrength = @strength
                WHERE (ArtistName1 = @artistName1 AND ArtistName2 = @artistName2)
                OR (ArtistName1 = @artistName2 AND ArtistName2 = @artistName1);";
        }
    }

    
    public List<ArtistNode> GetAllArtists()
   {
       var artists = new List<ArtistNode>();
   
       using (var connection = new SqliteConnection(_connectionString))
       {
           connection.Open();
   
           string selectQuery = "SELECT ArtistName FROM Artist;";
           using (var cmd = new SqliteCommand(selectQuery, connection))
           {
               using (var reader = cmd.ExecuteReader())
               {
                   while (reader.Read())
                   {
                       string artistName = reader.GetString(0);
                       artists.Add(new ArtistNode(artistName));
                   }
               }
           }
       }
   
       return artists;
   }

    public List<ArtistNode> GetAllSimilarArtistNames(string artistName)
    {
        var artists = new List<ArtistNode>();
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
            SELECT ArtistName
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
                        var name = reader["ArtistName"] as string;
                        artists.Add(new ArtistNode(name));
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
               SELECT ArtistName1, ArtistName2, ConnectionStrength
               FROM Connections;";
           using (var cmd = new SqliteCommand(selectQuery, connection))
           {
               using (var reader = cmd.ExecuteReader())
               {
                   while (reader.Read())
                   {
                       string artistName1 = reader.GetString(0);
                       string artistName2 = reader.GetString(1);
                       double strength = reader.GetDouble(2);
                     
   
                       // Fetch ArtistNodes from database to create ArtistEdge
                       ArtistNode artist1 = new ArtistNode(artistName1);
                       ArtistNode artist2 = new ArtistNode(artistName2);
                      
                       connections.Add(new ArtistEdge(artist1, artist2, strength));
            
                   }
               }
           }
       }
   
       return connections;
   }
    
    public void AddArtist(string artistName, string? lastExpanded = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistName, LastExpanded) VALUES (@artistName, @lastExpanded);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.Parameters.AddWithValue("@lastExpanded", (object?)lastExpanded ?? DBNull.Value);
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
    
    public void RemoveConnection(string artistName1, string artistName2)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string deleteQuery = @"
            DELETE FROM Connections
            WHERE (ArtistName1 = @artistName1 AND ArtistName2 = @artistName2)
            OR (ArtistName1 = @artistName2 AND ArtistName2 = @artistName1);";

            using (var cmd = new SqliteCommand(deleteQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
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
                    string lastExpanded = reader.IsDBNull(1) ? "NULL" : reader.GetString(1); // LastExpanded
                    Console.WriteLine($"ArtistName: {artistName}, LastExpanded: {lastExpanded}");
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