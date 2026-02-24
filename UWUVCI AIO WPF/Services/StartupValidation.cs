namespace UWUVCI_AIO_WPF.Services
{
    internal readonly struct StartupCheckResult
    {
        internal bool Success { get; }
        internal string Reason { get; }

        internal StartupCheckResult(bool success, string reason)
        {
            Success = success;
            Reason = reason ?? string.Empty;
        }
    }

    internal static class StartupValidation
    {
        internal static StartupCheckResult CheckLocalInstall()
        {
            bool ok = LocalInstallGuard.ValidateLocalInstall();
            return new StartupCheckResult(ok, ok ? "ok" : "local-install-check-failed");
        }

        internal static StartupCheckResult CheckReleaseIntegrity()
        {
            bool ok = ReleaseSignatureVerifier.ValidateReleaseIntegrity(out var reason);
            return new StartupCheckResult(ok, reason);
        }
    }
}

