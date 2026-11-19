namespace food.Mobile;

/// <summary>建立介面元件的小工具。版面全部用程式碼組成（沒有 XAML），配色見 <see cref="Theme"/>。</summary>
public static class Ui
{
    public enum Kind { Accent, Ghost, Outline, Danger, DangerSolid, Soft, OnHero }

    public static Label L(string text = "", double size = 14, Pair? color = null, bool bold = false) =>
        new Label
        {
            Text = text, FontSize = size, LineBreakMode = LineBreakMode.WordWrap,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        }.Fg(color ?? Theme.Text);

    /// <summary>單行、超出以 … 截斷的文字。</summary>
    public static Label L1(string text = "", double size = 14, Pair? color = null, bool bold = false)
    {
        var l = L(text, size, color, bold);
        l.LineBreakMode = LineBreakMode.TailTruncation;
        l.MaxLines = 1;
        return l;
    }

    public static Border Card(View content, double pad = 14) =>
        new Border { Content = content, Padding = pad, StrokeShape = Theme.Round(14), StrokeThickness = 1 }
            .Fill(Theme.Surface).Stroke_(Theme.Line);

    public static Border HeroCard(View content) =>
        new Border { Content = content, Padding = new Thickness(18, 16), StrokeShape = Theme.Round(18), StrokeThickness = 0 }.Hero();

    public static Border Pill(string text, Pair fg, Pair bg) =>
        new Border
        {
            Content = L1(text, 11, fg, bold: true), Padding = new Thickness(8, 2), StrokeThickness = 0,
            StrokeShape = Theme.Round(10), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start,
        }.Fill(bg);

    /// <param name="guard">false：對話框裡的按鈕（外層操作還在等對話框結果，不能被「同時只做一件事」擋掉）</param>
    public static Button Btn(string text, Kind kind = Kind.Ghost, Func<Task>? click = null, double height = 42, bool guard = true)
    {
        var b = new Button
        {
            Text = text, FontSize = 14, CornerRadius = 10, HeightRequest = height, Padding = new Thickness(14, 0),
            FontAttributes = kind is Kind.Accent or Kind.DangerSolid ? FontAttributes.Bold : FontAttributes.None,
            BorderWidth = kind == Kind.Outline ? 1 : 0,
        };
        (Pair bg, Pair fg) = kind switch
        {
            Kind.Accent => (Theme.Accent, Theme.OnAccent),
            Kind.Outline => (Theme.Surface, Theme.Text),
            Kind.Danger => (Theme.DangerSoft, Theme.Danger),
            Kind.DangerSolid => (Theme.Danger, Theme.OnAccent),
            Kind.Soft => (Theme.AccentSoft, Theme.Accent),
            Kind.OnHero => (new Pair(Color.FromArgb("#33FFFFFF"), Color.FromArgb("#33FFFFFF")), Theme.OnHero),
            _ => (Theme.Ghost, Theme.Text),
        };
        b.SetAppThemeColor(Button.BackgroundColorProperty, bg.Light, bg.Dark);
        b.SetAppThemeColor(Button.TextColorProperty, fg.Light, fg.Dark);
        b.SetAppThemeColor(Button.BorderColorProperty, Theme.Line.Light, Theme.Line.Dark);
        if (click != null) b.Clicked += (_, _) => { if (guard) Run(click); else Do(click); };
        return b;
    }

    /// <summary>右下角的圓形「+」按鈕。</summary>
    public static Button Fab(Func<Task> click)
    {
        var b = Btn("+", Kind.Accent, click, 58);
        b.WidthRequest = 58;
        b.CornerRadius = 29;
        b.FontSize = 28;
        b.Padding = new Thickness(0, 0, 0, 4);
        b.HorizontalOptions = LayoutOptions.End;
        b.VerticalOptions = LayoutOptions.End;
        b.Margin = new Thickness(0, 0, 18, 18);
        // 不加 Shadow：Android 上按下後陰影圖層會畫成整頁大小，讓畫面變灰
        return b;
    }

    /// <summary>只能同時進行一個操作（對話框還開著時，其他按鈕不會再開第二個）；發生錯誤時顯示訊息。</summary>
    static bool busy;

    public static async void Run(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        try { await action(); }
        catch (Exception ex) { await Dialogs.Error("發生錯誤", ex.Message); }
        finally { busy = false; }
    }

    /// <summary>不受 busy 限制的版本，給對話框內部使用。</summary>
    public static async void Do(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { await Dialogs.Error("發生錯誤", ex.Message); }
    }

    public static T OnTap<T>(this T view, Func<Task> action, bool guard = true) where T : View
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { if (guard) Run(action); else Do(action); };
        view.GestureRecognizers.Add(tap);
        return view;
    }

    /// <summary>分段選擇（例如 跟隨系統/淺色/深色）。回傳元件與設定目前選項的方法。</summary>
    public static (View View, Action<int> Select) Segment(string[] options, int selected, Action<int> changed)
    {
        var grid = new Grid { ColumnSpacing = 4, Padding = 4 };
        var buttons = new List<Button>();
        void Paint(int sel)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                var (bg, fg) = i == sel ? (Theme.Surface, Theme.Text) : (Theme.Ghost, Theme.Muted);
                b.SetAppThemeColor(Button.BackgroundColorProperty, bg.Light, bg.Dark);
                b.SetAppThemeColor(Button.TextColorProperty, fg.Light, fg.Dark);
                b.FontAttributes = i == sel ? FontAttributes.Bold : FontAttributes.None;
            }
        }
        for (int i = 0; i < options.Length; i++)
        {
            int k = i;
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var b = new Button { Text = options[i], FontSize = 14, CornerRadius = 8, HeightRequest = 36, Padding = 0 };
            b.Clicked += (_, _) => { Paint(k); changed(k); };
            buttons.Add(b);
            grid.Add(b, i);
        }
        Paint(selected);
        var box = new Border { Content = grid, StrokeThickness = 0, StrokeShape = Theme.Round(11) }.Fill(Theme.Ghost);
        return (box, Paint);
    }

    public static Label Section(string text) => new Label
    {
        Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, Margin = new Thickness(4, 12, 0, 2),
    }.Fg(Theme.Muted);

    /// <summary>頁面底色。</summary>
    public static T Page<T>(this T page, string title) where T : ContentPage
    {
        page.Title = title;
        page.SetAppThemeColor(VisualElement.BackgroundColorProperty, Theme.Bg.Light, Theme.Bg.Dark);
        return page;
    }

    /// <summary>左邊圓角小方塊圖示（清單列、選項用）。</summary>
    public static Border IconBox(string icon, Pair fg, Pair bg, double size = 36) => new Border
    {
        WidthRequest = size, HeightRequest = size, StrokeThickness = 0, StrokeShape = Theme.Round(size * 0.28), VerticalOptions = LayoutOptions.Center,
        Content = new Label { Text = icon, FontSize = size * 0.45, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }.Fg(fg),
    }.Fill(bg);

    /// <summary>星星評分的文字（★★★☆☆）。</summary>
    public static string Stars(int rating) => new string('★', Math.Clamp(rating, 0, 5)) + new string('☆', 5 - Math.Clamp(rating, 0, 5));
}
