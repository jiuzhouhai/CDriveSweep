using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理主流浏览器缓存 (Chrome / Edge / Firefox)
/// </summary>
public class BrowserCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_BrowserCache");
    public string Description => Loc.Get("Cleaner_BrowserCache_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly List<(string Name, string Path)> BrowserCaches = new();

    static BrowserCacheCleaner()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Chrome Cache
        AddIfExists("Chrome 缓存",
            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data"));

        // Edge Cache
        AddIfExists("Edge 缓存",
            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data"));

        // Firefox Cache
        // Firefox 使用 profiles，可能需要查找
        var firefoxProfiles = Path.Combine(roamingAppData, "Mozilla", "Firefox", "Profiles");
        if (Directory.Exists(firefoxProfiles))
        {
            try
            {
                foreach (var profile in Directory.EnumerateDirectories(firefoxProfiles))
                {
                    var cachePath = Path.Combine(profile, "cache2");
                    if (Directory.Exists(cachePath))
                        BrowserCaches.Add((Path.GetFileName(profile), cachePath));
                }
            }
            catch { }
        }
    }

    private static void AddIfExists(string name, string path)
    {
        if (Directory.Exists(path))
            BrowserCaches.Add((name, path));
    }

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        foreach (var (name, path) in BrowserCaches)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(path)) continue;

            try
            {
                long size = GetDirectorySize(path);
                if (size > 0)
                {
                    total += size;
                    items.Add(new ScanItem
                    {
                        Name = name,
                        Description = name,
                        Path = path,
                        SizeBytes = size,
                        IsDirectory = true
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

    public async Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        int cleaned = 0, failed = 0;
        long freedBytes = 0;
        var errors = new List<string>();

        foreach (var (name, path) in BrowserCaches)
        {
            if (ct.IsCancellationRequested) break;
            if (!Directory.Exists(path)) continue;

            try
            {
                long size = GetDirectorySize(path);

                foreach (var entry in Directory.EnumerateFileSystemEntries(path))
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
                freedBytes += size;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{path}: {ex.Message}");
            }
        }

        return new CleanResult
        {
            Category = Category,
            Success = failed == 0,
            FreedBytes = freedBytes,
            ItemsCleaned = cleaned,
            ItemsFailed = failed,
            Errors = errors
        };
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
