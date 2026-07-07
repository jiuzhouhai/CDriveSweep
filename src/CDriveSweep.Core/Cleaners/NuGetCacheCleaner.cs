using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 扫描 NuGet 包缓存（.NET 开发者）
/// </summary>
public class NuGetCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_NuGet");
    public string Description => Loc.Get("Cleaner_NuGet_Desc");
    public CleanerRisk Risk => CleanerRisk.Medium;

    private static string[] GetPossiblePaths()
    {
        var list = new List<string>();

        // 默认 .nuget\packages
        var defaultPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
        if (Directory.Exists(defaultPackages))
            list.Add(defaultPackages);

        // NUGET_PACKAGES 环境变量
        var nugetEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(nugetEnv) && Directory.Exists(nugetEnv))
            list.Add(nugetEnv);

        // NuGet HTTP 缓存
        var httpCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NuGet", "v3-cache");
        if (Directory.Exists(httpCache))
            list.Add(httpCache);

        // NuGet 插件缓存
        var pluginCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NuGet", "plugins-cache");
        if (Directory.Exists(pluginCache))
            list.Add(pluginCache);

        return list.Distinct().ToArray();
    }

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        foreach (var path in GetPossiblePaths())
        {
            if (!Directory.Exists(path)) continue;
            ct.ThrowIfCancellationRequested();
            try
            {
                long size = GetDirSize(path);
                if (size > 0)
                {
                    total += size;
                    items.Add(new ScanItem
                    {
                        Name = Path.GetFileName(path),
                        Description = "NuGet 包缓存",
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
        long freed = 0;
        var errors = new List<string>();

        foreach (var path in GetPossiblePaths())
        {
            if (ct.IsCancellationRequested) break;
            if (!Directory.Exists(path)) continue;

            try
            {
                freed += GetDirSize(path);
                Directory.Delete(path, true);
                cleaned++;
            }
            catch (Exception ex) { failed++; errors.Add($"{path}: {ex.Message}"); }
        }

        return new CleanResult
        {
            Category = Category,
            Success = failed == 0,
            FreedBytes = freed,
            ItemsCleaned = cleaned,
            ItemsFailed = failed,
            Errors = errors
        };
    }

    private static long GetDirSize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*",
                new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                try { size += new FileInfo(file).Length; }
                catch { }
            }

            foreach (var sub in Directory.EnumerateDirectories(path, "*",
                new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                size += GetDirSize(sub);
            }
        }
        catch { }
        return size;
    }
}
