namespace CDriveSweep.Core.Models;

/// <summary>
/// 某次清理操作的结果
/// </summary>
public class CleanResult
{
    public string Category { get; init; } = string.Empty;
    public bool Success { get; init; }
    public long FreedBytes { get; init; }
    public int ItemsCleaned { get; init; }
    public int ItemsFailed { get; init; }
    public List<string> Errors { get; init; } = new();

    public string FreedSizeDisplay => FreedBytes switch
    {
        >= 1_073_741_824 => $"{FreedBytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{FreedBytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{FreedBytes / 1024.0:F2} KB",
        _ => $"{FreedBytes} B"
    };
}
