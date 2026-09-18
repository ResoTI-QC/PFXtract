using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace PFXtract.Core;

[Flags]
public enum CertificateExportFormat
{
    Cer = 1,
    Pem = 2
}

public sealed record CertificateExportOptions(
    CertificateExportFormat Formats,
    bool ExportPrivateKeys = false,
    string? PrivateKeyPassword = null,
    bool ExportFullChain = false);

public sealed record ExportResult(int CertificateCount, int PrivateKeyCount, IReadOnlyList<string> Files);

public static partial class CertificateExporter
{
    public static ExportResult Export(
        IEnumerable<X509Certificate2> certificates,
        string outputDirectory,
        CertificateExportFormat formats)
        => Export(certificates, outputDirectory, new CertificateExportOptions(formats));

    public static ExportResult Export(
        IEnumerable<X509Certificate2> certificates,
        string outputDirectory,
        CertificateExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(certificates);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Le dossier de destination est requis.", nameof(outputDirectory));
        if ((options.Formats & (CertificateExportFormat.Cer | CertificateExportFormat.Pem)) == 0 &&
            !options.ExportPrivateKeys && !options.ExportFullChain)
            throw new ArgumentException("Sélectionnez au moins un élément à exporter.", nameof(options));
        if (options.ExportPrivateKeys && string.IsNullOrWhiteSpace(options.PrivateKeyPassword))
            throw new ArgumentException("Un mot de passe est requis pour chiffrer les clés privées.", nameof(options));

        Directory.CreateDirectory(outputDirectory);
        var certificateList = certificates.ToArray();
        var files = new List<string>();
        var count = 0;
        var privateKeyCount = 0;

        foreach (var certificate in certificateList)
        {
            var baseName = BuildFileName(certificate, count + 1);
            if (options.Formats.HasFlag(CertificateExportFormat.Cer))
            {
                var path = NextAvailablePath(outputDirectory, baseName, ".cer");
                File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
                files.Add(path);
            }

            if (options.Formats.HasFlag(CertificateExportFormat.Pem))
            {
                var path = NextAvailablePath(outputDirectory, baseName, ".pem");
                var pem = PemEncoding.WriteString("CERTIFICATE", certificate.RawData);
                File.WriteAllText(path, pem + Environment.NewLine, new UTF8Encoding(false));
                files.Add(path);
            }

            if (options.ExportPrivateKeys && certificate.HasPrivateKey)
            {
                var path = NextAvailablePath(outputDirectory, baseName, ".key");
                var encryptedKey = ExportEncryptedPrivateKey(certificate, options.PrivateKeyPassword!);
                var pem = PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", encryptedKey);
                File.WriteAllText(path, pem + Environment.NewLine, new UTF8Encoding(false));
                CryptographicOperations.ZeroMemory(encryptedKey);
                files.Add(path);
                privateKeyCount++;
            }

            count++;
        }

        if (options.ExportFullChain && certificateList.Length > 0)
        {
            var path = NextAvailablePath(outputDirectory, "chaine-complete", ".pem");
            var chainPem = string.Join(Environment.NewLine,
                certificateList.Select(c => PemEncoding.WriteString("CERTIFICATE", c.RawData))) + Environment.NewLine;
            File.WriteAllText(path, chainPem, new UTF8Encoding(false));
            files.Add(path);
        }

        return new ExportResult(count, privateKeyCount, files);
    }

    private static byte[] ExportEncryptedPrivateKey(X509Certificate2 certificate, string password)
    {
        var parameters = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            100_000);

        using AsymmetricAlgorithm? key = certificate.GetRSAPrivateKey()
            ?? (AsymmetricAlgorithm?)certificate.GetECDsaPrivateKey()
            ?? certificate.GetDSAPrivateKey();

        if (key is null)
            throw new CryptographicException($"La clé privée de « {certificate.GetNameInfo(X509NameType.SimpleName, false)} » utilise un algorithme non pris en charge.");

        return key.ExportEncryptedPkcs8PrivateKey(password, parameters);
    }

    private static string BuildFileName(X509Certificate2 certificate, int index)
    {
        var name = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (string.IsNullOrWhiteSpace(name))
            name = $"certificat-{index}";

        name = InvalidFileCharacters().Replace(name.Trim(), "-");
        name = RepeatedSeparators().Replace(name, "-").Trim(' ', '.', '-');
        if (name.Length > 80)
            name = name[..80].TrimEnd(' ', '.', '-');

        return string.IsNullOrWhiteSpace(name) ? $"certificat-{index}" : name;
    }

    private static string NextAvailablePath(string directory, string baseName, string extension)
    {
        var path = Path.Combine(directory, baseName + extension);
        var suffix = 2;
        while (File.Exists(path))
            path = Path.Combine(directory, $"{baseName}-{suffix++}{extension}");
        return path;
    }

    [GeneratedRegex("[<>:\"/\\|?*\\x00-\\x1F]")]
    private static partial Regex InvalidFileCharacters();

    [GeneratedRegex("[\\s_-]{2,}")]
    private static partial Regex RepeatedSeparators();
}
