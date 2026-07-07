using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理系统内存转储文件 (MEMORY.DMP)
/// </summary>
public class MemoryDumpCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_MemoryDump");
    public string Description => Loc.Get("Cleaner_MemoryDump_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    private static readonly string MemoryDumpPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP");

    private static readonly string MinidumpPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var items = new List<ScanItem>();
        long total = 0;

        // 主内存转储文件
        if (File.Exists(MemoryDumpPath))
        {
            try
            {
                var fi = new FileInfo(MemoryDumpPath);
                if (fi.Length > 0)
                {
                    total += fi.Length;
                    items.Add(new ScanItem
                    {
                        Name = "MEMORY.DMP",
                        Description = "系统蓝屏内存转储（主文件）",
                        Path = MemoryDumpPath,
                        SizeBytes = fi.Length,
                        IsDirectory = false
                    });
                }
            }
            catch { }
        }

        // Minidump 小转储
        if (Directory.Exists(MinidumpPath))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(MinidumpPath, "*.dmp"))
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
                                Description = "小内存转储文件",
                                Path = file,
                                SizeBytes = fi.Length,
                                IsDirectory = false
                            });
                        }
                    }
                    catch { }
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

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        int cleaned = 0, failed = 0;
        long freed = 0;
        var errors = new List<string>();

        if (File.Exists(MemoryDumpPath))
        {
            try
            {
                var fi = new FileInfo(MemoryDumpPath);
                freed += fi.Length;
                File.Delete(MemoryDumpPath);
                cleaned++;
            }
            catch (Exception ex) { failed++; errors.Add($"MEMORY.DMP: {ex.Message}"); }
        }

        if (Directory.Exists(MinidumpPath))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(MinidumpPath, "*.dmp"))
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var fi = new FileInfo(file);
                        freed += fi.Length;
                        File.Delete(file);
                        cleaned++;
                    }
                    catch (Exception ex) { failed++; errors.Add($"{file}: {ex.Message}"); }
                }
            }
            catch (Exception ex) { failed++; errors.Add(ex.Message); }
        }

        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = failed == 0,
            FreedBytes = freed,
            ItemsCleaned = cleaned,
            ItemsFailed = failed,
            Errors = errors
        });
    }
}
