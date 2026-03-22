using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Xunit;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Shared test fixture that launches a single MCP server process for all
/// integration tests. The process starts once during initialization and
/// is cleaned up during disposal.
/// </summary>
public sealed class ServerFixture : IAsyncLifetime
{
    private const int ServerReadyDelayMs = 500;
    private readonly string _serverAssemblyPath;

    public ServerFixture()
    {
        _serverAssemblyPath = ResolveServerAssemblyPath();
    }

    /// <summary>
    /// Gets the running server process.
    /// </summary>
    public Process Process { get; private set; } = null!;

    /// <summary>
    /// Starts and returns a new independent server process ready for interaction.
    /// </summary>
    public async Task<Process> StartIndependentServerProcessAsync()
    {
        Process process = StartServerProcess(_serverAssemblyPath);
        await WaitForServerReadyAsync(process);
        return process;
    }

    /// <summary>
    /// Starts a server process with extra command-line arguments (for CLI mode testing).
    /// Does not wait for server-ready state since the process may exit immediately.
    /// </summary>
    /// <param name="extraArgs">Additional arguments appended after the assembly path.</param>
    public Process StartProcessWithArgs(string extraArgs)
    {
        return StartServerProcess(_serverAssemblyPath, extraArgs);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        Process = await StartIndependentServerProcessAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        try
        {
            if (!Process.HasExited)
            {
                Process.StandardInput.Close();
                if (!Process.WaitForExit(2000))
                {
                    Process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }

        Process.Dispose();
        return Task.CompletedTask;
    }

    private static string ResolveServerAssemblyPath()
    {
        string baseDir = AppContext.BaseDirectory;
        string? solutionDir = FindSolutionDirectory(baseDir);
        solutionDir.Should().NotBeNull("the solution directory must be found");

        string assemblyPath = Path.Combine(
            solutionDir!, "src", "PdfToMarkdown", "bin", "Debug", "net9.0", "PdfToMarkdown.dll");
        File.Exists(assemblyPath).Should().BeTrue($"the server assembly must exist at {assemblyPath}");

        return assemblyPath;
    }

    private static Process StartServerProcess(string assemblyPath, string? extraArgs = null)
    {
        string arguments = $"\"{assemblyPath}\"";
        if (!string.IsNullOrEmpty(extraArgs))
        {
            arguments += $" {extraArgs}";
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        Process process = new() { StartInfo = startInfo };
        process.Start();
        return process;
    }

    private static async Task WaitForServerReadyAsync(Process process)
    {
        // The compiled exe starts in ~50ms. A 500ms delay provides ample margin.
        await Task.Delay(ServerReadyDelayMs);
        process.HasExited.Should().BeFalse("the server should not have exited during startup");
    }

    private static string? FindSolutionDirectory(string startDir)
    {
        string? dir = startDir;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }
}
