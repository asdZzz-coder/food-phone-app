namespace food.Mobile;

/// <summary>
/// 底部分頁的共同基底：不顯示標題列（頁面自己畫標題），出現時與資料變動後重新整理，
/// 切換深色/淺色時也重新整理（清單裡預先算好的顏色要換）。
/// </summary>
public abstract class TabPage : ContentPage
{
    protected TabPage(string title)
    {
        this.Page(title);
        Shell.SetNavBarIsVisible(this, false);
        // 換成可愛主題時整個畫面會重建，舊的頁面已不在視窗裡，就不再更新
        State.Changed += () => { if (Window != null) Refresh(); };
        if (Application.Current is { } app) app.RequestedThemeChanged += (_, _) => { if (Window != null) Refresh(); };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    public abstract void Refresh();

    /// <summary>頁面最上方的大標題與說明（說明可以是會更新的 Label）；可愛主題在標題後加裝飾、右邊放這一頁的小圖。</summary>
    /// <param name="tab">第幾個分頁（0 = 餐廳、1 = 分類、2 = 更多），決定小圖</param>
    protected static View Header(string title, int tab, View? sub = null, View? right = null)
    {
        // 右邊有按鈕時位置不夠，小圖改放在標題後面
        var text = Theme.CuteMode && right != null ? $"{title} {Cute.Mascot(tab)}" : Cute.Title(title);
        var s = new VerticalStackLayout { Spacing = 2, Children = { Ui.L(text, 24, bold: true) } };
        if (sub != null) s.Add(sub);
        var grid = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star) } };
        grid.Add(s, 0);
        if (Theme.CuteMode && right == null)
        {
            grid.ColumnDefinitions.Add(new(GridLength.Auto));
            grid.Add(new Label { Text = Cute.Mascot(tab), FontSize = 40, VerticalOptions = LayoutOptions.Center }, grid.ColumnDefinitions.Count - 1);
        }
        if (right != null)
        {
            right.VerticalOptions = LayoutOptions.Center;
            grid.ColumnDefinitions.Add(new(GridLength.Auto));
            grid.Add(right, grid.ColumnDefinitions.Count - 1);
        }
        return grid;
    }
}

/// <summary>
/// 從分頁點進去的頁面（新增 / 編輯餐廳）：自己畫「‹ 標題」列，不用 Shell 的標題列
/// （Shell 的標題列在 Android 15 會在狀態列底下留一條深色）。
/// </summary>
public abstract class SubPage : ContentPage
{
    readonly Label titleLabel = Ui.L1("", 20, bold: true);

    protected SubPage(string title)
    {
        this.Page(title);
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false); // 填表單時不需要底部分頁，按鈕可以貼底
        titleLabel.Text = title;
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Title)) titleLabel.Text = Title; };
    }

    /// <summary>頁面內容（標題列下面那一塊）。</summary>
    protected View Body
    {
        set
        {
            var back = Ui.Btn("‹", Ui.Kind.Ghost, () => Navigation.PopAsync(), 40);
            back.WidthRequest = 44;
            back.Padding = 0;
            back.FontSize = 22;
            titleLabel.VerticalOptions = LayoutOptions.Center;
            var head = new Grid { ColumnSpacing = 10, Padding = new Thickness(12, 10, 16, 6), ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
            head.Add(back, 0);
            head.Add(titleLabel, 1);
            if (Theme.CuteMode)
            {
                head.ColumnDefinitions.Add(new(GridLength.Auto));
                head.Add(new Label { Text = Cute.Set.PageDeco, FontSize = 26, VerticalOptions = LayoutOptions.Center }, 2);
            }
            var grid = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
            grid.Add(head, 0, 0);
            grid.Add(value, 0, 1);
            Content = grid;
        }
    }
}
