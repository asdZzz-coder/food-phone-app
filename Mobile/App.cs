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
}

/// <summary>底部三個分頁：餐廳、分類、更多。</summary>
public class AppShell : Shell
{
    public AppShell()
    {
        this.SetAppThemeColor(BackgroundColorProperty, Theme.Bg.Light, Theme.Bg.Dark);
        this.SetAppThemeColor(ForegroundColorProperty, Theme.Text.Light, Theme.Text.Dark);
        this.SetAppThemeColor(TitleColorProperty, Theme.Text.Light, Theme.Text.Dark);
        this.SetAppThemeColor(TabBarBackgroundColorProperty, Theme.Surface.Light, Theme.Surface.Dark);
        this.SetAppThemeColor(TabBarForegroundColorProperty, Theme.Accent.Light, Theme.Accent.Dark);
        this.SetAppThemeColor(TabBarTitleColorProperty, Theme.Accent.Light, Theme.Accent.Dark);
        this.SetAppThemeColor(TabBarUnselectedColorProperty, Theme.Subtle.Light, Theme.Subtle.Dark);

        var bar = new TabBar();
        bar.Items.Add(Tab("餐廳", "tab_restaurants.png", "restaurants", () => new RestaurantsPage()));
        bar.Items.Add(Tab("分類", "tab_categories.png", "categories", () => new CategoriesPage()));
        bar.Items.Add(Tab("更多", "tab_more.png", "more", () => new MorePage()));
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
