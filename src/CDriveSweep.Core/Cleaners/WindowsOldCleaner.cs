using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理 Windows.old（大版本升级后的旧系统备份）
/// </summary>
public class WindowsOldCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_WinOld");
    public string Description => Loc.Get("Cleaner_WinOld_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string WindowsOldPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows).Replace("\\Windows", ""),
        "Windows.old");

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        if (!Directory.Exists(WindowsOldPath))
            return Task.FromResult(new ScanResult
            {
                Category = Category, Description = Description,
                Items = items, TotalSizeBytes = 0
            });

        try
        {
            total = GetDirectorySize(WindowsOldPath);
            if (total > 0)
            {
                items.Add(new ScanItem
                {
                    Name = "Windows.old",
                    Description = "系统升级后的旧版 Windows 备份",
                    Path = WindowsOldPath,
                    SizeBytes = total,
                    IsDirectory = true
                });
            }
        }
        catch { }

        return Task.FromResult(new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = items,
            TotalSizeBytes = total
        });
    }

    public async Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        if (!Directory.Exists(WindowsOldPath))
            return new CleanResult { Category = Category, Success = true };

        try
        {
            long freed = GetDirectorySize(WindowsOldPath);
            Directory.Delete(WindowsOldPath, true);
            return new CleanResult
            {
                Category = Category,
                Success = true,
                FreedBytes = freed,
                ItemsCleaned = 1,
                ItemsFailed = 0
            };
        }
        catch (Exception ex)
        {
            return new CleanResult
            {
                Category = Category,
                Success = false,
                FreedBytes = 0,
                ItemsCleaned = 0,
                ItemsFailed = 1,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { size += new FileInfo(file).Length; }
                catch { }
            }
        }
        catch { }
        return size;
    }
}
