using System.Text;
using CDriveSweep.Core;
using CDriveSweep.Core.Localization;
using CDriveSweep.Core.Models;

namespace CDriveSweep.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 检查 --lang 参数
        if (args.Contains("--lang") || args.Contains("-lang"))
        {
            var langIndex = Array.IndexOf(args, "--lang");
            if (langIndex == -1) langIndex = Array.IndexOf(args, "-lang");
            if (langIndex >= 0 && langIndex + 1 < args.Length)
            {
                var lang = args[langIndex + 1].ToLowerInvariant();
                if (lang == "en" || lang == "en-us")
                    Loc.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(Loc.EnUS);
                else
                    Loc.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(Loc.ZhCN);
            }
        }

        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine($"║      {Loc.AppTitle}       ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();

        var engine = new CleanerEngine();
        var cleaners = engine.GetCleaners();

        if (args.Length == 0 || (args.Length == 2 && args[0].StartsWith("--lang")))
        {
            ShowHelp(cleaners);
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var autoYes = args.Contains("-y") || args.Contains("--yes");

        switch (command)
        {
            case "--list" or "-l":
                ListCategories(cleaners);
                break;

            case "--scan" or "-s":
                await ScanAllAsync(engine);
                break;

            case "--clean" or "-c":
                if (args.Length > 1 && !args[1].StartsWith('-'))
                    await CleanCategoryAsync(engine, cleaners, args[1], autoYes);
                else
                    await CleanAllAsync(engine, autoYes);
                break;

            case "--help" or "-h":
                ShowHelp(cleaners);
                break;

            default:
                Console.WriteLine(Loc.IsEnglish ? $"Unknown command: {command}" : $"未知参数: {command}");
                ShowHelp(cleaners);
                return 1;
        }

        return 0;
    }

    private static void ShowHelp(IReadOnlyList<Core.Cleaners.ICleaner> cleaners)
    {
        Console.WriteLine(Loc.IsEnglish ? "Usage:" : "用法:");
        Console.WriteLine("  CDriveSweep.Cli <command> [options]");
        Console.WriteLine();
        Console.WriteLine(Loc.IsEnglish ? "Commands:" : "命令:");
        Console.WriteLine("  -s, --scan           " + (Loc.IsEnglish ? "Scan all categories" : "扫描所有可清理项"));
        Console.WriteLine("  -c, --clean          " + (Loc.IsEnglish ? "Clean all categories" : "清理所有可清理项"));
        Console.WriteLine("  -c, --clean <name>   " + (Loc.IsEnglish ? "Clean specific category" : "清理指定分类"));
        Console.WriteLine("  -l, --list           " + (Loc.IsEnglish ? "List all categories" : "列出所有清理分类"));
        Console.WriteLine("  -h, --help           " + (Loc.IsEnglish ? "Show help" : "显示帮助"));
        Console.WriteLine();
        Console.WriteLine(Loc.IsEnglish ? "Options:" : "选项:");
        Console.WriteLine("  -y, --yes            " + (Loc.IsEnglish ? "Skip confirmation" : "跳过确认，直接执行清理"));
        Console.WriteLine("  --lang <zh|en>       " + (Loc.IsEnglish ? "Set language (zh/en)" : "设置语言 (zh/en)"));
        Console.WriteLine();
        Console.WriteLine(Loc.IsEnglish ? "Categories:" : "可用分类:");
        foreach (var c in cleaners)
            Console.WriteLine($"  · {c.Category,-20} {c.Description}");
    }

    private static void ListCategories(IReadOnlyList<Core.Cleaners.ICleaner> cleaners)
    {
        Console.WriteLine(Loc.IsEnglish ? "Cleanable categories:" : "可清理分类:");
        Console.WriteLine(new string('-', 60));
        foreach (var c in cleaners)
            Console.WriteLine($"  {c.Category,-24} | {c.Description}");
        Console.WriteLine();
    }

    private static async Task ScanAllAsync(CleanerEngine engine)
    {
        Console.WriteLine(Loc.IsEnglish ? "Scanning for cleanable items...\n" : "正在扫描可清理内容...\n");

        var progress = new Progress<string>(msg => Console.WriteLine(msg));
        var results = await engine.ScanAllAsync(progress);

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine(Loc.IsEnglish
            ? $"Scan complete, {results.Count} categories"
            : $"扫描完成，共 {results.Count} 个分类");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();

        long grandTotal = 0;
        foreach (var r in results)
        {
            grandTotal += r.TotalSizeBytes;
            var label = Loc.IsEnglish ? "files" : "文件";
            Console.WriteLine($"  {r.Category,-20} | {r.FileCount,5} {label} | {r.TotalSizeDisplay,10}");
        }

        Console.WriteLine();
        var totalLabel = Loc.IsEnglish ? "Total freeable: " : "  总计可释放: ";
        Console.WriteLine($"{totalLabel}{FormatSize(grandTotal)}");
    }

    private static async Task CleanAllAsync(CleanerEngine engine, bool autoYes)
    {
        Console.WriteLine(Loc.IsEnglish ? "Scanning...\n" : "正在扫描可清理内容...\n");

        var progress = new Progress<string>(msg => Console.WriteLine(msg));
        var scanResults = await engine.ScanAllAsync(progress);

        long grandTotal = scanResults.Sum(r => r.TotalSizeBytes);
        var estimateLabel = Loc.IsEnglish ? "Estimated space to free: " : "预计释放空间: ";
        Console.WriteLine($"\n{estimateLabel}{FormatSize(grandTotal)}\n");

        if (!autoYes)
        {
            Console.Write(Loc.IsEnglish ? "Confirm cleanup? (y/n): " : "确认执行清理? (y/n): ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key != ConsoleKey.Y)
            {
                Console.WriteLine(Loc.IsEnglish ? "Cleanup cancelled." : "已取消清理。");
                return;
            }
        }

        Console.WriteLine(Loc.IsEnglish ? "\nCleaning...\n" : "\n正在清理...\n");
        var cleanResults = await engine.CleanAllAsync(progress);

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine(Loc.IsEnglish ? "Cleanup complete!" : "清理完成!");
        Console.WriteLine("═══════════════════════════════════════");

        long totalFreed = 0;
        var cleanedLabel = Loc.IsEnglish ? "cleaned" : "清理";
        var freedLabel = Loc.IsEnglish ? "freed" : "释放";
        foreach (var r in cleanResults)
        {
            totalFreed += r.FreedBytes;
            var status = r.Success ? "√" : "×";
            Console.WriteLine($"  [{status}] {r.Category,-20} | {cleanedLabel} {r.ItemsCleaned} {(Loc.IsEnglish ? "items" : "项")} | {freedLabel} {r.FreedSizeDisplay}");
            foreach (var err in r.Errors.Take(3))
                Console.WriteLine($"       {err}");
        }

        var totalLabel = Loc.IsEnglish ? "Total freed: " : "  总计释放: ";
        Console.WriteLine($"\n{totalLabel}{FormatSize(totalFreed)}");
    }

    private static async Task CleanCategoryAsync(CleanerEngine engine,
        IReadOnlyList<Core.Cleaners.ICleaner> cleaners, string categoryName, bool autoYes)
    {
        var cleaner = cleaners.FirstOrDefault(c =>
            c.Category.Contains(categoryName, StringComparison.OrdinalIgnoreCase));

        if (cleaner == null)
        {
            Console.WriteLine(Loc.IsEnglish
                ? $"Category not found: {categoryName}"
                : $"未找到分类: {categoryName}");
            Console.WriteLine(Loc.IsEnglish ? "Available categories:" : "可用分类:");
            foreach (var c in cleaners)
                Console.WriteLine($"  · {c.Category}");
            return;
        }

        Console.WriteLine(Loc.IsEnglish ? $"Scanning: {cleaner.Category}..." : $"扫描: {cleaner.Category}...");
        var scanResult = await engine.ScanAsync(cleaner);
        var filesLabel = Loc.IsEnglish ? "files" : "个文件";
        Console.WriteLine(Loc.IsEnglish
            ? $"  Found {scanResult.FileCount} {filesLabel}, total {scanResult.TotalSizeDisplay}"
            : $"  发现 {scanResult.FileCount} {filesLabel}, 共 {scanResult.TotalSizeDisplay}");

        if (!autoYes)
        {
            Console.Write(Loc.IsEnglish ? "Confirm? (y/n): " : "确认清理? (y/n): ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key != ConsoleKey.Y)
            {
                Console.WriteLine(Loc.IsEnglish ? "Cancelled." : "已取消清理。");
                return;
            }
        }

        Console.WriteLine(Loc.IsEnglish ? "Cleaning..." : "正在清理...");
        var cleanResult = await engine.CleanAsync(cleaner);
        Console.WriteLine(Loc.IsEnglish
            ? $"  Done! Freed {cleanResult.FreedSizeDisplay}, {cleanResult.ItemsCleaned} items cleaned"
            : $"  完成! 释放 {cleanResult.FreedSizeDisplay}, {cleanResult.ItemsCleaned} 项已清理");
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{bytes / 1024.0:F2} KB",
        _ => $"{bytes} B"
    };
}
