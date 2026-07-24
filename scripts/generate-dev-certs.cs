// Regenerates the dev PKI under src/certs: a self-signed Dev-CA plus server certs (login, world)
// and client certs (world-client, zone-client) for the internal gRPC mTLS links.
//
// Server cert SANs cover local runs (localhost / 127.0.0.1) and Kubernetes service DNS
// (login-service / world-service in the "wakaikami" namespace), so the same certs work under
// Aspire and inside a cluster.
//
// Usage: dotnet run scripts/generate-dev-certs.cs [-- <output-dir>]
//        (default output dir: src/certs relative to the repo root)

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

const string certPassword = "dev-only";
const string k8sNamespace = "wakaikami";

var outputDir =
    args.Length > 0 ? args[0] : Path.Combine(FindRepoRoot(), "src", "certs");
Directory.CreateDirectory(outputDir);

var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
var caNotAfter = DateTimeOffset.UtcNow.AddYears(5);
var leafNotAfter = DateTimeOffset.UtcNow.AddYears(2);

// --- Dev CA -------------------------------------------------------------------------------------
using var caKey = RSA.Create(4096);
var caRequest = new CertificateRequest(
    "CN=Wakaikami Dev CA",
    caKey,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1
);
caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
caRequest.CertificateExtensions.Add(
    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true)
);
caRequest.CertificateExtensions.Add(
    new X509SubjectKeyIdentifierExtension(caRequest.PublicKey, false)
);
using var caCert = caRequest.CreateSelfSigned(notBefore, caNotAfter);

File.WriteAllBytes(Path.Combine(outputDir, "dev-ca.cer"), caCert.Export(X509ContentType.Cert));
File.WriteAllText(
    Path.Combine(outputDir, "dev-ca-base64.txt"),
    Convert.ToBase64String(caCert.Export(X509ContentType.Cert))
);

// --- Leaf certs ---------------------------------------------------------------------------------
var serverEku = new Oid("1.3.6.1.5.5.7.3.1"); // Server Auth
var clientEku = new Oid("1.3.6.1.5.5.7.3.2"); // Client Auth

var login = CreateLeaf("login", "login.pfx", serverEku, ServiceSans("login-service"));
var world = CreateLeaf("world", "world.pfx", serverEku, ServiceSans("world-service"));
var worldClient = CreateLeaf("world-client", "world-client.pfx", clientEku, sans: null);
var zoneClient = CreateLeaf("zone-client", "zone-client.pfx", clientEku, sans: null);

File.WriteAllText(
    Path.Combine(outputDir, "thumbprints.txt"),
    $"""
    CA:           {caCert.Thumbprint}
    Login Server: {login}
    World Server: {world}
    World Client: {worldClient}
    Zone Client:  {zoneClient}
    """ + Environment.NewLine
);

Console.WriteLine($"Dev certs written to {outputDir}");
Console.WriteLine(File.ReadAllText(Path.Combine(outputDir, "thumbprints.txt")));
return;

string CreateLeaf(string name, string fileName, Oid eku, SubjectAlternativeNameBuilder? sans)
{
    using var key = RSA.Create(2048);
    var request = new CertificateRequest(
        $"CN={(sans is null ? name : "localhost")}",
        key,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1
    );
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    request.CertificateExtensions.Add(
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            true
        )
    );
    request.CertificateExtensions.Add(
        new X509EnhancedKeyUsageExtension(new OidCollection { eku }, true)
    );
    request.CertificateExtensions.Add(
        new X509SubjectKeyIdentifierExtension(request.PublicKey, false)
    );
    if (sans is not null)
        request.CertificateExtensions.Add(sans.Build());

    var serial = new byte[16];
    RandomNumberGenerator.Fill(serial);
    using var signed = request.Create(caCert, notBefore, leafNotAfter, serial);
    using var withKey = signed.CopyWithPrivateKey(key);

    File.WriteAllBytes(
        Path.Combine(outputDir, fileName),
        withKey.Export(X509ContentType.Pfx, certPassword)
    );
    return withKey.Thumbprint;
}

SubjectAlternativeNameBuilder ServiceSans(string serviceName)
{
    var builder = new SubjectAlternativeNameBuilder();
    builder.AddDnsName("localhost");
    builder.AddDnsName("127.0.0.1");
    builder.AddIpAddress(IPAddress.Loopback);
    builder.AddDnsName(serviceName);
    builder.AddDnsName($"{serviceName}.{k8sNamespace}");
    builder.AddDnsName($"{serviceName}.{k8sNamespace}.svc");
    builder.AddDnsName($"{serviceName}.{k8sNamespace}.svc.cluster.local");
    return builder;
}

static string FindRepoRoot()
{
    for (var dir = AppContext.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir))
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
            return dir;
    }
    // File-based apps run from a temp dir; fall back to the current working directory.
    for (var dir = Directory.GetCurrentDirectory(); dir != null; dir = Path.GetDirectoryName(dir))
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
            return dir;
    }
    throw new InvalidOperationException("Could not locate the repo root (no .git directory found).");
}
