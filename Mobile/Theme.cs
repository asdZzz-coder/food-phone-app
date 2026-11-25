using Microsoft.Maui.Controls.Shapes;

namespace food.Mobile;

/// <summary>
/// 配色與電腦版 ThemeService.Palette 相同（暖白底、橘色重點色）。每個顏色是一組（淺色, 深色），
/// 介面用 SetAppThemeColor 綁定，切換深色/淺色時自動換色。
/// 「可愛」主題是淺色的粉嫩版本：Light 會改成 Cute 的顏色（沒有另外指定就沿用淺色），圖案見 <see cref="Cute"/>。
/// </summary>
public sealed class Pair(Color light, Color dark, Color? cute = null)
{
    public Color Light => Theme.CuteMode && cute is { } c ? c : light;
    public Color Dark => dark;
    public Color Now => Theme.IsDark ? Dark : Light;
}

public static class Theme
{
    static Pair P(string light, string dark, string? cute = null) =>
        new(Color.FromArgb(light), Color.FromArgb(dark), cute == null ? null : Color.FromArgb(cute));

    //                                          淺色        深色        可愛
    public static readonly Pair Bg = P("#F7F5F2", "#141312", "#FFE6EF");
    public static readonly Pair Surface = P("#FFFFFF", "#1E1C1A");
    public static readonly Pair InputBg = P("#FAF8F5", "#181614", "#FFF5F9");
    public static readonly Pair Text = P("#111827", "#ECE8E4", "#5B2E43");
    public static readonly Pair Muted = P("#6B7280", "#A8A29E", "#A0687F");
    public static readonly Pair Subtle = P("#9CA3AF", "#78716C", "#C48BA3");
    public static readonly Pair Line = P("#ECE7E1", "#2F2B28", "#F6BDD2");
    public static readonly Pair LineStrong = P("#D6CFC7", "#4A443F", "#EE9DBC");
    public static readonly Pair Ghost = P("#EFEAE4", "#2A2725", "#FFD9E7");
    public static readonly Pair Accent = P("#D9480F", "#F0702A", "#E0508A");
    public static readonly Pair AccentSoft = P("#FFF1E8", "#3A2418", "#FFE3EE");
    public static readonly Pair OnAccent = P("#FFFFFF", "#FFFFFF");
    public static readonly Pair Danger = P("#C81E1E", "#F87171", "#D7334F");
    public static readonly Pair DangerSoft = P("#FEF2F2", "#2C1B1F", "#FDE8EC");
    public static readonly Pair Star = P("#F59E0B", "#FBBF24", "#FF4F93");
    public static readonly Pair StarSoft = P("#FEF3C7", "#3A2E12", "#FFE0EC");
    public static readonly Pair StarEmpty = P("#E2DCD5", "#4A443F", "#F6C7D8");
    /// <summary>橘紅漸層的大卡片上，字固定用白色系；可愛主題的大卡片是粉嫩漸層，字改成深莓色。</summary>
    public static readonly Pair OnHero = P("#FFFFFF", "#FFFFFF", "#5B3446");
    public static readonly Pair OnHeroMuted = P("#FFE4D5", "#FFE4D5", "#7A4560");
    /// <summary>大卡片上的半透明按鈕底色。</summary>
    public static readonly Pair HeroBtn = P("#33FFFFFF", "#33FFFFFF", "#B3FFFFFF");
    /// <summary>分頁列（底部）：可愛主題用粉紅色，一眼就看得出換了主題。</summary>
    public static readonly Pair TabBar = P("#FFFFFF", "#1E1C1A", "#FFD3E3");
    public static readonly Pair TabOff = P("#9CA3AF", "#78716C", "#B0708C");

    public static Brush Gradient(string a, string b) => new LinearGradientBrush(
        [new GradientStop(Color.FromArgb(a), 0), new GradientStop(Color.FromArgb(b), 1)], new Point(0, 0), new Point(1, 1));

    // 與電腦版 ThemeService.Gradient 相同
    static readonly Brush HeroLight = Gradient("#F97316", "#DC2626"), HeroDark = Gradient("#EA6A1A", "#C23A2E");

    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    // ---------- 主題偏好：跟隨系統 / 淺色 / 深色 / 可愛（存在資料夾的 theme.txt，與電腦版相同的寫法） ----------
    public const string FollowSystem = "system", LightMode = "light", DarkMode = "dark", CuteTheme = "cute";

