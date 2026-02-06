using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace UWUVCI_AIO_WPF.Services
{
    internal static class ReleaseSignatureVerifier
    {
        private const string SignatureFileName = "uwuvci.sig";
        private const bool EnforceSignature = true;

        // BEGIN_PUBLIC_KEY
        private const string PublicKeyXml = @"";
        // END_PUBLIC_KEY

        public static bool IsOfficialBuild { get; private set; }

        public static bool Verify(out string reason)
        {
            if (!EnforceSignature)
            {
                IsOfficialBuild = true;
                reason = "signature-check-disabled";
                return true;
            }

            try
            {
                var exePath = GetExecutablePath();
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    reason = "exe-not-found";
                    IsOfficialBuild = false;
                    return false;
                }

                var sigPath = Path.Combine(Path.GetDirectoryName(exePath) ?? string.Empty, SignatureFileName);
                if (!File.Exists(sigPath))
                {
                    reason = "signature-missing";
                    IsOfficialBuild = false;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(PublicKeyXml))
                {
                    reason = "public-key-missing";
                    IsOfficialBuild = false;
                    return false;
                }

                var signature = Convert.FromBase64String(File.ReadAllText(sigPath, Encoding.UTF8).Trim());
                var hash = ComputeSha256(exePath);

                using var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(PublicKeyXml);

                var verified = rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signature);
                IsOfficialBuild = verified;
                reason = verified ? "ok" : "signature-invalid";
                return verified;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ReleaseSignatureVerifier] Error: {ex}");
                reason = "exception";
                IsOfficialBuild = false;
                return false;
            }
        }

        private static string GetExecutablePath()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                if (!string.IsNullOrWhiteSpace(asm.Location))
                    return asm.Location;
            }
            catch { }

            try
            {
                return Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return sha.ComputeHash(stream);
        }
    }
}
