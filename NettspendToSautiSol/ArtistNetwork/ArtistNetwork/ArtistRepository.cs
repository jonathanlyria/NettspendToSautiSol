using Microsoft.Data.Sqlite;

namespace NettspendToSautiSol;

public class ArtistRepository : IArtistRepository
{
    private readonly string _connectionString;

    public ArtistRepository(string databasePath)
    {
        _connectionString = "Data Source=" + databasePath;
    }

    public bool IsArtistInDbByName(string artistName)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
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

    public List<ArtistNode> GetAllArtistsAsArtistNodes()
    {
        List<ArtistNode> artists = new List<ArtistNode>();

        using (SqliteConnection connection = new SqliteConnection(_connectionString))
            
        {
            connection.Open();

            string selectQuery = "SELECT SpotifyId, ArtistName FROM Artist;";
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

    public void UpdateIsExpanded(string spotifyId)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
            
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

    public void AddArtistToDb(ArtistNode artist)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string insertQuery =
                "INSERT INTO Artist (ArtistName, isExpanded, SpotifyID) VALUES (@artistName, 0, @spotifyId);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@artistName", artist.Name);
                cmd.Parameters.AddWithValue("@spotifyId", artist.SpotifyId);
                cmd.ExecuteNonQuery();
            }
        }
        
    }
    
    public Queue<ArtistNode>? GetSearchQueue()
    {
        Queue<ArtistNode> queue = new Queue<ArtistNode>();
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
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
}