using System.Runtime.InteropServices;
using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理 %TEMP% 和 C:\Windows\Temp
/// </summary>
public class TempFileCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_SystemTemp");
    public string Description => Loc.Get("Cleaner_SystemTemp_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        var tempPaths = new[]
        {
            Path.GetTempPath().TrimEnd('\\'),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };

        foreach (var dir in tempPaths)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                ScanDirectory(dir, items, ct);
            }
            catch { /* 跳过无法访问的目录 */ }
        }

        var totalSize = items.Sum(i => i.SizeBytes);
        return Task.FromResult(new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = items,
            TotalSizeBytes = totalSize
        });
    }

    public async Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        int cleaned = 0, failed = 0;
        long freedBytes = 0;
        var errors = new List<string>();

        var tempPaths = new[]
        {
            Path.GetTempPath().TrimEnd('\\'),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };

        foreach (var dir in tempPaths)
        {
            if (!Directory.Exists(dir)) continue;
            (int c, int f, long bytes, var errs) = CleanDirectory(dir, ct);
            cleaned += c;
            failed += f;
            freedBytes += bytes;
            errors.AddRange(errs);
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

    private static void ScanDirectory(string dir, List<ScanItem> items, CancellationToken ct)
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
                    if (fi.Length > 0)
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = "临时文件",
                            Path = file,
                            SizeBytes = fi.Length,
                            IsDirectory = false
                        });
                }
                catch { /* 跳过锁定的文件 */ }
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                ct.ThrowIfCancellationRequested();
                ScanDirectory(subDir, items, ct);
            }
        }
        catch { /* 跳过无法访问的目录 */ }
    }

    private static (int cleaned, int failed, long freedBytes, List<string> errors)
        CleanDirectory(string dir, CancellationToken ct)
    {
        int cleaned = 0, failed = 0;
        long freedBytes = 0;
        var errors = new List<string>();

        foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var attr = File.GetAttributes(entry);
                var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;

                if (isDir)
                {
                    long dirSize = GetDirectorySize(entry);
                    try
                    {
                        Directory.Delete(entry, true);
                        cleaned++;
                        freedBytes += dirSize;
                    }
                    catch
                    {
                        // 目录删除失败，递归逐个清理
                        var (c, f, b, _) = CleanDirectory(entry, ct);
                        cleaned += c;
                        failed += f;
                        freedBytes += b;
                    }
                }
                else
                {
                    long fileSize = new FileInfo(entry).Length;
                    try
                    {
                        File.SetAttributes(entry, FileAttributes.Normal);
                        File.Delete(entry);
                        cleaned++;
                        freedBytes += fileSize;
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
                errors.Add($"{entry}: {ex.Message}");
            }
        }

        return (cleaned, failed, freedBytes, errors);
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
