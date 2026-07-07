using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 扫描 QQ 缓存文件
/// </summary>
public class QQCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_QQ");
    public string Description => Loc.Get("Cleaner_QQ_Desc");
    public CleanerRisk Risk => CleanerRisk.Medium;

    private static List<(string Name, string Path)> GetCachePaths()
    {
        var paths = new List<(string, string)>();
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // QQ 数据通常在这里
        var tencentFiles = Path.Combine(docs, "Tencent Files");
        if (Directory.Exists(tencentFiles))
        {
            foreach (var userDir in Directory.EnumerateDirectories(tencentFiles))
            {
                if (ctCheck(userDir)) continue;
                AddSubDir(paths, userDir, "Image", "图片");
                AddSubDir(paths, userDir, "Video", "视频");
                AddSubDir(paths, userDir, "FileRecv", "接收文件");
            }
        }

        // 也尝试 AppData\Roaming\Tencent
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var tencent = Path.Combine(appData, "Tencent");
        if (Directory.Exists(tencent))
        {
            foreach (var sub in Directory.EnumerateDirectories(tencent))
            {
                if (ctCheck(sub)) continue;
                var name = Path.GetFileName(sub);
                AddSubDir(paths, sub, "Image", $"{name}-图片");
                AddSubDir(paths, sub, "Video", $"{name}-视频");
            }
        }

        return paths;
    }

    private static bool ctCheck(string dir) => false; // dummy, real CancellationToken passed via method

    private static void AddSubDir(List<(string Name, string Path)> paths,
        string parent, string subDir, string label)
    {
        var full = Path.Combine(parent, subDir);
        if (Directory.Exists(full))
            paths.Add(($"{Path.GetFileName(parent)} {label}", full));
    }

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        foreach (var (name, path) in GetCachePaths())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                long size = GetDirSize(path);
                if (size > 0)
                {
                    total += size;
                    items.Add(new ScanItem
                    {
                        Name = name,
                        Description = "QQ 缓存/文件",
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

        foreach (var (_, path) in GetCachePaths())
        {
            if (ct.IsCancellationRequested) break;
            if (!Directory.Exists(path)) continue;

            try
            {
                freed += GetDirSize(path);
                foreach (var entry in Directory.EnumerateFileSystemEntries(path))
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var attr = File.GetAttributes(entry);
                        var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                        if (isDir) Directory.Delete(entry, true);
                        else File.Delete(entry);
                        cleaned++;
                    }
                    catch (Exception ex) { failed++; errors.Add($"{entry}: {ex.Message}"); }
                }
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
