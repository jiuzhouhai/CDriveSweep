using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 扫描微信和企业微信缓存文件
/// </summary>
public class WeChatCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_WeChat");
    public string Description => Loc.Get("Cleaner_WeChat_Desc");
    public CleanerRisk Risk => CleanerRisk.Medium;

    private static List<(string Name, string Path)> GetCachePaths()
    {
        var paths = new List<(string, string)>();
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // 微信：Documents\WeChat Files\<wxid>\FileStorage\...
        var wechatData = Path.Combine(documents, "WeChat Files");
        if (Directory.Exists(wechatData))
        {
            foreach (var userId in Directory.EnumerateDirectories(wechatData))
            {
                var fileStorage = Path.Combine(userId, "FileStorage");
                if (Directory.Exists(fileStorage))
                {
                    foreach (var sub in Directory.EnumerateDirectories(fileStorage))
                        paths.Add((Path.GetFileName(sub), sub));
                }

                // 微信消息附件（通常是最大的缓存）
                var msgAttach = Path.Combine(userId, "MsgAttach");
                if (Directory.Exists(msgAttach))
                    paths.Add(($"{Path.GetFileName(userId)} 消息附件", msgAttach));

                // Msg 下也有 FileStorage
                var msgFs = Path.Combine(userId, "Msg", "FileStorage");
                if (Directory.Exists(msgFs))
                    paths.Add(($"{Path.GetFileName(userId)} Msg-FileStorage", msgFs));
            }
        }

        // 企业微信：Documents\WXWork\<corpid>\Cache
        var wxWorkData = Path.Combine(documents, "WXWork");
        if (Directory.Exists(wxWorkData))
        {
            foreach (var dir in Directory.EnumerateDirectories(wxWorkData))
            {
                var cachePath = Path.Combine(dir, "Cache");
                if (Directory.Exists(cachePath))
                    paths.Add(($"企业微信 {Path.GetFileName(dir)} 缓存", cachePath));

                var dataPath = Path.Combine(dir, "Data");
                if (Directory.Exists(dataPath))
                    paths.Add(($"企业微信 {Path.GetFileName(dir)} 数据", dataPath));

                var imagePath = Path.Combine(dir, "Image");
                if (Directory.Exists(imagePath))
                    paths.Add(($"企业微信 {Path.GetFileName(dir)} 图片", imagePath));
            }
        }

        return paths;
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
                        Description = "微信/企业微信缓存",
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
