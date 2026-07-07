namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清理器风险等级
/// </summary>
public enum CleanerRisk
{
    /// <summary>低风险，安全清理，默认选中</summary>
    Low,

    /// <summary>中等风险，建议确认后清理，默认不选中</summary>
    Medium,

    /// <summary>需要用户自行判断，仅扫描展示，不自动清理</summary>
    Review
}
