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
    public string GetArtistId(string artistName)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = "SELECT ArtistName FROM Artist WHERE ArtistName = @artistName;";
            using (var cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                using (var reader = cmd.ExecuteReader())
                {
                    string spotifyId = reader.GetString(2);
                    return spotifyId;
                }
            }
      
        }
    }

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
            using (var cmd = new SqliteCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName1", artistName1);
                cmd.Parameters.AddWithValue("@artistName2", artistName2);
                cmd.Parameters.AddWithValue("@strength", strength);
            
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
   
           string selectQuery = "SELECT ArtistName, LastExpanded FROM Artist ORDER BY LastExpanded DESC;";
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

   public PriorityQueue<ArtistNode, double>? GetExpanderQueue()
   {
       var queue = new PriorityQueue<ArtistNode, double>();
       using (var connection = new SqliteConnection(_connectionString))
       {
           connection.Open();

           // Prioritize nulls first, then order by LastExpanded DESC
           string selectQuery = @"
            SELECT 
                A.ArtistName,
                CASE 
                    WHEN A.LastExpanded IS NULL THEN 2 - IFNULL(C.ConnectionStrength, 0)
                    ELSE 1 - IFNULL(C.ConnectionStrength, 0)
                END AS Priority,
                A.LastExpanded
            FROM 
                Artist A
            LEFT JOIN 
                Connections C 
            ON 
                A.ArtistName = C.ArtistName1
                AND C.ConnectionID = (
                    SELECT ConnectionID 
                    FROM Connections 
                    WHERE ArtistName1 = A.ArtistName 
                    ORDER BY ConnectionID DESC 
                    LIMIT 1
                )
            ORDER BY 
                Priority DESC,
                A.LastExpanded ASC;";
                
           using (var cmd = new SqliteCommand(selectQuery, connection))
           {
               using (var reader = cmd.ExecuteReader())
               {
                   if (!reader.HasRows) return null;
                   while (reader.Read())
                   {
                       string artistName = reader.GetString(0);
                       double priority = reader.GetDouble(1);
                       queue.Enqueue(new ArtistNode(artistName), priority);
                   }
               }
           }
       }
       return queue;
   }

    public void AddArtist(string artistName, string spotifyId, string? lastExpanded = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistName, LastExpanded, SpotifyID) VALUES (@artistName, @lastExpanded, @spotifyId);";
            using (var cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artistName);
                cmd.Parameters.AddWithValue("@lastExpanded", (object?)lastExpanded ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
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