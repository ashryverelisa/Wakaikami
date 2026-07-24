// End-to-end local Kubernetes deployment: creates the kind cluster (if missing), builds and loads
// the container images, creates the cert secret from src/certs and applies all manifests.
//
// Prerequisites: docker, kind, kubectl.
// Usage: dotnet run scripts/deploy-kind.cs [-- --skip-build]

using System.Diagnostics;

const string clusterName = "wakaikami";

var skipBuild = args.Contains("--skip-build");
var repoRoot = FindRepoRoot();

string[] images =
[
    "wakaikami/migrator:latest",
    "wakaikami/login-service:latest",
    "wakaikami/world-service:latest",
    "wakaikami/zone-service:latest",
    "wakaikami/web:latest",
];

if (!skipBuild)
    Run("dotnet", ["run", Path.Combine(repoRoot, "scripts", "build-images.cs")]);

var clusters = Capture("kind", ["get", "clusters"]);
if (!clusters.Split('\n').Select(line => line.Trim()).Contains(clusterName))
{
    Console.WriteLine($"==> Creating kind cluster '{clusterName}'");
    Run("kind", ["create", "cluster", "--config", Path.Combine(repoRoot, "k8s", "kind", "config.yaml")]);
}

Console.WriteLine("==> Loading images into kind");
Run("kind", ["load", "docker-image", "--name", clusterName, .. images]);

Console.WriteLine("==> Applying manifests");
Run("kubectl", ["apply", "-f", Path.Combine(repoRoot, "k8s", "00-namespace.yaml")]);

// mTLS certs as a secret (not part of the yaml manifests: binary files, dev-generated).
// create --dry-run | apply makes re-runs idempotent.
var certSecretYaml = Capture(
    "kubectl",
    [
        "-n",
        "wakaikami",
        "create",
        "secret",
        "generic",
        "wakaikami-certs",
        $"--from-file={Path.Combine(repoRoot, "src", "certs")}",
        "--dry-run=client",
        "-o",
        "yaml",
    ]
);
Run("kubectl", ["apply", "-f", "-"], stdin: certSecretYaml);

Run("kubectl", ["apply", "-f", Path.Combine(repoRoot, "k8s")]);

Console.WriteLine("==> Waiting for migrator job");
Run("kubectl", ["-n", "wakaikami", "wait", "--for=condition=complete", "job/migrator", "--timeout=300s"]);

Console.WriteLine("==> Waiting for deployments");
Run(
    "kubectl",
    [
        "-n",
        "wakaikami",
        "rollout",
        "status",
        "deployment/login",
        "deployment/world",
        "deployment/zone",
        "deployment/web",
        "--timeout=300s",
    ]
);

Console.WriteLine("==> Done");
Run("kubectl", ["-n", "wakaikami", "get", "pods"]);
Console.WriteLine();
Console.WriteLine("Web UI:      http://localhost:8080");
Console.WriteLine("Login game:  127.0.0.1:9010 | World game: 127.0.0.1:9120 | Zone game: 127.0.0.1:9230");
return;

static void Run(string command, string[] arguments, string? stdin = null)
{
    var startInfo = new ProcessStartInfo(command)
    {
        UseShellExecute = false,
        RedirectStandardInput = stdin is not null,
    };
    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    using var process =
        Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Failed to start '{command}'.");
    if (stdin is not null)
    {
        process.StandardInput.Write(stdin);
        process.StandardInput.Close();
    }
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException(
            $"'{command} {string.Join(' ', arguments)}' exited with code {process.ExitCode}."
        );
}

static string Capture(string command, string[] arguments)
{
    var startInfo = new ProcessStartInfo(command)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
    };
    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    using var process =
        Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Failed to start '{command}'.");
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException(
            $"'{command} {string.Join(' ', arguments)}' exited with code {process.ExitCode}."
        );
    return output;
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