    /// <summary>目前是可愛主題（粉嫩的淺色）。綁定顏色時就決定了，所以切換時要重建畫面（<see cref="App.Rebuild"/>）。</summary>
    public static bool CuteMode { get; private set; }

    static string ThemeFile => System.IO.Path.Combine(food.Services.DataStore.DataDirectory, "theme.txt");

    public static string Load()
    {
        try { return File.Exists(ThemeFile) ? File.ReadAllText(ThemeFile).Trim().ToLowerInvariant() : FollowSystem; }
        catch (IOException) { return FollowSystem; }
        catch (UnauthorizedAccessException) { return FollowSystem; }
    }

    /// <summary>套用主題；回傳是否換進或換出可愛主題（是的話畫面要重建）。</summary>
    public static bool Apply(string mode, bool save = true)
    {
        bool cute = mode == CuteTheme, changed = cute != CuteMode;
        CuteMode = cute;
        if (Application.Current is { } app)
            app.UserAppTheme = mode == DarkMode ? AppTheme.Dark : mode is LightMode or CuteTheme ? AppTheme.Light : AppTheme.Unspecified;
        if (save) Write(ThemeFile, mode);
        return changed;
    }

    /// <summary>把設定寫進資料夾裡的小檔案（寫不進去就算了，下次用預設值）。</summary>
    public static void Write(string file, string text)
    {
        try
        {
            Directory.CreateDirectory(food.Services.DataStore.DataDirectory);
            File.WriteAllText(file, text);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>可愛主題的角更圓。</summary>
    public static double R(double r) => CuteMode ? Math.Round(r * 1.4) : r;

    // ---------- 餐廳頭像：依名稱固定挑一個顏色（與電腦版 RestaurantRow.AvatarColor 相同） ----------
    static readonly string[] AvatarColors =
    [
        "#EA580C", "#DC2626", "#D97706", "#16A34A", "#0891B2",
        "#2563EB", "#7C3AED", "#DB2777", "#65A30D", "#0D9488",
    ];

    /// <summary>可愛主題的頭像底色：粉嫩色，上面放食物圖案。</summary>
    static readonly string[] CuteAvatarColors =
    [
        "#FFC9DA", "#FFDDB8", "#FFF0A8", "#C8EED6", "#C4E4FF",
        "#DCCBFF", "#FFCCEF", "#CDF3EC", "#FFD6C9", "#E4F2B8",
    ];

    static uint Hash(string name)
    {
        uint h = 2166136261; // FNV-1a，跨次啟動結果一致
        foreach (var c in name.Trim().ToUpperInvariant()) { h ^= c; h *= 16777619; }
        return h;
    }

    public static Brush Avatar(string name)
    {
        var colors = CuteMode ? CuteAvatarColors : AvatarColors;
        var color = Color.FromArgb(colors[Hash(name) % (uint)colors.Length]);
        float k = CuteMode ? 0.9f : 0.78f;
        var dark = new Color(color.Red * k, color.Green * k, color.Blue * k);
        return new LinearGradientBrush([new GradientStop(color, 0), new GradientStop(dark, 1)], new Point(0, 0), new Point(1, 1));
    }

    /// <summary>頭像上的字：名稱第一個字；可愛主題改成猜出來的食物圖案（見 <see cref="Cute.Food"/>）。</summary>
    public static string Initial(string name, string note = "")
    {
        if (CuteMode) return Cute.Food(name, note);
        return string.IsNullOrWhiteSpace(name) ? "?" : name.Trim().EnumerateRunes().First().ToString().ToUpperInvariant();
    }

    // ---------- 綁定顏色的小工具 ----------
    public static T Fg<T>(this T label, Pair c) where T : Label
    {
        label.SetAppThemeColor(Label.TextColorProperty, c.Light, c.Dark);
        return label;
    }

    public static Border Fill(this Border b, Pair c)
    {
        b.SetAppThemeColor(Border.BackgroundColorProperty, c.Light, c.Dark);
        return b;
    }

    public static Border Stroke_(this Border b, Pair c)
    {
        b.SetAppThemeColor(Border.StrokeProperty, c.Light, c.Dark);
        return b;
    }

    public static Border Hero(this Border b)
    {
        b.SetAppTheme<Brush>(Border.BackgroundProperty, CuteMode ? Cute.Set.Hero : HeroLight, HeroDark);
        return b;
    }

    public static RoundRectangle Round(double r) => new() { CornerRadius = new CornerRadius(R(r)) };
}
