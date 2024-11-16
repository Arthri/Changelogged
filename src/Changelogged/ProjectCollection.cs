using System.Text.Json;
using System.Text.RegularExpressions;

namespace Changelogged;

internal static partial class ProjectsLoader
{
    /// <summary>
    /// Gets a list of project names from the solution file in the working directory.
    /// </summary>
    /// <returns>A list of project names.</returns>
    /// <remarks>
    /// There must only be one solution file in the working directory.
    /// </remarks>
    public static HashSet<string> FromSolution()
    {
        string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.sln");
        if (files.Length is not 1)
        {
            Console.Error.WriteLine("::error::The working directory must have exactly one solution file.");
            Environment.Exit(1);
            return null!;
        }

        string solution = File.ReadAllText(files[0]);
        MatchCollection matches = ProjectNameRegex().Matches(solution);

        var projects = new HashSet<string>(matches.Count);
        int i = 0;
        foreach (Match match in matches)
        {
            _ = projects.Add(match.Groups[1].Value);
            i++;
        }

        // TODO: Use ReadOnlySet<T> in .NET 9
        return projects;
    }

    [GeneratedRegex("""^Project\("{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}}"\) = "([^"]+)", "[^"]+", "{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}}"\r?$""", RegexOptions.Multiline)]
    private static partial Regex ProjectNameRegex();

    /// <summary>
    /// Gets a list of project names from the project files of the solution filter in the working directory.
    /// </summary>
    /// <param name="fileName">The name of the solution filter.</param>
    /// <returns>A list of project names.</returns>
    public static HashSet<string> FromSolutionFilter(string fileName)
    {
        SolutionFilter? filter;
        try
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            filter = JsonSerializer.Deserialize(stream, SourceGenerationContext.Default.SolutionFilter);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("::error::Unable to read and deserialize solution filter.");
            throw;
        }

        if (filter is null)
        {
            Console.Error.WriteLine("::error::Solution filter is null.");
            Environment.Exit(1);
            return null!;
        }

        return filter.Solution.Projects.Select(p => Path.GetFileNameWithoutExtension(p.Replace('\\', '/'))).ToHashSet();
    }
}
