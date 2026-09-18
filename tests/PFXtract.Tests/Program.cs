using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PFXtract.Core;

var testRoot = Path.Combine(Path.GetTempPath(), "pfxtract-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);

try
{
    const string password = "test-Secret-42";
    var pfxPath = Path.Combine(testRoot, "exemple.pfx");
    var outputPath = Path.Combine(testRoot, "sortie");

    using var caKey = RSA.Create(2048);
    var caRequest = new CertificateRequest("CN=Autorite Test", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
    using var ca = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(1));

    using var leafKey = RSA.Create(2048);
    var leafRequest = new CertificateRequest("CN=Certificat Test", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    leafRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
    var serial = RandomNumberGenerator.GetBytes(16);
    using var signedLeaf = leafRequest.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30), serial);
    using var leaf = signedLeaf.CopyWithPrivateKey(leafKey);

    using var publicCa = new X509Certificate2(ca.Export(X509ContentType.Cert));
    var pfxCollection = new X509Certificate2Collection { leaf, publicCa };
    File.WriteAllBytes(pfxPath, pfxCollection.Export(X509ContentType.Pfx, password)!);

    using var bundle = CertificateBundleReader.Load(pfxPath, password);
    Assert(bundle.Certificates.Count == 2, "Le PFX doit contenir le certificat final et son CA.");
    Assert(bundle.Certificates.Count(c => c.HasPrivateKey) == 1, "Le certificat final doit avoir sa clé privée avant export.");
    Assert(bundle.Certificates.Any(c => c.Extensions.OfType<X509BasicConstraintsExtension>().Any(e => e.CertificateAuthority)),
        "Le certificat CA doit être détecté.");

    var result = CertificateExporter.Export(bundle.Certificates, outputPath,
        CertificateExportFormat.Cer | CertificateExportFormat.Pem);
    Assert(result.CertificateCount == 2, "Les deux certificats doivent être exportés.");
    Assert(result.Files.Count == 4, "Les formats CER et PEM doivent être créés pour chaque certificat.");
    Assert(result.Files.All(File.Exists), "Tous les fichiers annoncés doivent exister.");

    var cerPaths = result.Files.Where(path => path.EndsWith(".cer", StringComparison.OrdinalIgnoreCase)).ToArray();
    var cerPath = cerPaths.Single(path => new X509Certificate2(path).Thumbprint == leaf.Thumbprint);
    using var exported = new X509Certificate2(cerPath);
    Assert(!exported.HasPrivateKey, "Le CER exporté ne doit jamais contenir la clé privée.");
    Assert(exported.Thumbprint == leaf.Thumbprint, "Le certificat exporté doit correspondre à l’original.");

    var pemPath = result.Files.First(path => path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase));
    var pem = File.ReadAllText(pemPath);
    Assert(pem.Contains("BEGIN CERTIFICATE") && pem.Contains("END CERTIFICATE"), "Le PEM doit être valide.");

    var complete = CertificateExporter.Export(bundle.Certificates, outputPath,
        new CertificateExportOptions(0, true, "Export-Secret-42", true));
    Assert(complete.PrivateKeyCount == 1, "Une clé privée doit être exportée.");
    var keyPath = complete.Files.Single(path => path.EndsWith(".key", StringComparison.OrdinalIgnoreCase));
    var keyPem = File.ReadAllText(keyPath);
    Assert(keyPem.Contains("BEGIN ENCRYPTED PRIVATE KEY"), "La clé privée doit être chiffrée.");
    using var importedKey = RSA.Create();
    importedKey.ImportFromEncryptedPem(keyPem, "Export-Secret-42");
    Assert(importedKey.ExportParameters(false).Modulus!.SequenceEqual(leafKey.ExportParameters(false).Modulus!),
        "La clé privée chiffrée doit pouvoir être réimportée.");
    var chainPath = complete.Files.Single(path => Path.GetFileName(path).StartsWith("chaine-complete"));
    Assert(File.ReadAllText(chainPath).Split("BEGIN CERTIFICATE").Length - 1 == 2,
        "La chaîne complète doit contenir le certificat final et le CA.");

    var hosting = HostingBundleBuilder.Build(bundle.Certificates);
    Assert(hosting.CertificateCrt.Contains("BEGIN CERTIFICATE"), "Le bloc CRT doit être généré.");
    Assert(hosting.PrivateKey.Contains("BEGIN PRIVATE KEY"), "Le bloc KEY PKCS#8 doit être généré.");
    Assert(hosting.CertificateAuthorityBundle.Split("BEGIN CERTIFICATE").Length - 1 == 1,
        "Le CABUNDLE doit contenir le CA.");
    using var hostingKey = RSA.Create();
    hostingKey.ImportFromPem(hosting.PrivateKey);
    Assert(hostingKey.ExportParameters(false).Modulus!.SequenceEqual(leafKey.ExportParameters(false).Modulus!),
        "La clé du bloc KEY doit correspondre au certificat final.");

    var duplicate = CertificateExporter.Export(bundle.Certificates, outputPath, CertificateExportFormat.Cer);
    Assert(duplicate.Files.Count == 2 && duplicate.Files.All(path => !cerPaths.Contains(path)),
        "Un export existant ne doit pas être écrasé.");

    var wrongPasswordRejected = false;
    try { using var ignored = CertificateBundleReader.Load(pfxPath, "mauvais"); }
    catch (CryptographicException) { wrongPasswordRejected = true; }
    Assert(wrongPasswordRejected, "Un mauvais mot de passe doit être refusé.");

    Console.WriteLine("Tous les tests sont réussis.");
}
finally
{
    Directory.Delete(testRoot, true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("ÉCHEC : " + message);
    Console.WriteLine("OK — " + message);
}
