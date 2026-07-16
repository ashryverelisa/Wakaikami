using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Wakaikami.Networking.Grpc.Helpers;

public static class GrpcCertHelper
{
    private const string CertPassword = "dev-only";
    private const string CertSubDir = "certs";

    private static X509Certificate2? _cachedCa;

    /// <summary>
    /// A <see cref="RemoteCertificateValidationCallback"/> that validates the server certificate
    /// against the Dev-CA (self-signed, not in the OS trust store). Accepts the certificate when it
    /// chains to the Dev-CA; rejects everything else. Uses a cached CA so the file is only read once.
    /// </summary>
    public static RemoteCertificateValidationCallback DevCaValidator =>
        (_, certificate, _, errors) =>
        {
            if (certificate is null)
                return false;

            // Dev-only: treat self-signed certs that match our CA thumbprint as trusted, even when
            // the OS trust store doesn't contain the CA.
            if (
                errors != SslPolicyErrors.RemoteCertificateChainErrors
                || certificate is not X509Certificate2 cert2
            )
                return errors == SslPolicyErrors.None;

            _cachedCa ??= LoadDevCaCert();
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(_cachedCa);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain.Build(cert2);
        };

    public static X509Certificate2 LoadLoginServerCert() =>
        X509CertificateLoader.LoadPkcs12FromFile(ResolveCertPath("login.pfx"), CertPassword);

    public static X509Certificate2 LoadWorldServerCert() =>
        X509CertificateLoader.LoadPkcs12FromFile(ResolveCertPath("world.pfx"), CertPassword);

    public static X509Certificate2 LoadWorldClientCert() =>
        X509CertificateLoader.LoadPkcs12FromFile(ResolveCertPath("world-client.pfx"), CertPassword);

    public static X509Certificate2 LoadZoneClientCert() =>
        X509CertificateLoader.LoadPkcs12FromFile(ResolveCertPath("zone-client.pfx"), CertPassword);

    public static SocketsHttpHandler CreateClientHandler(X509Certificate2 clientCert) =>
        new()
        {
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            EnableMultipleHttp2Connections = true,
            SslOptions = new SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection { clientCert },
                RemoteCertificateValidationCallback = DevCaValidator,
            },
        };

    public static X509Certificate2 LoadDevCaCert() =>
        X509CertificateLoader.LoadCertificateFromFile(ResolveCertPath("dev-ca.cer"));

    public static bool ValidateClientCertificate(
        X509Certificate2? cert,
        X509Chain? chain,
        SslPolicyErrors errors
    )
    {
        if (cert is null)
            return false;

        _cachedCa ??= LoadDevCaCert();
        using var customChain = new X509Chain();
        customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        customChain.ChainPolicy.CustomTrustStore.Add(_cachedCa);
        customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        customChain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.2")); // Client Auth EKU
        return customChain.Build(cert);
    }

    private static string ResolveCertPath(string fileName)
    {
        var assemblyDir = AppContext.BaseDirectory;
        for (var dir = assemblyDir; dir != null; dir = Path.GetDirectoryName(dir))
        {
            var candidate = Path.Combine(dir, CertSubDir, fileName);
            if (File.Exists(candidate))
                return candidate;

            // Don't walk past the repo root (heuristic: look for the .git directory)
            if (Directory.Exists(Path.Combine(dir!, ".git")))
                break;
        }

        var cwd = Path.Combine(Directory.GetCurrentDirectory(), CertSubDir, fileName);
        return File.Exists(cwd)
            ? cwd
            : throw new FileNotFoundException(
                $"Cannot find {CertSubDir}/{fileName}. Ensure the certs/ directory is present at the repo root.",
                fileName
            );
    }
}
