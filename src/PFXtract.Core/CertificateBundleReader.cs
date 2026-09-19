using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PFXtract.Core;

/// <summary>Valide et charge les conteneurs PKCS#12 (.pfx et .p12).</summary>
public static class CertificateBundleReader
{
    /// <summary>
    /// Charge tous les certificats et leurs éventuelles clés privées sans les
    /// installer dans le magasin de certificats Windows.
    /// </summary>
    public static CertificateBundle Load(string path, string? password)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Le chemin du fichier est requis.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Le fichier PFX est introuvable.", path);

        var extension = Path.GetExtension(path);
        if (!extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".p12", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Le fichier doit porter l’extension .pfx ou .p12.", nameof(path));

        var collection = new X509Certificate2Collection();
        try
        {
            // EphemeralKeySet empêche toute persistance silencieuse des clés sur le poste.
            // Exportable est requis pour produire les fichiers KEY demandés par l’utilisateur.
            collection.Import(path, password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

            if (collection.Count == 0)
                throw new CryptographicException("Ce fichier ne contient aucun certificat.");

            return new CertificateBundle(collection.Cast<X509Certificate2>());
        }
        catch
        {
            foreach (var certificate in collection)
                certificate.Dispose();
            throw;
        }
    }
}
