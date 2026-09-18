using System.Security.Cryptography.X509Certificates;

namespace PFXtract.Core;

public sealed class CertificateBundle : IDisposable
{
    public CertificateBundle(IEnumerable<X509Certificate2> certificates)
    {
        Certificates = certificates.ToArray();
    }

    public IReadOnlyList<X509Certificate2> Certificates { get; }

    public void Dispose()
    {
        foreach (var certificate in Certificates)
            certificate.Dispose();
    }
}
