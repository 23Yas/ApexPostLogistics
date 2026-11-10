namespace ApexPost.Api.DTOs;

/// <summary>
/// Response body for a bulk scan upload: counts of accepted, de-duplicated and rejected scans.
/// </summary>
public sealed class ScanUploadResponse
{
    /// <summary>Number of scans persisted.</summary>
    public int Accepted { get; init; }

    /// <summary>Number of scans skipped because their ID was already stored.</summary>
    public int DuplicatesSkipped { get; init; }

    /// <summary>Number of scans rejected by validation.</summary>
    public int Rejected { get; init; }

    /// <summary>Per-scan rejection reasons (scan ID plus reason code).</summary>
    public IReadOnlyList<ScanRejectionDto> Rejections { get; init; } = Array.Empty<ScanRejectionDto>();
}

/// <summary>
/// Reason a single scan was rejected during bulk upload.
/// </summary>
public sealed class ScanRejectionDto
{
    /// <summary>ID of the rejected scan.</summary>
    public Guid ScanId { get; init; }

    /// <summary>Machine-readable rejection reason.</summary>
    public string Reason { get; init; } = string.Empty;
}
