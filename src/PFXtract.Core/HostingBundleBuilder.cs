using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PFXtract.Core;

public sealed record HostingBundle(
    string CertificateCrt,
    string PrivateKey,
    string CertificateAuthorityBundle,
    string CertificateName,
    int AuthorityCount);

public static class HostingBundleBuilder
{
    public static HostingBundle Build(IEnumerable<X509Certificate2> certificates)
    {
        ArgumentNullException.ThrowIfNull(certificates);
        var all = certificates.ToArray();
        if (all.Length == 0)
            throw new ArgumentException("Aucun certificat n’a été sélectionné.", nameof(certificates));

        var leaf = all.FirstOrDefault(c => c.HasPrivateKey && !IsCertificateAuthority(c))
            ?? all.FirstOrDefault(c => c.HasPrivateKey)
            ?? throw new CryptographicException("Aucune clé privée n’est présente pour les certificats sélectionnés.");

        var authorities = OrderAuthorities(leaf, all.Where(c => !ReferenceEquals(c, leaf) && IsCertificateAuthority(c)));
        var certificatePem = PemEncoding.WriteString("CERTIFICATE", leaf.RawData) + Environment.NewLine;
        var privateKeyPem = ExportUnencryptedPrivateKey(leaf);
        var authorityPem = string.Join(Environment.NewLine,
            authorities.Select(c => PemEncoding.WriteString("CERTIFICATE", c.RawData)));
        if (authorityPem.Length > 0)
            authorityPem += Environment.NewLine;

        var name = leaf.GetNameInfo(X509NameType.SimpleName, false);
        return new HostingBundle(certificatePem, privateKeyPem, authorityPem,
            string.IsNullOrWhiteSpace(name) ? "Certificat" : name,
            authorities.Count);
    }

    private static string ExportUnencryptedPrivateKey(X509Certificate2 certificate)
    {
        using AsymmetricAlgorithm? key = certificate.GetRSAPrivateKey()
            ?? (AsymmetricAlgorithm?)certificate.GetECDsaPrivateKey()
            ?? certificate.GetDSAPrivateKey();
        if (key is null)
            throw new CryptographicException("L’algorithme de la clé privée n’est pas pris en charge.");

        try
        {
            return ToPem("PRIVATE KEY", key.ExportPkcs8PrivateKey());
        }
        catch (CryptographicException)
        {
            // Certaines clés CNG importées depuis un PFX refusent l’export PKCS#8 en clair,
            // mais permettent l’export chiffré. On les transpose alors entièrement en mémoire.
            return ExportThroughEncryptedPkcs8(key);
        }
    }

    private static string ExportThroughEncryptedPkcs8(AsymmetricAlgorithm source)
    {
        var temporaryPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToCharArray();
        byte[]? encrypted = null;
        try
        {
            encrypted = source.ExportEncryptedPkcs8PrivateKey(temporaryPassword,
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 10_000));
            using AsymmetricAlgorithm destination = source switch
            {
                RSA => RSA.Create(),
                ECDsa => ECDsa.Create(),
                DSA => DSA.Create(),
                _ => throw new CryptographicException("L’algorithme de la clé privée n’est pas pris en charge.")
            };
            destination.ImportEncryptedPkcs8PrivateKey(temporaryPassword, encrypted, out _);
            return ToPem("PRIVATE KEY", destination.ExportPkcs8PrivateKey());
        }
        finally
        {
            Array.Fill(temporaryPassword, '\0');
            if (encrypted is not null)
                CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private static string ToPem(string label, byte[] keyBytes)
    {
        try
        {
            return PemEncoding.WriteString(label, keyBytes) + Environment.NewLine;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    private static List<X509Certificate2> OrderAuthorities(
        X509Certificate2 leaf,
        IEnumerable<X509Certificate2> authorities)
    {
        var remaining = authorities.ToList();
        var ordered = new List<X509Certificate2>();
        var issuer = leaf.IssuerName.RawData;

        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(c => c.SubjectName.RawData.AsSpan().SequenceEqual(issuer));
            if (next is null) break;
            ordered.Add(next);
            remaining.Remove(next);
            issuer = next.IssuerName.RawData;
        }

        ordered.AddRange(remaining);
        return ordered;
    }

    private static bool IsCertificateAuthority(X509Certificate2 certificate) =>
        certificate.Extensions.OfType<X509BasicConstraintsExtension>()
            .Any(extension => extension.CertificateAuthority);
}
