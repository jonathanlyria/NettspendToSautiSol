namespace GlobalTypes;

public static class ArtistSimilarity
{
    /// <summary>Converts a Last.fm similarity in [0, 1] into a dissimilarity cost.</summary>
    public static double ToCost(double similarity)
    {
        if (!double.IsFinite(similarity) || similarity < 0 || similarity > 1)
            throw new ArgumentOutOfRangeException(nameof(similarity),
                "Artist similarity must be a finite value between 0 and 1.");

        return 1 - similarity;
    }
}
