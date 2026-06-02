using System.Diagnostics;
using System.Net.Http.Headers;

namespace KimaiDotNet.Reporting.ODataService.Tests.Integration;

internal static class KimaiDockerComposeHelper
{
    private const string KimaiBaseUrl = "http://localhost:8001";
    private const string ApiToken = "kimai-local-integration-test-token";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private static readonly Lazy<Task> Startup = new(StartAsync);

    public static Task EnsureStartedAsync() => Startup.Value;

    private static async Task StartAsync()
    {
        var composeFile = FindComposeFile();
        var workingDirectory = Path.GetDirectoryName(composeFile);
        if (workingDirectory == null)
        {
            throw new InvalidOperationException("Failed to resolve docker compose working directory.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("compose");
        startInfo.ArgumentList.Add("up");
        startInfo.ArgumentList.Add("-d");

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start docker compose.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
            $"docker compose up -d failed (exit {process.ExitCode}).\n{stderr}\n{stdout}");
        }

        await WaitForKimaiReadyAsync();
    }

    private static string FindComposeFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "docker-compose.yml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("docker-compose.yml not found from test base directory.");
    }

    private static async Task WaitForKimaiReadyAsync()
    {
        using var http = new HttpClient
        {
            BaseAddress = new Uri(KimaiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);

        var deadline = DateTime.UtcNow.Add(StartupTimeout);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync("/api/ping");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Retry until timeout.
            }

            await Task.Delay(PollInterval);
        }

        throw new InvalidOperationException("Kimai API did not become ready before the timeout.");
    }
}
