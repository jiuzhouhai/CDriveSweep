using System.Runtime.InteropServices;
using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 清空回收站
/// </summary>
public class RecycleBinCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_RecycleBin");
    public string Description => Loc.Get("Cleaner_RecycleBin_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        // 回收站无法精确扫描大小，提供估算
        var items = new List<ScanItem>();
        long total = 0;

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            var recyclePath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
            if (!Directory.Exists(recyclePath)) continue;

            try
            {
                foreach (var userDir in Directory.EnumerateDirectories(recyclePath))
                {
                    try
                    {
                        long size = GetDirectorySize(userDir);
                        if (size > 0)
                        {
                            total += size;
                            items.Add(new ScanItem
                            {
                                Name = $"回收站 ({drive.Name})",
                                Description = $"驱动器 {drive.Name} 的回收站内容",
                                Path = recyclePath,
                                SizeBytes = size,
                                IsDirectory = true
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
        uint result = SHEmptyRecycleBin(IntPtr.Zero, null,
            SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);

        return Task.FromResult(new CleanResult
        {
            Category = Category,
            Success = result == 0,
            FreedBytes = 0, // SHEmptyRecycleBin 不返回大小
            ItemsCleaned = result == 0 ? 1 : 0,
            ItemsFailed = result == 0 ? 0 : 1,
            Errors = result != 0
                ? new List<string> { $"清空回收站失败，错误码: {result}" }
                : new List<string>()
        });
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
