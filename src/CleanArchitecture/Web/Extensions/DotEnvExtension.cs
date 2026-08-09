namespace CleanArchitecture.Web.Extensions;

/// <summary>
/// Loads a local <c>.env</c> file (key=value lines, <c>#</c> comments, optional surrounding quotes)
/// into the current process environment variables so the built-in environment-variable
/// configuration provider picks them up. This keeps secrets out of the repository.
/// </summary>
public static class DotEnvExtension
{
    public static void LoadDotEnv()
    {
        string? envFile = FindDotEnvFile();
        if (envFile is null)
        {
            return;
        }

        foreach (string line in File.ReadAllLines(envFile))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            int separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = trimmed[..separatorIndex].Trim();
            string value = trimmed[(separatorIndex + 1)..].Trim();

            // Strip a single pair of surrounding quotes, if present.
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"')
                    || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindDotEnvFile()
    {
        // Search from the current working directory up toward the repository root,
        // then fall back to the output directory (for in-IDE/`dotnet run` launches).
        var candidates = new List<string>();
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            candidates.Add(directory.FullName);
            directory = directory.Parent;
        }

        candidates.Add(AppContext.BaseDirectory);

        foreach (string candidate in candidates.Distinct())
        {
            string envPath = Path.Combine(candidate, ".env");
            if (File.Exists(envPath))
            {
                return envPath;
            }
        }

        return null;
    }
}
