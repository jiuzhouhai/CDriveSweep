namespace CDriveSweep.Core.Models;

/// <summary>
/// 某个清理器的扫描结果
/// </summary>
public class ScanResult
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<ScanItem> Items { get; init; } = new();
    public long TotalSizeBytes { get; init; }
    public int FileCount => Items.Count;
    public bool HasItems => Items.Count > 0;

    public string TotalSizeDisplay => TotalSizeBytes switch
    {
        >= 1_073_741_824 => $"{TotalSizeBytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{TotalSizeBytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{TotalSizeBytes / 1024.0:F2} KB",
        _ => $"{TotalSizeBytes} B"
    };
}
