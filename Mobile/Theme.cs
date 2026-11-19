using Microsoft.Maui.Controls.Shapes;

namespace food.Mobile;

/// <summary>
/// 配色與電腦版 ThemeService.Palette 相同（暖白底、橘色重點色）。每個顏色是一組（淺色, 深色），
/// 介面用 SetAppThemeColor 綁定，切換深色/淺色時自動換色。
/// </summary>
public record Pair(Color Light, Color Dark)
{
    public Color Now => Theme.IsDark ? Dark : Light;
}

public static class Theme
{
    static Pair P(string light, string dark) => new(Color.FromArgb(light), Color.FromArgb(dark));

    public static readonly Pair Bg = P("#F7F5F2", "#141312");
    public static readonly Pair Surface = P("#FFFFFF", "#1E1C1A");
    public static readonly Pair InputBg = P("#FAF8F5", "#181614");
    public static readonly Pair Text = P("#111827", "#ECE8E4");
    public static readonly Pair Muted = P("#6B7280", "#A8A29E");
    public static readonly Pair Subtle = P("#9CA3AF", "#78716C");
    public static readonly Pair Line = P("#ECE7E1", "#2F2B28");
    public static readonly Pair LineStrong = P("#D6CFC7", "#4A443F");
    public static readonly Pair Ghost = P("#EFEAE4", "#2A2725");
    public static readonly Pair Accent = P("#D9480F", "#F0702A");
    public static readonly Pair AccentSoft = P("#FFF1E8", "#3A2418");
    public static readonly Pair OnAccent = P("#FFFFFF", "#FFFFFF");
    public static readonly Pair Danger = P("#C81E1E", "#F87171");
    public static readonly Pair DangerSoft = P("#FEF2F2", "#2C1B1F");
    public static readonly Pair Star = P("#F59E0B", "#FBBF24");
    public static readonly Pair StarSoft = P("#FEF3C7", "#3A2E12");
    public static readonly Pair StarEmpty = P("#E2DCD5", "#4A443F");
    /// <summary>橘紅漸層的大卡片上，字固定用白色系。</summary>
    public static readonly Pair OnHero = P("#FFFFFF", "#FFFFFF");
    public static readonly Pair OnHeroMuted = P("#FFE4D5", "#FFE4D5");

    static Brush Gradient(string a, string b) => new LinearGradientBrush(
        [new GradientStop(Color.FromArgb(a), 0), new GradientStop(Color.FromArgb(b), 1)], new Point(0, 0), new Point(1, 1));

    // 與電腦版 ThemeService.Gradient 相同
    static readonly Brush HeroLight = Gradient("#F97316", "#DC2626"), HeroDark = Gradient("#EA6A1A", "#C23A2E");

    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    // ---------- 主題偏好：跟隨系統 / 淺色 / 深色（存在資料夾的 theme.txt，與電腦版相同的寫法） ----------
    public const string FollowSystem = "system", LightMode = "light", DarkMode = "dark";

    static string ThemeFile => System.IO.Path.Combine(food.Services.DataStore.DataDirectory, "theme.txt");

    public static string Load()
    {
        try { return File.Exists(ThemeFile) ? File.ReadAllText(ThemeFile).Trim().ToLowerInvariant() : FollowSystem; }
        catch (IOException) { return FollowSystem; }
        catch (UnauthorizedAccessException) { return FollowSystem; }
    }

    public static void Apply(string mode, bool save = true)
    {
        if (Application.Current is { } app)
            app.UserAppTheme = mode == DarkMode ? AppTheme.Dark : mode == LightMode ? AppTheme.Light : AppTheme.Unspecified;
        if (!save) return;
        try
        {
            Directory.CreateDirectory(food.Services.DataStore.DataDirectory);
            File.WriteAllText(ThemeFile, mode);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // ---------- 餐廳頭像：依名稱固定挑一個顏色（與電腦版 RestaurantRow.AvatarColor 相同） ----------
    static readonly string[] AvatarColors =
    [
        "#EA580C", "#DC2626", "#D97706", "#16A34A", "#0891B2",
        "#2563EB", "#7C3AED", "#DB2777", "#65A30D", "#0D9488",
    ];

    public static Brush Avatar(string name)
    {
        uint h = 2166136261; // FNV-1a，跨次啟動結果一致
        foreach (var c in name.Trim().ToUpperInvariant()) { h ^= c; h *= 16777619; }
        var color = Color.FromArgb(AvatarColors[h % (uint)AvatarColors.Length]);
        var dark = new Color(color.Red * 0.78f, color.Green * 0.78f, color.Blue * 0.78f);
        return new LinearGradientBrush([new GradientStop(color, 0), new GradientStop(dark, 1)], new Point(0, 0), new Point(1, 1));
    }

    public static string Initial(string name) => string.IsNullOrWhiteSpace(name)
        ? "?"
        : name.Trim().EnumerateRunes().First().ToString().ToUpperInvariant();

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
        b.SetAppTheme<Brush>(Border.BackgroundProperty, HeroLight, HeroDark);
        return b;
    }

    public static RoundRectangle Round(double r) => new() { CornerRadius = new CornerRadius(r) };
}
