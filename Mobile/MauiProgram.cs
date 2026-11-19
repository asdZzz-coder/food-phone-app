using System.Globalization;

namespace food.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // 資料放在 App 自己的資料夾（解除安裝才會刪掉）；一定要在第一次用到 DataStore 之前設定
        Environment.SetEnvironmentVariable("FOODKEEPER_DATA_DIR", Path.Combine(FileSystem.AppDataDirectory, "FoodKeeper"));

        var culture = (CultureInfo)CultureInfo.GetCultureInfo("zh-TW").Clone();
        culture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if ANDROID
        // 輸入框外面已經有圓角框，拿掉 Android 預設的底線
        static void NoUnderline(Android.Views.View v) =>
            v.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => NoUnderline(h.PlatformView));
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => NoUnderline(h.PlatformView));
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => NoUnderline(h.PlatformView));
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoUnderline", (h, _) => NoUnderline(h.PlatformView));
#endif
        return builder.Build();
    }
}
