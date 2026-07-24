// Builds Linux container images for all deployable services via the .NET SDK container publish
// (no Dockerfiles). Images land in the local Docker daemon as wakaikami/<service>:<tag>.
//
// Usage: dotnet run scripts/build-images.cs [-- <tag>]   (default tag: latest)

using System.Diagnostics;

var tag = args.Length > 0 ? args[0] : "latest";
var srcDir = Path.Combine(FindRepoRoot(), "src");

string[] projects =
[
    "Wakaikami.Database.Migrations",
    "Wakaikami.LoginService",
    "Wakaikami.WorldService",
    "Wakaikami.ZoneService",
    "Wakaikami.Web",
];

foreach (var project in projects)
{
    Console.WriteLine($"==> Publishing container image for {project}");
    Run(
        "dotnet",
        [
            "publish",
            Path.Combine(srcDir, project),
            "-c",
            "Release",
            "--os",
            "linux",
            "--arch",
            "x64",
            "/t:PublishContainer",
            $"-p:ContainerImageTag={tag}",
        ]
    );
}

Console.WriteLine("==> Done. Images:");
Run(
    "docker",
    ["images", "--filter", "reference=wakaikami/*", "--format", "{{.Repository}}:{{.Tag}}  {{.Size}}"]
);
return;

static void Run(string command, string[] arguments)
{
    var startInfo = new ProcessStartInfo(command) { UseShellExecute = false };
    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    using var process =
        Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Failed to start '{command}'.");
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException(
            $"'{command} {string.Join(' ', arguments)}' exited with code {process.ExitCode}."
        );
}

static string FindRepoRoot()
{
    // File-based apps run from a temp dir; search from the CWD (expected: the repo checkout).
    for (var dir = Directory.GetCurrentDirectory(); dir != null; dir = Path.GetDirectoryName(dir))
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
            return dir;
    }
    throw new InvalidOperationException("Could not locate the repo root (no .git directory found).");
}
