namespace food.Mobile;

public class App : Application
{
    public App()
    {
        Theme.Apply(Theme.Load(), save: false);
        RequestedThemeChanged += (_, _) => SystemBars.Update();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "餐廳收藏" };
        window.Created += (_, _) =>
        {
            SystemBars.Update();
            // 啟動後安靜地檢查一次更新，有新版才提示
            Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () => Ui.Run(() => Ops.CheckUpdate(silent: true)));
        };
        return window;
    }

    /// <summary>
    /// 換進或換出可愛主題、換圖案組後重建整個畫面：顏色和圖案是在建立元件時決定的，要重新建立才會套用。
    /// <paramref name="tab"/> 是重建後要停在哪個分頁（0 = 餐廳、1 = 分類、2 = 更多）。
    /// </summary>
    public static void Rebuild(int tab)
    {
        if (Current?.Windows.FirstOrDefault() is not { } window) return;
        window.Page = new AppShell(tab);
        SystemBars.Update();
    }
}

/// <summary>底部三個分頁：餐廳、分類、更多。</summary>
public class AppShell : Shell
{
    public AppShell(int tab = 0)
    {
        this.SetAppThemeColor(BackgroundColorProperty, Theme.Bg.Light, Theme.Bg.Dark);
        this.SetAppThemeColor(ForegroundColorProperty, Theme.Text.Light, Theme.Text.Dark);
        this.SetAppThemeColor(TitleColorProperty, Theme.Text.Light, Theme.Text.Dark);
        this.SetAppThemeColor(TabBarBackgroundColorProperty, Theme.TabBar.Light, Theme.TabBar.Dark);
        this.SetAppThemeColor(TabBarForegroundColorProperty, Theme.Accent.Light, Theme.Accent.Dark);
        this.SetAppThemeColor(TabBarTitleColorProperty, Theme.Accent.Light, Theme.Accent.Dark);
        this.SetAppThemeColor(TabBarUnselectedColorProperty, Theme.TabOff.Light, Theme.TabOff.Dark);

        var bar = new TabBar();
        bar.Items.Add(Tab("餐廳", "tab_restaurants.png", "restaurants", () => new RestaurantsPage()));
        bar.Items.Add(Tab("分類", "tab_categories.png", "categories", () => new CategoriesPage()));
        bar.Items.Add(Tab("更多", "tab_more.png", "more", () => new MorePage()));
        bar.CurrentItem = bar.Items[tab];
        Items.Add(bar);
    }

    static ShellContent Tab(string title, string icon, string route, Func<Page> create) =>
        new() { Title = title, Icon = icon, Route = route, ContentTemplate = new DataTemplate(create) };
}

/// <summary>狀態列、導覽列的圖示顏色跟著深色/淺色切換。</summary>
static partial class SystemBars
{
    public static partial void Update();
}
