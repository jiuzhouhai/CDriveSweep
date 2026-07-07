using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理器统一接口
/// </summary>
public interface ICleaner
{
    /// <summary>分类名称，如"系统临时文件"</summary>
    string Category { get; }

    /// <summary>分类描述</summary>
    string Description { get; }

    /// <summary>风险等级</summary>
    CleanerRisk Risk { get; }

    /// <summary>扫描可清理的内容，不执行删除</summary>
    Task<ScanResult> ScanAsync(CancellationToken ct = default);

    /// <summary>执行清理</summary>
    Task<CleanResult> CleanAsync(CancellationToken ct = default);
}
