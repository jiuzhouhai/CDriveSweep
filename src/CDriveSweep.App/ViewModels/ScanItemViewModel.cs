using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CDriveSweep.Core.Models;

namespace CDriveSweep.App.ViewModels;

/// <summary>
/// 单个扫描文件/目录的视图模型，支持点击打开位置
/// </summary>
public class ScanItemViewModel
{
    private readonly ScanItem _item;

    public ScanItemViewModel(ScanItem item)
    {
        _item = item;
        OpenCommand = new RelayCommand(OpenInExplorer);
    }

    public string Name => _item.Name;
    public string Description => _item.Description;
    public string FullPath => _item.Path;
    public string SizeDisplay => _item.SizeDisplay;
    public bool IsDirectory => _item.IsDirectory;
    public string TypeIcon => IsDirectory ? "📁" : "📄";

    public ICommand OpenCommand { get; }

    private void OpenInExplorer()
    {
        try
        {
            if (File.Exists(_item.Path))
            {
                Process.Start("explorer.exe", $"/select,\"{_item.Path}\"");
            }
            else if (Directory.Exists(_item.Path))
            {
                Process.Start("explorer.exe", _item.Path);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"无法打开资源管理器: {ex.Message}");
        }
    }
}
