using System.Security.Cryptography;
 
using Microsoft.Data.Sqlite; //cite******************

namespace NettspendToSautiSol;

public class ArtistNetworkDatabaseManager 
{
    private readonly string  ConnectionString = $"Data Source=/Users/jonathanlyria/RiderProjects" +
                                                        $"/NettspendToSautiSol/NettspendToSautiSol/ArtistNetwork" +
                                                        $"/ArtistNetwork/ArtistNetworkDatabase.db";
    public  void InitializeDatabase()
    {
        string databasePath = "/Users/jonathanlyria/RiderProjects/NettspendToSautiSol/NettspendToSautiSol/ArtistNetwork" +
                              "/ArtistNetwork/ArtistNetworkDatabase.db";
        if (!File.Exists(databasePath)) 
        {
            File.Create(databasePath).Close();
        }
        
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
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

        Console.WriteLine("Database initialized: Tables 'Artist' and 'Connections' created.");
    }


    public  bool IsArtistInDbByName(string artistName)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        {
            connection.Open();

            string selectQuery = "SELECT 1 FROM Artist WHERE LOWER(ArtistName) = LOWER(@artistName);";
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
    
    public  bool IsArtistInDbById(string id)
    {
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
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
    
    public  bool IsConnectionInDb(string spotifyId1, string spotifyId2)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
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




    public  string GetIdFromName(string artistName)
    {
        string spotifyId = "";

        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
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

    public  string GetNameFromId(string spotifyId)
    {
        string name = "";
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
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
    
    public  List<ArtistNode> GetAllArtistNodes()
    {
        List<ArtistNode> artists = new List<ArtistNode>();

        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string selectQuery = "SELECT SpotifyId, ArtistName, isExpanded FROM Artist;";
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

    public  Dictionary<ArtistNode, Dictionary<ArtistNode, double>> GetNetwork()
    {
        Dictionary<ArtistNode, Dictionary<ArtistNode, double>> artistNetwork = new Dictionary<ArtistNode, Dictionary<ArtistNode, double>>();

        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
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


    public Queue<ArtistNode>? GetSearchQueue()
    {
        Queue<ArtistNode> queue = new Queue<ArtistNode>();
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string selectQuery = @"
           SELECT ArtistName, SpotifyID FROM Artist WHERE isExpanded = 0";

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
    public  void UpdateIsExpanded(string spotifyId)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        {
            connection.Open();

            string updateQuery = "UPDATE Artist SET isExpanded = 1 WHERE SpotifyId = @spotifyId;";
            using (SqliteCommand cmd = new SqliteCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId", spotifyId);
                cmd.ExecuteNonQuery();
            }
        }
    }


    public  void AddArtistToDb(ArtistNode artist)
    {
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string insertQuery = "INSERT INTO Artist (ArtistName, isExpanded, SpotifyID) VALUES (@artistName, 0, @spotifyId);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artist.Name);
                cmd.Parameters.AddWithValue("@spotifyId", artist.SpotifyId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public  void AddConnectionToDb(string spotifyId1, string spotifyId2, double weight)
    {
    
        using (SqliteConnection connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();

            string insertQuery = @"
                INSERT INTO Connections (SpotifyId1, SpotifyId2, Weight)
                VALUES (@spotifyId1, @spotifyId2, @strength);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId1", spotifyId1);
                cmd.Parameters.AddWithValue("@spotifyId2", spotifyId2);
                cmd.Parameters.AddWithValue("@strength", weight);
                cmd.ExecuteNonQuery();
            }
        }

    }
    
}