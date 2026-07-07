using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace CDriveSweep.Core.Localization;

/// <summary>
/// 本地化管理器，支持运行时中英文切换
/// </summary>
public static class Loc
{
    public const string ZhCN = "zh-CN";
    public const string EnUS = "en-US";

    private static readonly ResourceManager _rm = new("CDriveSweep.Core.Localization.Strings",
        typeof(Loc).Assembly);

    /// <summary>当前 UI 语言变化事件，通知 WPF 刷新绑定</summary>
    public static event Action? LanguageChanged;

    private static CultureInfo _currentCulture = CultureInfo.GetCultureInfo(ZhCN);

    /// <summary>当前语言</summary>
    public static CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Name != value.Name)
            {
                _currentCulture = value;
                CultureInfo.CurrentUICulture = value;
                LanguageChanged?.Invoke();
            }
        }
    }

    /// <summary>当前是否为英文</summary>
    public static bool IsEnglish => _currentCulture.Name == EnUS;

    /// <summary>获取本地化字符串</summary>
    public static string Get(string key) => _rm.GetString(key, _currentCulture) ?? $"#{key}#";

    // ---------- 常用字符串属性（供 WPF 绑定） ----------
    public static string AppTitle => Get("AppTitle");
    public static string AppSubTitle => Get("AppSubTitle");
    public static string BtnScan => Get("BtnScan");
    public static string BtnClean => Get("BtnClean");
    public static string BtnSelectAll => Get("BtnSelectAll");
    public static string BtnDeselectAll => Get("BtnDeselectAll");
    public static string BtnLanguage => Get("BtnLanguage");
    public static string StatusReady => Get("StatusReady");
    public static string StatusScanning => Get("StatusScanning");
    public static string StatusCleaning => Get("StatusCleaning");
    public static string StatusComplete => Get("StatusComplete");
    public static string LabelSelected => Get("LabelSelected");
    public static string TipAdmin => Get("TipAdmin");
    public static string TipExpand => Get("TipExpand");
    public static string SafetyTip => Get("SafetyTip");

    public static string Cleaner_SystemTemp => Get("Cleaner_SystemTemp");
    public static string Cleaner_SystemTemp_Desc => Get("Cleaner_SystemTemp_Desc");
    public static string Cleaner_RecycleBin => Get("Cleaner_RecycleBin");
    public static string Cleaner_RecycleBin_Desc => Get("Cleaner_RecycleBin_Desc");
    public static string Cleaner_BrowserCache => Get("Cleaner_BrowserCache");
    public static string Cleaner_BrowserCache_Desc => Get("Cleaner_BrowserCache_Desc");
    public static string Cleaner_WinUpdate => Get("Cleaner_WinUpdate");
    public static string Cleaner_WinUpdate_Desc => Get("Cleaner_WinUpdate_Desc");
    public static string Cleaner_Prefetch => Get("Cleaner_Prefetch");
    public static string Cleaner_Prefetch_Desc => Get("Cleaner_Prefetch_Desc");
    public static string Cleaner_Thumbnail => Get("Cleaner_Thumbnail");
    public static string Cleaner_Thumbnail_Desc => Get("Cleaner_Thumbnail_Desc");
    public static string Cleaner_LogFiles => Get("Cleaner_LogFiles");
    public static string Cleaner_LogFiles_Desc => Get("Cleaner_LogFiles_Desc");
    public static string Cleaner_Dns => Get("Cleaner_Dns");
    public static string Cleaner_Dns_Desc => Get("Cleaner_Dns_Desc");

    public static string Cleaner_MemoryDump => Get("Cleaner_MemoryDump");
    public static string Cleaner_MemoryDump_Desc => Get("Cleaner_MemoryDump_Desc");
    public static string Cleaner_DeliveryOpt => Get("Cleaner_DeliveryOpt");
    public static string Cleaner_DeliveryOpt_Desc => Get("Cleaner_DeliveryOpt_Desc");
    public static string Cleaner_ErrorReport => Get("Cleaner_ErrorReport");
    public static string Cleaner_ErrorReport_Desc => Get("Cleaner_ErrorReport_Desc");
    public static string Cleaner_WinOld => Get("Cleaner_WinOld");
    public static string Cleaner_WinOld_Desc => Get("Cleaner_WinOld_Desc");
    public static string Cleaner_WeChat => Get("Cleaner_WeChat");
    public static string Cleaner_WeChat_Desc => Get("Cleaner_WeChat_Desc");
    public static string Cleaner_QQ => Get("Cleaner_QQ");
    public static string Cleaner_QQ_Desc => Get("Cleaner_QQ_Desc");
    public static string Cleaner_NuGet => Get("Cleaner_NuGet");
    public static string Cleaner_NuGet_Desc => Get("Cleaner_NuGet_Desc");
    public static string Cleaner_LargeFiles => Get("Cleaner_LargeFiles");
    public static string Cleaner_LargeFiles_Desc => Get("Cleaner_LargeFiles_Desc");
    public static string Cleaner_Duplicates => Get("Cleaner_Duplicates");
    public static string Cleaner_Duplicates_Desc => Get("Cleaner_Duplicates_Desc");
    public static string Cleaner_EmptyFolders => Get("Cleaner_EmptyFolders");
    public static string Cleaner_EmptyFolders_Desc => Get("Cleaner_EmptyFolders_Desc");

    public static string Risk_Low => Get("Risk_Low");
    public static string Risk_Medium => Get("Risk_Medium");
    public static string Risk_Review => Get("Risk_Review");

    public static string Msg_SelectCategory => Get("Msg_SelectCategory");
    public static string Msg_ConfirmClean => Get("Msg_ConfirmClean");
    public static string Msg_ScanComplete => Get("Msg_ScanComplete");
    public static string Msg_CleanComplete => Get("Msg_CleanComplete");
    public static string Msg_CleanedItems => Get("Msg_CleanedItems");
    public static string Msg_FreedSpace => Get("Msg_FreedSpace");
    public static string Msg_FailedItems => Get("Msg_FailedItems");
    public static string Msg_Canceled => Get("Msg_Canceled");
    public static string Msg_AdminHint => Get("Msg_AdminHint");
    public static string Msg_CannotUndo => Get("Msg_CannotUndo");
    public static string Msg_ConfirmContinue => Get("Msg_ConfirmContinue");
    public static string Msg_Yes => Get("Msg_Yes");
    public static string Msg_No => Get("Msg_No");

    public static string Label_NotScanned => Get("Label_NotScanned");
    public static string Label_NoFiles => Get("Label_NoFiles");
    public static string Label_Files => Get("Label_Files");
    public static string Label_Cleaned => Get("Label_Cleaned");
    public static string Label_Failed => Get("Label_Failed");
}
