using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Changelogged;

internal static partial class Projects
{
    private static partial class Regexes
    {
        [GeneratedRegex("""\.slnx?\z""")]
        public static partial Regex SolutionExtensions { get; }
    }

    private static async Task<SolutionModel> OpenSolutionAsync()
    {
        string? solutionFile = null;
        {
            IEnumerable<string> files = Directory
                .EnumerateFiles(Environment.CurrentDirectory)
                .Where(f => Regexes.SolutionExtensions.IsMatch(f));
            using IEnumerator<string> enumerator = files.GetEnumerator();
            if (!enumerator.MoveNext() || (solutionFile = enumerator.Current) is var _ && enumerator.MoveNext())
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                Console.Error.WriteLine("::error::The working directory must have exactly one solution file.");
#pragma warning restore CA1849 // Call async methods when in an async method
                Environment.Exit(1);
                return null!;
            }
        }

        await using FileStream stream = File.OpenRead(solutionFile);
        return await (solutionFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            ? SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None)
            : SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None))
            ;
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
        return OpenSolutionAsync().GetAwaiter().GetResult()
            .SolutionProjects
            .Select(p => p.ActualDisplayName)
            .ToHashSet()
            .AsReadOnly()
            ;
    }

    private static async Task<ReadOnlySet<string>> FromProjectAsync(string projectName)
    {
        string projectPath = (await OpenSolutionAsync()).SolutionProjects.First(p => Path.GetFileName(p.FilePath) == projectName).FilePath;
        string dotnetExecutableName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        using var process = Process.Start(
            new ProcessStartInfo(dotnetExecutableName, ["msbuild", "-t:IncludeTransitiveProjectReferences", "-getItem:ProjectReference", projectPath])
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = true,
            }
        );
        await process!.WaitForExitAsync();
        return (await JsonSerializer.DeserializeAsync(process.StandardOutput.BaseStream, SourceGenerationContext.Default.MSBuildProjectReferenceCollection))!
            .Select(p => p.Identity)
            .ToHashSet()
            .AsReadOnly()
            ;
    }

    /// <summary>
    /// Gets a list of project names from evaluating the dependencies of the project with the specified <paramref name="projectName" />.
    /// </summary>
    /// <param name="projectName">The file name of the project to evaluate.</param>
    /// <returns>A list of project names.</returns>
    public static ReadOnlySet<string> FromProject(string projectName)
    {
        return FromProjectAsync(projectName).GetAwaiter().GetResult();
    }
}
