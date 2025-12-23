namespace Portfolio.Core;

internal class Constants
{
    #region General
    #endregion
    #region Errors
    public const string ERROR_INVALID_ID = "The provided ID is invalid.";
    public const string ERROR_NOT_FOUND = "The requested resource was not found.";
    #endregion
    #region Debug
    public const string DEBUG_REQUEST_RECEIVED = "Debug: Request received.";
    public const string DEBUG_RESPONSE_SENT = "Debug: Response sent.";

    // X509CertificateService debug messages
    public const string DEBUG_CACHE_HIT = "GetCertificateByName: cache hit for '{0}'. Cached is null: {1}";
    public const string DEBUG_CACHED_EXPORT_PRESENT = "GetCertificateByName: cached export present for '{0}', validating presence in store before returning reconstructed cert.";
    public const string DEBUG_VALIDATE_STORE_FIND = "GetCertificateByName: validate store find returned {0} results for thumb {1}.";
    public const string DEBUG_CACHED_ENTRY_NO_EXPORT = "GetCertificateByName: cached entry present for '{0}' but no cached export - validating in store.";
    public const string DEBUG_FRESH_LOOKUP = "GetCertificateByName: fresh lookup by subject '{0}' returned {1} results.";
    public const string DEBUG_EXPORT_SUCCEEDED = "GetCertificateByName: export succeeded for '{0}', size={1}.";
    public const string DEBUG_EXPORT_FAILED = "GetCertificateByName: export failed for '{0}'.";
    public const string DEBUG_CACHED_ENTRY_CACHED = "GetCertificateByName: cached entry for '{0}' (HasExport={1}).";
    public const string DEBUG_RECONSTRUCTION_FAILED = "GetCertificateByName: reconstruction from export failed for '{0}', falling back to copy of store certificate.";
    public const string DEBUG_NO_CANDIDATES = "GetCertificateByName: no candidates found for '{0}' - caching negative result.";
    #endregion
}
