using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Changelogged;

internal static partial class ProjectsLoader
{
    private static partial class Regexes
    {
        [GeneratedRegex("""\.slnx?\z""")]
        public static partial Regex SolutionExtensions { get; }
    }

    /// <summary>
    /// Gets a list of project names from the solution file in the working directory.
    /// </summary>
    /// <returns>A list of project names.</returns>
    /// <remarks>
    /// There must only be one solution file in the working directory.
    /// </remarks>
    public static ReadOnlySet<string> FromSolution()
    {
        string? solutionFile = null;
        {
            IEnumerable<string> files = Directory
                .EnumerateFiles(Environment.CurrentDirectory)
                .Where(f => Regexes.SolutionExtensions.IsMatch(f));
            using IEnumerator<string> enumerator = files.GetEnumerator();
            if (!enumerator.MoveNext() || (solutionFile = enumerator.Current) is var _ && enumerator.MoveNext())
            {
                Console.Error.WriteLine("::error::The working directory must have exactly one solution file.");
                Environment.Exit(1);
                return null!;
            }
        }

        using FileStream stream = File.OpenRead(solutionFile);
        SolutionModel solutionModel = solutionFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            ? SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None).GetAwaiter().GetResult()
            : SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None).GetAwaiter().GetResult()
            ;

        return solutionModel
            .SolutionProjects
            .Select(p => p.ActualDisplayName)
            .ToHashSet()
            .AsReadOnly()
            ;
    }

    /// <summary>
    /// Gets a list of project names from the project files of the solution filter in the working directory.
    /// </summary>
    /// <param name="fileName">The name of the solution filter.</param>
    /// <returns>A list of project names.</returns>
    public static ReadOnlySet<string> FromSolutionFilter(string fileName)
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

        return filter
            .Solution
            .Projects
            .Select(p => Path.GetFileNameWithoutExtension(p.Replace('\\', '/')))
            .ToHashSet()
            .AsReadOnly()
            ;
    }
}
