namespace CNCToolingDatabase.Helpers;

public static class DataFileResolver
{
    public static string Resolve(params string[] relativeParts)
    {
        var relative = Path.Combine(relativeParts);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, relative),
            Path.Combine(Directory.GetCurrentDirectory(), relative),
            Path.Combine(AppContext.BaseDirectory, "Data", Path.GetFileName(relative)),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", Path.GetFileName(relative))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, relative);
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }

        return candidates[0];
    }

    public static bool Exists(params string[] relativeParts) =>
        File.Exists(Resolve(relativeParts));
}
