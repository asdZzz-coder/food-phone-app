using food.Services;

namespace food.Mobile;

/// <summary>更多：Excel 匯出匯入、外觀（深色/淺色）、檢查更新、關於。</summary>
public class MorePage : TabPage
{
    readonly Label versionText = Ui.L1("", 12, Theme.Muted);

    public MorePage() : base("更多")
    {
        var themes = new[] { Theme.FollowSystem, Theme.LightMode, Theme.DarkMode, Theme.CuteTheme };
        var (themeSeg, _) = Ui.Segment(["跟隨系統", "淺色", "深色", "可愛 🎀"], Math.Max(Array.IndexOf(themes, Theme.Load()), 0), i =>
        {
            // 換進或換出可愛主題要重建畫面（等按鈕的點擊處理完再換），重建後停在「更多」
            if (Theme.Apply(themes[i])) Dispatcher.Dispatch(() => App.Rebuild(2));
        });
        bool cute = Theme.CuteMode;

        var look = new VerticalStackLayout { Spacing = 8, Children = { themeSeg } };
        if (cute)
            look.Add(Row(Cute.Set.Icon, $"可愛圖案：{Cute.Set.Name}", "換一組圖案：" + string.Join("、", Cute.Sets.Select(s => s.Name)), ChooseStickers));
        else
            look.Add(Ui.L("選「可愛 🎀」會換成粉嫩配色，餐廳頭像變成食物圖案，還有小動物、甜點、花園、海洋、星空、水果六組圖案可以挑。", 12, Theme.Subtle));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 8, Padding = new Thickness(16, 16, 16, 32),
                Children =
                {
                    Header("更多", 2),
                    Ui.Section("資料"),
                    Row(cute ? "📤" : "⬆", "匯出 Excel", "所有分類與餐廳（.xlsx），可存到雲端硬碟或傳到電腦；電腦版可以直接匯入", Ops.Export),
                    Row(cute ? "📥" : "⬇", "匯入 Excel", "電腦版匯出的檔案，或自己依「國家、縣市、地區、餐廳名稱、地址、電話、評分、備註」欄位填好的檔案", Ops.Import),
                    Ui.Section("外觀"),
                    look,
                    Ui.Section("關於"),
                    Row(cute ? "🔄" : "↻", "檢查更新", "有新版會下載新的安裝檔（APK）", () => Ops.CheckUpdate(silent: false)),
                    Row(cute ? "💌" : "ℹ", "關於餐廳收藏", "版本與資料位置", () => Dialogs.Info("關於餐廳收藏",
                        $"版本 {Updater.CurrentVersion}\n資料位置：{Path.Combine(DataStore.DataDirectory, "data.json")}\n\n" +
                        "資料存在手機裡，解除安裝 App 會一起刪除，記得定期「匯出 Excel」備份。")),
                    versionText,
                },
            },
        };
    }

    /// <summary>挑可愛主題的圖案組；換了就重建畫面，停在「更多」。</summary>
    async Task ChooseStickers()
    {
        var sets = Cute.Sets;
        var options = sets.Select(s => (s.Icon, s.Name + (s == Cute.Set ? "（目前）" : ""), s.Description)).ToArray();
        if (await Dialogs.Choose("可愛圖案", "分頁小圖、分類圖示、評分、對話框和大卡片的顏色都會換成這一組。", options) is not { } i) return;
        if (Cute.Choose(sets[i])) Dispatcher.Dispatch(() => App.Rebuild(2));
    }

    static View Row(string icon, string title, string sub, Func<Task> action)
    {
        var grid = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(Ui.IconBox(icon, Theme.Accent, Theme.AccentSoft), 0);
        grid.Add(new VerticalStackLayout
        {
            Spacing = 2, VerticalOptions = LayoutOptions.Center,
            Children = { Ui.L1(title, 15, bold: true), Ui.L(sub, 12, Theme.Muted) },
        }, 1);
        grid.Add(new Label { Text = "›", FontSize = 20, VerticalOptions = LayoutOptions.Center }.Fg(Theme.Subtle), 2);
        return Ui.Card(grid, 12).OnTap(action);
    }

    public override void Refresh() => versionText.Text = $"餐廳收藏 Android 版 v{Updater.CurrentVersion}";
}
