using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理 Windows 错误报告文件 (WER)
/// </summary>
public class ErrorReportCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_ErrorReport");
    public string Description => Loc.Get("Cleaner_ErrorReport_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string WerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Microsoft", "Windows", "WER");

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        if (!Directory.Exists(WerPath))
            return Task.FromResult(EmptyResult());

        try
        {
            ScanWerDir(WerPath, items, ref total, ct);
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

    public async Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        await Task.Yield();
        int cleaned = 0, failed = 0;
        long freed = 0;
        var errors = new List<string>();

        if (!Directory.Exists(WerPath))
            return new CleanResult { Category = Category, Success = true };

        try
        {
            CleanWerDir(WerPath, ref cleaned, ref failed, ref freed, errors, ct);
        }
        catch (Exception ex) { errors.Add(ex.Message); }

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

    private static void ScanWerDir(string dir, List<ScanItem> items, ref long total, CancellationToken ct)
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
                    {
                        total += fi.Length;
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = "错误报告文件",
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
                ScanWerDir(sub, items, ref total, ct);
            }
        }
        catch { }
    }

    private static void CleanWerDir(string dir, ref int cleaned, ref int failed,
        ref long freed, List<string> errors, CancellationToken ct)
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
                    freed += fi.Length;
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    cleaned++;
                }
                catch (Exception ex) { failed++; errors.Add($"{file}: {ex.Message}"); }
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                ct.ThrowIfCancellationRequested();
                CleanWerDir(sub, ref cleaned, ref failed, ref freed, errors, ct);
                // 尝试删除已清空的子目录
                try { if (!Directory.EnumerateFileSystemEntries(sub).Any()) Directory.Delete(sub); }
                catch { }
            }
        }
        catch { }
    }

    private ScanResult EmptyResult() => new()
    {
        Category = Category,
        Description = Description,
        Items = new List<ScanItem>(),
        TotalSizeBytes = 0
    };
}
