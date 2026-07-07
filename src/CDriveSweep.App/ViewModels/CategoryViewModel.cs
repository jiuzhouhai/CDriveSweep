using System.ComponentModel;
using System.Collections.ObjectModel;
using CDriveSweep.Core.Localization;

namespace CDriveSweep.App.ViewModels;

/// <summary>
/// 清理分类的视图模型，绑定到 WPF 列表
/// </summary>
public class CategoryViewModel : INotifyPropertyChanged
{
    public Core.Cleaners.ICleaner Cleaner { get; init; } = null!;

    /// <summary>分类名（来自 Cleaner，通过 Loc 获取）</summary>
    public string Category => Cleaner.Category;
    /// <summary>描述（来自 Cleaner，通过 Loc 获取）</summary>
    public string Description => Cleaner.Description;

    public string RiskDisplay => Cleaner.Risk switch
    {
        Core.Cleaners.CleanerRisk.Low => Loc.Get("Risk_Low"),
        Core.Cleaners.CleanerRisk.Medium => Loc.Get("Risk_Medium"),
        Core.Cleaners.CleanerRisk.Review => Loc.Get("Risk_Review"),
        _ => ""
    };

    public string RiskColor => Cleaner.Risk switch
    {
        Core.Cleaners.CleanerRisk.Low => "#28A745",
        Core.Cleaners.CleanerRisk.Medium => "#FFC107",
        Core.Cleaners.CleanerRisk.Review => "#6C757D",
        _ => "#888"
    };

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                SelectedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    private long _sizeBytes;
    public long SizeBytes
    {
        get => _sizeBytes;
        set
        {
            if (_sizeBytes != value)
            {
                _sizeBytes = value;
                OnPropertyChanged(nameof(SizeBytes));
                OnPropertyChanged(nameof(SizeDisplay));
                OnPropertyChanged(nameof(HasSize));
            }
        }
    }

    private int _fileCount;
    public int FileCount
    {
        get => _fileCount;
        set
        {
            if (_fileCount != value)
            {
                _fileCount = value;
                OnPropertyChanged(nameof(FileCount));
                OnPropertyChanged(nameof(FileCountDisplay));
            }
        }
    }

    public ObservableCollection<ScanItemViewModel> ScanItems { get; } = new();

    public string SizeDisplay => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{SizeBytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{SizeBytes / 1024.0:F2} KB",
        > 0 => $"{SizeBytes} B",
        _ => ""
    };

    private bool _hasScanned;
    public bool HasScanned
    {
        get => _hasScanned;
        set
        {
            if (_hasScanned != value)
            {
                _hasScanned = value;
                OnPropertyChanged(nameof(HasScanned));
                OnPropertyChanged(nameof(FileCountDisplay));
            }
        }
    }

    public string FileCountDisplay
    {
        get
        {
            if (FileCount > 0)
                return $"{FileCount} {Loc.Get("Label_Files")}";
            if (_hasScanned)
                return Loc.Get("Label_NoFiles");
            return Loc.Get("Label_NotScanned");
        }
    }

    public bool HasSize => SizeBytes > 0;

    private string _detailInfo = string.Empty;
    public string DetailInfo
    {
        get => _detailInfo;
        set
        {
            if (_detailInfo != value)
            {
                _detailInfo = value;
                OnPropertyChanged(nameof(DetailInfo));
                OnPropertyChanged(nameof(HasDetail));
            }
        }
    }

    public bool HasDetail => !string.IsNullOrEmpty(DetailInfo);

    public event EventHandler? SelectedChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>语言切换后刷新所有绑定属性</summary>
    public void RefreshLocalized()
    {
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(RiskDisplay));
        OnPropertyChanged(nameof(FileCountDisplay));
        OnPropertyChanged(nameof(DetailInfo));
    }
}
