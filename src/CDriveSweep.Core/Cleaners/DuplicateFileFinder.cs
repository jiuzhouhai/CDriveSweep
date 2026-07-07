using System.Security.Cryptography;
using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 查找 C 盘重复文件（仅展示，不自动删除）
/// </summary>
public class DuplicateFileFinder : ICleaner
{
    public string Category => Loc.Get("Cleaner_Duplicates");
    public string Description => Loc.Get("Cleaner_Duplicates_Desc");
    public CleanerRisk Risk => CleanerRisk.Review;

    private const long MinFileSize = 1024; // 跳过 <1KB 的文件
    private const int MaxResults = 50;
    private static readonly HashSet<string> SkipExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe", ".sys", ".log", ".pdb", ".tmp", ".msi"
    };

    public async Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var groups = new Dictionary<long, List<FileInfo>>();
        string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
        var usersPath = Path.Combine(root, "Users", Environment.UserName);

        if (Directory.Exists(usersPath))
        {
            await Task.Run(() =>
            {
                try { GroupBySize(usersPath, groups, ct); }
                catch { }
            }, ct);
        }

        // 找出大小相同且内容相同的文件组
        var items = new List<ScanItem>();
        foreach (var kvp in groups.Where(g => g.Value.Count >= 2))
        {
            if (ct.IsCancellationRequested || items.Count >= MaxResults) break;

            var fileGroup = kvp.Value;
            // 按 Hash 分组
            var hashGroups = GroupByHash(fileGroup, ct);
            foreach (var hkvp in hashGroups.Where(g => g.Value.Count >= 2))
            {
                var dupGroup = hkvp.Value;
                long size = dupGroup[0].Length;
                long wasted = size * (dupGroup.Count - 1);
                // 只取每组第一个作为代表显示
                items.Add(new ScanItem
                {
                    Name = $"{Path.GetFileName(dupGroup[0].FullName)} (×{dupGroup.Count})",
                    Description = $"重复文件，共 {dupGroup.Count} 个副本",
                    Path = dupGroup[0].FullName,
                    SizeBytes = wasted,
                    IsDirectory = false
                });
            }
        }

        return new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = items,
            TotalSizeBytes = items.Sum(i => i.SizeBytes)
        };
    }

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = true,
            FreedBytes = 0,
            ItemsCleaned = 0,
            ItemsFailed = 0
        });
    }

    private static void GroupBySize(string dir, Dictionary<long, List<FileInfo>> groups, CancellationToken ct)
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
                    if (fi.Length < MinFileSize) continue;
                    if (SkipExts.Contains(fi.Extension)) continue;

                    if (!groups.TryGetValue(fi.Length, out var list))
                        groups[fi.Length] = list = new List<FileInfo>();
                    list.Add(fi);
                }
                catch { }
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                if (FileSystemHelper.IsReparsePoint(sub)) continue;
                try { GroupBySize(sub, groups, ct); }
                catch { }
            }
        }
        catch { }
    }

    private static Dictionary<string, List<FileInfo>> GroupByHash(List<FileInfo> files, CancellationToken ct)
    {
        var result = new Dictionary<string, List<FileInfo>>();
        foreach (var file in files.Take(200)) // 限制每组最多比较 200 个文件
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                using var fs = file.OpenRead();
                using var md5 = MD5.Create();
                var hash = BitConverter.ToString(md5.ComputeHash(fs));

                if (!result.TryGetValue(hash, out var list))
                    result[hash] = list = new List<FileInfo>();
                list.Add(file);
            }
            catch { }
        }
        return result;
    }
}
