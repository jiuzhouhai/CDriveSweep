using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 扫描 C 盘大文件（仅展示，不自动删除）
/// </summary>
public class LargeFileFinder : ICleaner
{
    public string Category => Loc.Get("Cleaner_LargeFiles");
    public string Description => Loc.Get("Cleaner_LargeFiles_Desc");
    public CleanerRisk Risk => CleanerRisk.Review;

    private const long MinSizeBytes = 100 * 1024 * 1024; // 100MB
    private const int MaxResults = 100;

    private static readonly string[] SkipFolders =
    {
        "System Volume Information", "$Recycle.Bin", "Recovery", "Windows",
        "Program Files", "Program Files (x86)", "ProgramData"
    };

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new SortedSet<ScanItem>(Comparer<ScanItem>.Create((a, b) => b.SizeBytes.CompareTo(a.SizeBytes)));
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";

        // 只扫描 Users 目录，跳过系统目录
        var usersPath = Path.Combine(root, "Users");
        if (Directory.Exists(usersPath))
        {
            try
            {
                string userName = Environment.UserName;
                var userPath = Path.Combine(usersPath, userName);
                if (Directory.Exists(userPath))
                    FindLargeFiles(userPath, items, ct);
            }
            catch { }
        }

        return Task.FromResult(new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = items.Take(MaxResults).ToList(),
            TotalSizeBytes = items.Sum(i => i.SizeBytes)
        });
    }

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        // 大文件不由程序自动删除
        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = true,
            FreedBytes = 0,
            ItemsCleaned = 0,
            ItemsFailed = 0
        });
    }

    private static void FindLargeFiles(string dir, SortedSet<ScanItem> items, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Length >= MinSizeBytes)
                    {
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = $"大文件 ({fi.Extension})",
                            Path = file,
                            SizeBytes = fi.Length,
                            IsDirectory = false
                        });
                    }
                }
                catch { }
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                ct.ThrowIfCancellationRequested();
                // 跳过 Junction / 符号链接，避免跟踪到其他盘
                if (FileSystemHelper.IsReparsePoint(sub)) continue;
                try { FindLargeFiles(sub, items, ct); }
                catch { }
            }
        }
        catch { }
    }
}
