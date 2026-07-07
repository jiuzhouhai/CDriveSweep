using CDriveSweep.Core.Cleaners;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core;

/// <summary>
/// 清理引擎 - 协调所有清理器的扫描和清理
/// </summary>
public class CleanerEngine
{
    private readonly List<ICleaner> _cleaners;

    public CleanerEngine()
    {
        _cleaners = new List<ICleaner>
        {
            // 基础清理（低风险）
            new TempFileCleaner(),
            new RecycleBinCleaner(),
            new BrowserCacheCleaner(),
            new WindowsUpdateCleaner(),
            new PrefetchCleaner(),
            new ThumbnailCacheCleaner(),
            new LogFileCleaner(),
            new DnsCacheCleaner(),

            // 深度清理 - 低风险
            new MemoryDumpCleaner(),
            new DeliveryOptimizationCleaner(),
            new ErrorReportCleaner(),
            new WindowsOldCleaner(),

            // 深度清理 - 中风险（默认不勾选）
            new WeChatCacheCleaner(),
            new QQCacheCleaner(),
            new NuGetCacheCleaner(),

            // 深度清理 - 需用户自行判断（仅扫描展示）
            new LargeFileFinder(),
            new DuplicateFileFinder(),
            new EmptyFolderCleaner(),
        };
    }

    /// <summary>注册自定义清理器</summary>
    public void RegisterCleaner(ICleaner cleaner) => _cleaners.Add(cleaner);

    /// <summary>获取所有已注册的清理器</summary>
    public IReadOnlyList<ICleaner> GetCleaners() => _cleaners.AsReadOnly();

    /// <summary>扫描所有清理器</summary>
    public async Task<List<ScanResult>> ScanAllAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<ScanResult>();

        foreach (var cleaner in _cleaners)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"正在扫描: {cleaner.Category}...");

            try
            {
                var result = await cleaner.ScanAsync(ct);
                results.Add(result);
                progress?.Report($"  {cleaner.Category}: {result.TotalSizeDisplay} 可清理");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                progress?.Report($"  {cleaner.Category}: 扫描失败 - {ex.Message}");
                results.Add(new ScanResult
                {
                    Category = cleaner.Category,
                    Description = cleaner.Description,
                    Items = new List<ScanItem>(),
                    TotalSizeBytes = 0
                });
            }
        }

        return results;
    }

    /// <summary>扫描指定清理器</summary>
    public async Task<ScanResult> ScanAsync(ICleaner cleaner, CancellationToken ct = default)
    {
        return await cleaner.ScanAsync(ct);
    }

    /// <summary>清理指定分类</summary>
    public async Task<CleanResult> CleanAsync(ICleaner cleaner, CancellationToken ct = default)
    {
        return await cleaner.CleanAsync(ct);
    }

    /// <summary>清理所有</summary>
    public async Task<List<CleanResult>> CleanAllAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<CleanResult>();
        long totalFreed = 0;
        int totalCleaned = 0;

        foreach (var cleaner in _cleaners)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"正在清理: {cleaner.Category}...");

            try
            {
                var result = await cleaner.CleanAsync(ct);
                results.Add(result);
                totalFreed += result.FreedBytes;
                totalCleaned += result.ItemsCleaned;
                progress?.Report($"  {cleaner.Category}: 已释放 {result.FreedSizeDisplay}");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                progress?.Report($"  {cleaner.Category}: 清理失败 - {ex.Message}");
                results.Add(new CleanResult
                {
                    Category = cleaner.Category,
                    Success = false,
                    FreedBytes = 0,
                    ItemsCleaned = 0,
                    ItemsFailed = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        progress?.Report($"\n总计: 清理 {totalCleaned} 项, 释放 {FormatSize(totalFreed)}");

        return results;
    }

    /// <summary>格式化大小显示</summary>
    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{bytes / 1024.0:F2} KB",
        _ => $"{bytes} B"
    };
}
