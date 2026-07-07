using System.Diagnostics;
using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Core.Cleaners;

/// <summary>
/// 刷新 DNS 缓存
/// </summary>
public class DnsCacheCleaner : ICleaner
{
    public string Category => Loc.Get("Cleaner_Dns");
    public string Description => Loc.Get("Cleaner_Dns_Desc");
    public CleanerRisk Risk => CleanerRisk.Low;

    public Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        // DNS 缓存大小无法直接获取，提供一个固定提示
        return Task.FromResult(new ScanResult
        {
            Category = Category,
            Description = Description,
            Items = new List<ScanItem>
            {
                new()
                {
                    Name = "DNS 解析缓存",
                    Description = "ipconfig /flushdns",
                    Path = "系统命令",
                    SizeBytes = 0,
                    IsDirectory = false
                }
            },
            TotalSizeBytes = 0
        });
    }

    public Task<CleanResult> CleanAsync(CancellationToken ct = default)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(10000);
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                return Task.FromResult(new CleanResult
                {
                    Category = Category,
                    Success = true,
                    FreedBytes = 0,
                    ItemsCleaned = 1,
                    ItemsFailed = 0
                });
            }
            else
            {
                return Task.FromResult(new CleanResult
                {
                    Category = Category,
                    Success = false,
                    FreedBytes = 0,
                    ItemsCleaned = 0,
                    ItemsFailed = 1,
                    Errors = new List<string> { error.Trim() }
                });
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CleanResult
            {
                Category = Category,
                Success = false,
                FreedBytes = 0,
                ItemsCleaned = 0,
                ItemsFailed = 1,
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
