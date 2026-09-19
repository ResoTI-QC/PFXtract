using System.Security.Cryptography.X509Certificates;

namespace PFXtract.Core;

/// <summary>
/// Regroupe les certificats chargés depuis un PFX et garantit leur libération.
/// </summary>
public sealed class CertificateBundle : IDisposable
{
    public CertificateBundle(IEnumerable<X509Certificate2> certificates)
    {
        Certificates = certificates.ToArray();
    }

    /// <summary>Certificats présents dans le conteneur, y compris les autorités.</summary>
    public IReadOnlyList<X509Certificate2> Certificates { get; }

    public void Dispose()
    {
        // X509Certificate2 peut détenir des ressources cryptographiques natives.
        foreach (var certificate in Certificates)
            certificate.Dispose();
    }
}
