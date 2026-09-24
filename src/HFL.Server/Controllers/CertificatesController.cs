using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Server.Data;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/v1/certificates")]
    public class CertificatesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private static readonly object _syncLock = new();

        public CertificatesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCertificatesList()
        {
            var certs = new List<object>();

            // 1. Check existing certificate files on Linux / server
            string certPath = "/etc/ssl/certs/ssl-cert-snakeoil.pem";
            if (!System.IO.File.Exists(certPath))
            {
                certPath = Path.Combine(AppContext.BaseDirectory, "data", "certs", "hfl-wildcard.pem");
            }

            // 2. If certificate doesn't exist yet, dynamically generate real RSA-2048 x509 SAN Certificate on the fly
            string certPem = "";
            string thumbprint = "";

            if (System.IO.File.Exists(certPath))
            {
                certPem = await System.IO.File.ReadAllTextAsync(certPath);
                try
                {
                    var x509 = X509Certificate2.CreateFromPem(certPem);
                    thumbprint = x509.Thumbprint;
                }
                catch { }
            }
            else
            {
                var realCert = GetOrCreateRealSignedCertificate();
                certPem = realCert.ExportCertificatePem();
                thumbprint = realCert.Thumbprint;
            }

            certs.Add(new
            {
                domain = "*.local, *.internal, test.local",
                certificatePem = certPem,
                subject = "CN=HFL Razbloker Root CA, O=HFL Razbloker Enterprise, OU=Security",
                thumbprint = thumbprint
            });

            return Ok(certs);
        }

        [HttpGet("root-ca")]
        public IActionResult GetRootCa()
        {
            var realCert = GetOrCreateRealSignedCertificate();
            byte[] rawBytes = realCert.Export(X509ContentType.Cert);
            return File(rawBytes, "application/x-x509-ca-cert", "hfl-root-ca.crt");
        }

        private static X509Certificate2 GetOrCreateRealSignedCertificate()
        {
            lock (_syncLock)
            {
                string certDir = Path.Combine(AppContext.BaseDirectory, "data", "certs");
                if (!Directory.Exists(certDir)) Directory.CreateDirectory(certDir);

                string pemPath = Path.Combine(certDir, "hfl-wildcard.pem");
                string keyPath = Path.Combine(certDir, "hfl-wildcard.key");

                if (System.IO.File.Exists(pemPath) && System.IO.File.Exists(keyPath))
                {
                    try
                    {
                        return X509Certificate2.CreateFromPemFile(pemPath, keyPath);
                    }
                    catch { }
                }

                // Generate real cryptographically signed Certificate with SAN extension
                using var rsa = RSA.Create(2048);
                var req = new CertificateRequest(
                    "CN=*.local, O=HFL Razbloker Enterprise, OU=Security, C=RU",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                // Add Basic Constraints: CA = True
                req.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true)
                );

                // Add Subject Alternative Names (SAN) for all internal domains and IPs
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("*.local");
                sanBuilder.AddDnsName("local");
                sanBuilder.AddDnsName("*.internal");
                sanBuilder.AddDnsName("internal");
                sanBuilder.AddDnsName("test.local");
                sanBuilder.AddDnsName("panel.local");
                sanBuilder.AddIpAddress(IPAddress.Parse("31.77.8.9"));
                sanBuilder.AddIpAddress(IPAddress.Parse("127.0.0.1"));
                req.CertificateExtensions.Add(sanBuilder.Build());

                // Key Usage
                req.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        true
                    )
                );

                // Enhanced Key Usage (Server Auth, Client Auth)
                var eku = new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1"), // Server Auth
                    new Oid("1.3.6.1.5.5.7.3.2")  // Client Auth
                };
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

                // Sign for 10 years
                var now = DateTimeOffset.UtcNow;
                var cert = req.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));

                // Save PEM and KEY files to disk
                System.IO.File.WriteAllText(pemPath, cert.ExportCertificatePem());
                System.IO.File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

                return cert;
            }
        }
    }
}
