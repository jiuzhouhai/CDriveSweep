using System.IO;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 文件系统工具方法
/// </summary>
internal static class FileSystemHelper
{
    /// <summary>
    /// 安全获取目录大小，跳过符号链接和 junction，防止重复计算
    /// </summary>
    public static long GetDirectorySize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*",
                new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                try { size += new FileInfo(file).Length; }
                catch { }
            }

            foreach (var subDir in Directory.EnumerateDirectories(path, "*",
                new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                size += GetDirectorySize(subDir);
            }
        }
        catch { }
        return size;
    }

    /// <summary>
    /// 检查路径是否为 ReparsePoint（Junction、符号链接等）
    /// </summary>
    public static bool IsReparsePoint(string path)
    {
        try
        {
            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch { return false; }
    }
}
