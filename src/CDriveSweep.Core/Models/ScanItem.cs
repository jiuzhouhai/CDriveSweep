namespace CDriveSweep.Core.Models;

/// <summary>
/// 单个待清理项的扫描结果
/// </summary>
public class ScanItem
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public bool IsDirectory { get; init; }

    public string SizeDisplay => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{SizeBytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{SizeBytes / 1024.0:F2} KB",
        _ => $"{SizeBytes} B"
    };
}
