using food.Services;

namespace food.Mobile;

/// <summary>更多：Excel 匯出匯入、外觀（深色/淺色）、檢查更新、關於。</summary>
public class MorePage : TabPage
{
    readonly Label versionText = Ui.L1("", 12, Theme.Muted);

    public MorePage() : base("更多")
    {
        var themes = new[] { Theme.FollowSystem, Theme.LightMode, Theme.DarkMode };
        var (themeSeg, _) = Ui.Segment(["跟隨系統", "淺色", "深色"], Math.Max(Array.IndexOf(themes, Theme.Load()), 0), i => Theme.Apply(themes[i]));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 8, Padding = new Thickness(16, 16, 16, 32),
                Children =
                {
                    Header("更多"),
                    Ui.Section("資料"),
                    Row("⬆", "匯出 Excel", "所有分類與餐廳（.xlsx），可存到雲端硬碟或傳到電腦；電腦版可以直接匯入", Ops.Export),
                    Row("⬇", "匯入 Excel", "電腦版匯出的檔案，或自己依「國家、縣市、地區、餐廳名稱、地址、電話、評分、備註」欄位填好的檔案", Ops.Import),
                    Ui.Section("外觀"),
                    themeSeg,
                    Ui.Section("關於"),
                    Row("↻", "檢查更新", "有新版會下載新的安裝檔（APK）", () => Ops.CheckUpdate(silent: false)),
                    Row("ℹ", "關於餐廳收藏", "版本與資料位置", () => Dialogs.Info("關於餐廳收藏",
                        $"版本 {Updater.CurrentVersion}\n資料位置：{Path.Combine(DataStore.DataDirectory, "data.json")}\n\n" +
                        "資料存在手機裡，解除安裝 App 會一起刪除，記得定期「匯出 Excel」備份。")),
                    versionText,
                },
            },
        };
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
