namespace GlobalTypes;

public static class EnvironmentConfiguration
{
    public static string Require(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Set the {name} environment variable. See README.md for setup.");

        return value;
    }
}
