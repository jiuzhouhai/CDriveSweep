using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 扫描 C 盘中所有空目录（仅扫描展示，不自动删除）
/// </summary>
public class EmptyFolderCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_EmptyFolders");
    public string Description => Loc.Get("Cleaner_EmptyFolders_Desc");
    public CleanerRisk Risk => CleanerRisk.Review;

    private static readonly string[] SkipFolders = { "System Volume Information", "$Recycle.Bin", "Recovery" };

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";

        try
        {
            foreach (var topDir in Directory.EnumerateDirectories(root))
            {
                ct.ThrowIfCancellationRequested();
                var dirName = Path.GetFileName(topDir);
                if (SkipFolders.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase))) continue;

                FindEmptyDirs(topDir, items, ct);
            }
        }
        catch { }

        return Task.FromResult(new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = items.Take(200).ToList(),
            TotalSizeBytes = 0
        });
    }

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        // 空文件夹不自动删除，需要用户手动处理
        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = true,
            FreedBytes = 0,
            ItemsCleaned = 0,
            ItemsFailed = 0
        });
    }

    private static void FindEmptyDirs(string dir, List<ScanItem> items, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || items.Count >= 200) return;
        try
        {
            bool hasEntries = false;
            foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                hasEntries = true;
                try
                {
                    var attr = File.GetAttributes(entry);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        FindEmptyDirs(entry, items, ct);
                }
                catch { }
            }

            if (!hasEntries)
            {
                items.Add(new ScanItem
                {
                    Name = dir,
                    Description = "空目录",
                    Path = dir,
                    SizeBytes = 0,
                    IsDirectory = true
                });
            }
        }
        catch { }
    }
}
