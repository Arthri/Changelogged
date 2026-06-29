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

    private static async Task<(string Path, SolutionModel Model)> OpenSolutionAsync()
    {
        string? solutionFile = null;
        {
            IEnumerable<string> files = Directory
                .EnumerateFiles(Environment.GetEnvironmentVariable("SOLUTION_DIRECTORY") ?? Environment.CurrentDirectory)
                .Where(f => Regexes.SolutionExtensions.IsMatch(f));
            using IEnumerator<string> enumerator = files.GetEnumerator();
            if (!enumerator.MoveNext() || (solutionFile = enumerator.Current) is var _ && enumerator.MoveNext())
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                Console.Error.WriteLine("::error::The solution directory must have exactly one solution file.");
#pragma warning restore CA1849 // Call async methods when in an async method
                Environment.Exit(1);
                throw new UnreachableException();
            }
        }

        await using FileStream stream = File.OpenRead(solutionFile);
        SolutionModel model = await (solutionFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            ? SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None)
            : SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None))
            ;

        return (solutionFile, model);
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
            .Model
            .SolutionProjects
            .Select(p => Path.GetFileNameWithoutExtension(p.FilePath))
            .ToHashSet()
            .AsReadOnly()
            ;
    }

    private static async Task<ReadOnlySet<string>> FromProjectAsync(string projectName)
    {
        (string? solutionPath, SolutionModel? solution) = await OpenSolutionAsync();
        string projectPath = Path.Join(
            Path.GetDirectoryName(solutionPath),
            solution.SolutionProjects.First(p => Path.GetFileNameWithoutExtension(p.FilePath) == projectName).FilePath
        );
        IEnumerable<string> arguments = ["msbuild", "-t:IncludeTransitiveProjectReferences", "-getItem:ProjectReference", projectPath];

        string executableName;
        if (OperatingSystem.IsWindows())
        {
            using Process whereProcess = Process.Start(
                new ProcessStartInfo(
                    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                    ["/c", "where dotnet"]
                )
                {
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                }
            )!;

            await whereProcess.WaitForExitAsync();
            executableName = await whereProcess.StandardOutput.ReadToEndAsync();
        }
        else
        {
            executableName = "/usr/bin/env";
            arguments = arguments.Prepend("dotnet");
        }

        using var process = Process.Start(
            new ProcessStartInfo(executableName, arguments)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            }
        );
        await process!.WaitForExitAsync();
        return (await JsonSerializer.DeserializeAsync(process.StandardOutput.BaseStream, SourceGenerationContext.Default.MSBuildEvaluationOutput))!
            .Items
            .ProjectReference
            .Select(p => p.Filename)
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
