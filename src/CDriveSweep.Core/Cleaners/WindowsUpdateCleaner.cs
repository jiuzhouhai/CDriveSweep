using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理 Windows Update 下载缓存
/// </summary>
public class WindowsUpdateCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_WinUpdate");
    public string Description => Loc.Get("Cleaner_WinUpdate_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string UpdateCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "SoftwareDistribution", "Download");

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();

        if (!Directory.Exists(UpdateCachePath))
            return Task.FromResult(new ScanResult
            {
                Category = Category,
                Description = Description,
                Items = items,
                TotalSizeBytes = 0
            });

        try
        {
            long total = 0;
            foreach (var entry in Directory.EnumerateFileSystemEntries(UpdateCachePath))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var attr = File.GetAttributes(entry);
                    var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                    long size = isDir ? GetDirectorySize(entry) : new FileInfo(entry).Length;

                    if (size > 0)
                    {
                        total += size;
                        items.Add(new ScanItem
                        {
                            Name = Path.GetFileName(entry),
                            Description = "Windows Update 下载文件",
                            Path = entry,
                            SizeBytes = size,
                            IsDirectory = isDir
                        });
                    }
                }
                catch { }
            }

            return Task.FromResult(new ScanResult
            {
                Category = Category,
                Description = Description,
                Items = items,
                TotalSizeBytes = total
            });
        }
        catch
        {
            return Task.FromResult(new ScanResult
            {
                Category = Category,
                Description = Description,
                Items = items,
                TotalSizeBytes = 0
            });
        }
    }

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(UpdateCachePath))
            return Task.FromResult(new CleanResult
            {
                Category = Category,
                Success = true,
                FreedBytes = 0,
                ItemsCleaned = 0,
                ItemsFailed = 0
            });

        // 需要管理员权限，使用 rundll32 调用 Windows Update API
        // 作为后备方案，直接清理下载文件夹
        int cleaned = 0, failed = 0;
        long freedBytes = 0;
        var errors = new List<string>();

        try
        {
            freedBytes = GetDirectorySize(UpdateCachePath);

            foreach (var entry in Directory.EnumerateFileSystemEntries(UpdateCachePath))
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    var attr = File.GetAttributes(entry);
                    var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;

                    if (isDir)
                        Directory.Delete(entry, true);
                    else
                    {
                        File.SetAttributes(entry, FileAttributes.Normal);
                        File.Delete(entry);
                    }
                    cleaned++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{entry}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            failed++;
            errors.Add(ex.Message);
        }

        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = failed == 0,
            FreedBytes = freedBytes,
            ItemsCleaned = cleaned,
            ItemsFailed = failed,
            Errors = errors
        });
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
