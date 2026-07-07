using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理 Windows 日志文件 (C:\Windows\Logs)
/// </summary>
public class LogFileCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_LogFiles");
    public string Description => Loc.Get("Cleaner_LogFiles_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string WindowsLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs");

    private static readonly HashSet<string> LogExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".log", ".etl", ".evtx.bak", ".txt", ".dmp"
    };

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        if (!Directory.Exists(WindowsLogPath))
            return Task.FromResult(new ScanResult
            {
                Category = Category,
                Description = Description,
                Items = items,
                TotalSizeBytes = 0
            });

        try
        {
            ScanLogDirectory(WindowsLogPath, items, ref total, ct);
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
        long freedBytes = 0;
        var errors = new List<string>();

        if (!Directory.Exists(WindowsLogPath))
            return new CleanResult
            {
                Category = Category,
                Success = true,
                FreedBytes = 0,
                ItemsCleaned = 0,
                ItemsFailed = 0
            };

        try
        {
            CleanLogDirectory(WindowsLogPath, ref cleaned, ref failed, ref freedBytes, errors, ct);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
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

    private void ScanLogDirectory(string dir, List<ScanItem> items, ref long total, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                ct.ThrowIfCancellationRequested();
                var ext = Path.GetExtension(file);
                if (!LogExtensions.Contains(ext)) continue;

                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Length > 0)
                    {
                        total += fi.Length;
                        items.Add(new ScanItem
                        {
                            Name = fi.Name,
                            Description = $"日志文件 ({ext})",
                            Path = file,
                            SizeBytes = fi.Length,
                            IsDirectory = false
                        });
                    }
                }
                catch { }
            }

            // 递归子目录，只进入一层
            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                ct.ThrowIfCancellationRequested();
                ScanLogDirectory(subDir, items, ref total, ct);
            }
        }
        catch { }
    }

    private void CleanLogDirectory(string dir, ref int cleaned, ref int failed,
        ref long freedBytes, List<string> errors, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                ct.ThrowIfCancellationRequested();
                var ext = Path.GetExtension(file);
                if (!LogExtensions.Contains(ext)) continue;

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

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                ct.ThrowIfCancellationRequested();
                CleanLogDirectory(subDir, ref cleaned, ref failed, ref freedBytes, errors, ct);
            }
        }
        catch { }
    }
}
