using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理缩略图缓存
/// </summary>
public class ThumbnailCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_Thumbnail");
    public string Description => Loc.Get("Cleaner_Thumbnail_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string ExplorerCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Windows", "Explorer");

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        if (!Directory.Exists(ExplorerCachePath))
            return Task.FromResult(new ScanResult
            {
                Category = Category,
                Description = Description,
                Items = items,
                TotalSizeBytes = 0
            });

        try
        {
            foreach (var file in Directory.EnumerateFiles(ExplorerCachePath, "thumbcache_*.db"))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Length > 0)
                    {
                        total += fi.Length;
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = "缩略图缓存",
                            Path = file,
                            SizeBytes = fi.Length,
                            IsDirectory = false
                        });
                    }
                }
                catch { }
            }

            // 也扫描 iconcache 文件
            foreach (var file in Directory.EnumerateFiles(ExplorerCachePath, "iconcache_*.db"))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Length > 0)
                    {
                        total += fi.Length;
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = "图标缓存",
                            Path = file,
                            SizeBytes = fi.Length,
                            IsDirectory = false
                        });
                    }
                }
                catch { }
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

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(ExplorerCachePath))
            return Task.FromResult(new CleanResult
            {
                Category = Category,
                Success = true,
                FreedBytes = 0,
                ItemsCleaned = 0,
                ItemsFailed = 0
            });

        int cleaned = 0, failed = 0;
        long freedBytes = 0;
        var errors = new List<string>();
        var patterns = new[] { "thumbcache_*.db", "iconcache_*.db" };

        try
        {
            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.EnumerateFiles(ExplorerCachePath, pattern))
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var fi = new FileInfo(file);
                        freedBytes += fi.Length;
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        cleaned++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        errors.Add($"{file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
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
}
