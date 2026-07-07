using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CDriveSweep.App.ViewModels;
using CDriveSweep.Core;
using CDriveSweep.Core.Localization;

namespace CDriveSweep.App;

public partial class MainWindow : Window
{
    private readonly CleanerEngine _engine = new();
    private readonly ObservableCollection<CategoryViewModel> _categories = new();
    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();

        foreach (var cleaner in _engine.GetCleaners())
        {
            var vm = new CategoryViewModel { Cleaner = cleaner };
            vm.IsSelected = cleaner.Risk == CDriveSweep.Core.Cleaners.CleanerRisk.Low;
            vm.SelectedChanged += (_, _) => UpdateTotalSelected();
            _categories.Add(vm);
        }

        CategoriesPanel.ItemsSource = _categories;
        BtnClean.IsEnabled = false;

        // 语言切换事件
        Loc.LanguageChanged += () => Dispatcher.Invoke(RefreshUILanguage);

        RefreshUILanguage();
    }

    private void RefreshUILanguage()
    {
        // 标题和按钮
        Title = Loc.Get("AppTitle");
        TxtTitle.Text = "CDriveSweep";
        TxtSubTitle.Text = $" - {Loc.Get("AppSubTitle")}";
        BtnScan.Content = Loc.Get("BtnScan");
        BtnClean.Content = Loc.Get("BtnClean");
        BtnSelectAll.Content = Loc.Get("BtnSelectAll");
        BtnDeselectAll.Content = Loc.Get("BtnDeselectAll");
        BtnLanguage.Content = Loc.Get("BtnLanguage");

        // 提示区域
        TxtSafetyTip.Text = "⚠ " + Loc.Get("SafetyTip");
        TxtLabelSelected.Text = Loc.Get("LabelSelected");
        TxtStatusTip.Text = " | " + Loc.Get("TipExpand");

        // 状态和进度
        if (!_isRunning)
        {
            TxtStatus.Text = Loc.Get("StatusReady");
            TxtProgress.Text = Loc.Get("StatusReady");
        }

        // 刷新所有分类
        foreach (var vm in _categories)
            vm.RefreshLocalized();
    }

    private void BtnLanguage_Click(object sender, RoutedEventArgs e)
    {
        Loc.CurrentCulture = Loc.IsEnglish
            ? System.Globalization.CultureInfo.GetCultureInfo(Loc.ZhCN)
            : System.Globalization.CultureInfo.GetCultureInfo(Loc.EnUS);
    }

    private void CategoryHeader_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is CategoryViewModel vm)
            vm.IsExpanded = !vm.IsExpanded;
    }

    private void CheckBox_Click(object sender, RoutedEventArgs e) => e.Handled = true;

    private void ScanItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScanItemViewModel item)
        {
            if (item.OpenCommand.CanExecute(null))
                item.OpenCommand.Execute(null);
        }
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;

        var selected = _categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(Loc.Get("Msg_SelectCategory"), Loc.Get("Dlg_Tip"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _isRunning = true;
        SetButtonsEnabled(false);
        TxtStatus.Text = string.Format(Loc.Get("StatusScanning"), selected.Count);

        foreach (var vm in _categories)
        {
            if (!vm.IsSelected)
            {
                vm.SizeBytes = 0;
                vm.FileCount = 0;
                vm.HasScanned = false;
                vm.ScanItems.Clear();
                vm.IsExpanded = false;
                vm.DetailInfo = "";
            }
        }

        foreach (var vm in selected)
        {
            TxtProgress.Text = string.Format(Loc.Get("StatusCleaning"),
                selected.IndexOf(vm) + 1, selected.Count);

            await Task.Run(async () =>
            {
                var result = await _engine.ScanAsync(vm.Cleaner);
                Dispatcher.Invoke(() =>
                {
                    vm.SizeBytes = result.TotalSizeBytes;
                    vm.FileCount = result.FileCount;
                    vm.HasScanned = true;
                    vm.ScanItems.Clear();
                    foreach (var item in result.Items.Take(200))
                        vm.ScanItems.Add(new ScanItemViewModel(item));
                    if (result.Items.Count > 200)
                        vm.DetailInfo = $"共 {result.Items.Count} 个文件，此处只展示前 200 个";
                });
            });
        }

        UpdateTotalSelected();
        BtnClean.IsEnabled = true;
        TxtStatus.Text = string.Format(Loc.Get("StatusComplete"), selected.Count, selected.Sum(c => c.FileCount));
        TxtProgress.Text = Loc.Get("StatusComplete");
        _isRunning = false;
        SetButtonsEnabled(true);
    }

    private async void BtnClean_Click(object sender, RoutedEventArgs e)
    {
        var selected = _categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(Loc.Get("Msg_SelectCategory"), Loc.Get("Dlg_Tip"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmMsg = string.Format(Loc.Get("Msg_CannotUndo"), selected.Count) + "\n\n" + Loc.Get("Msg_ConfirmContinue");
        var result = MessageBox.Show(confirmMsg, Loc.Get("Msg_ConfirmClean"),
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        if (_isRunning) return;
        _isRunning = true;
        SetButtonsEnabled(false);
        BtnClean.IsEnabled = false;

        long totalFreed = 0;
        int totalCleaned = 0;
        int totalFailed = 0;
        var failedCategories = new List<string>();

        foreach (var vm in selected)
        {
            TxtStatus.Text = $"{Loc.Get("StatusCleaning")}: {vm.Category}";
            TxtProgress.Text = string.Format(Loc.Get("StatusCleaning"),
                selected.IndexOf(vm) + 1, selected.Count);

            await Task.Run(async () =>
            {
                var cleanResult = await _engine.CleanAsync(vm.Cleaner);
                Dispatcher.Invoke(() =>
                {
                    totalFreed += cleanResult.FreedBytes;
                    totalCleaned += cleanResult.ItemsCleaned;
                    totalFailed += cleanResult.ItemsFailed;

                    vm.SizeBytes = 0;
                    vm.FileCount = 0;
                    vm.ScanItems.Clear();
                    vm.IsExpanded = false;

                    if (cleanResult.Success && cleanResult.ItemsFailed == 0)
                        vm.DetailInfo = string.Format(Loc.Get("Label_Cleaned"), cleanResult.ItemsCleaned);
                    else if (cleanResult.ItemsFailed > 0)
                    {
                        vm.DetailInfo = string.Format(Loc.Get("Label_Failed"), cleanResult.ItemsCleaned, cleanResult.ItemsFailed);
                        failedCategories.Add(vm.Category);
                    }
                    else
                        vm.DetailInfo = $"{(Loc.IsEnglish ? "Error: " : "清理出错: ")}{cleanResult.Errors.FirstOrDefault() ?? Loc.Get("Msg_Canceled")}";
                });
            });
        }

        UpdateTotalSelected();
        TxtStatus.Text = $"{Loc.Get("Msg_CleanComplete")} {string.Format(Loc.Get("Msg_CleanedItems"), totalCleaned)}, {string.Format(Loc.Get("Msg_FreedSpace"), FormatSize(totalFreed))}";
        TxtProgress.Text = Loc.Get("Msg_CleanComplete");
        _isRunning = false;
        SetButtonsEnabled(true);
        BtnClean.IsEnabled = true;

        var msg = $"{Loc.Get("Msg_CleanComplete")}\n\n{string.Format(Loc.Get("Msg_CleanedItems"), totalCleaned)}\n{string.Format(Loc.Get("Msg_FreedSpace"), FormatSize(totalFreed))}";
        if (totalFailed > 0)
        {
            msg += $"\n{string.Format(Loc.Get("Msg_FailedItems"), totalFailed)}";
            msg += $"\n\n{Loc.Get("Msg_AdminHint")}";
        }
        MessageBox.Show(msg, Loc.Get("Msg_CleanComplete"), MessageBoxButton.OK,
            totalFailed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var vm in _categories) vm.IsSelected = true;
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var vm in _categories) vm.IsSelected = false;
    }

    private void UpdateTotalSelected()
    {
        var total = _categories.Where(c => c.IsSelected).Sum(c => c.SizeBytes);
        TxtTotalSelected.Text = FormatSize(total);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        BtnScan.IsEnabled = enabled;
        BtnClean.IsEnabled = enabled && _categories.Any(c => c.HasScanned);
        BtnSelectAll.IsEnabled = enabled;
        BtnDeselectAll.IsEnabled = enabled;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{bytes / 1024.0:F2} KB",
        _ => $"{bytes} B"
    };
}
