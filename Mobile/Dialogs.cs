namespace food.Mobile;

/// <param name="Options">有值時改用下拉選單，值為選到的那一項（Initial 為預設選項）</param>
public record Field(string Label, string Initial = "", string Hint = "", string[]? Options = null);

/// <summary>
/// 統一風格的訊息 / 確認 / 輸入對話框（對應電腦版的 MessageBox、InputDialog）。
/// 對話框是蓋在目前畫面上的半透明頁面，按返回鍵等於取消。
/// </summary>
public static class Dialogs
{
    public static Task Info(string title, string message) => Alert("i", (Theme.Accent, Theme.AccentSoft), title, message);
    public static Task Success(string title, string message) => Alert("✓", (Theme.Accent, Theme.AccentSoft), title, message);
    public static Task Error(string title, string message) => Alert("!", (Theme.Danger, Theme.DangerSoft), title, message);

    static async Task Alert(string icon, (Pair, Pair) color, string title, string message)
    {
        var d = new DialogPage(icon, color, title, message);
        d.SetButtons(null, "知道了", false);
        await d.Show();
    }

    public static async Task<bool> Confirm(string title, string message, string okText = "確定", bool danger = false)
    {
        var d = new DialogPage(danger ? "!" : "?", danger ? (Theme.Danger, Theme.DangerSoft) : (Theme.Accent, Theme.AccentSoft), title, message);
        d.SetButtons("取消", okText, danger);
        return await d.Show() != null;
    }

    /// <param name="validate">回傳錯誤訊息則留在對話框；回傳 null 表示通過。</param>
    public static async Task<string[]?> Prompt(string title, string message, Field[] fields,
                                               Func<string[], string?>? validate = null, string okText = "確定", bool danger = false)
    {
        var d = new DialogPage(danger ? "!" : "✎", danger ? (Theme.Danger, Theme.DangerSoft) : (Theme.Accent, Theme.AccentSoft), title, message);
        d.AddFields(fields);
        d.Validate = validate;
        d.SetButtons("取消", okText, danger);
        return await d.Show();
    }

    /// <summary>顯示數個大型選項按鈕，回傳所選索引；取消回傳 null。</summary>
    /// <param name="indent">每個選項往右縮排的層數（分類樹用）；null 表示都不縮排</param>
    public static async Task<int?> Choose(string title, string message, (string Icon, string Title, string Description)[] options,
                                          int[]? indent = null)
    {
        var d = new DialogPage("?", (Theme.Accent, Theme.AccentSoft), title, message);
        d.AddChoices(options, indent);
        d.SetButtons("取消", null, false);
        var r = await d.Show();
        return r != null ? int.Parse(r[0]) : null;
    }

    /// <summary>精簡的動作選單（清單列的「編輯 / 刪除」等），回傳所選文字；取消回傳 null。</summary>
    public static async Task<string?> Menu(string title, params string[] actions)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return null;
        var r = await page.DisplayActionSheetAsync(title, "取消", null, actions);
        return actions.Contains(r) ? r : null;
    }

    /// <summary>有框的輸入欄（餐廳頁面也用）。</summary>
    public static Border InputBox(View input, double height = 48) => DialogPage.Box(input, height);

    /// <summary>套用輸入框的文字顏色。</summary>
    public static T Styled<T>(this T input) where T : InputView
    {
        input.FontSize = 15;
        input.SetAppThemeColor(InputView.TextColorProperty, Theme.Text.Light, Theme.Text.Dark);
        input.SetAppThemeColor(InputView.PlaceholderColorProperty, Theme.Subtle.Light, Theme.Subtle.Dark);
        return input;
    }

    sealed class DialogPage : ContentPage
    {
        readonly TaskCompletionSource<string[]?> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly VerticalStackLayout body = new() { Spacing = 0 };
        readonly Label error = Ui.L("", 13, Theme.Danger);
        readonly List<Func<string>> values = [];
        readonly HorizontalStackLayout buttons = new() { Spacing = 10, HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 18, 0, 0) };
        View? firstInput;
        public Func<string[], string?>? Validate;

        public DialogPage(string icon, (Pair Fg, Pair Bg) color, string title, string message)
        {
            BackgroundColor = Color.FromArgb("#73000000");
            Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
            var head = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
            // 可愛主題：對話框的小圖示換成圖案組的 emoji
            var iconBox = Ui.IconBox(Cute.DialogIcon(icon), color.Fg, color.Bg);
            ((Label)iconBox.Content!).FontAttributes = FontAttributes.Bold;
            head.Add(iconBox, 0);
            head.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }.Fg(Theme.Text), 1);
            body.Add(head);
            if (message != "") body.Add(new Label { Text = message, FontSize = 14, LineHeight = 1.3, Margin = new Thickness(0, 12, 0, 0) }.Fg(Theme.Muted));
            error.IsVisible = false;
            error.Margin = new Thickness(0, 12, 0, 0);

            var card = new Border
            {
                StrokeThickness = 0, StrokeShape = Theme.Round(18), Padding = new Thickness(20, 20, 20, 16),
                Content = new VerticalStackLayout { Children = { body, error, buttons } },
                MaximumWidthRequest = 520, Margin = new Thickness(16, 24),
                VerticalOptions = LayoutOptions.Center,
            }.Fill(Theme.Surface);
            Content = new ScrollView { Content = new Grid { Children = { card }, MinimumHeightRequest = 200 }, VerticalOptions = LayoutOptions.Center };
        }

        public void SetButtons(string? cancel, string? ok, bool danger)
        {
            if (cancel != null) buttons.Add(Ui.Btn(cancel, Ui.Kind.Ghost, () => Close(null), guard: false));
            if (ok != null) buttons.Add(Ui.Btn(ok, danger ? Ui.Kind.DangerSolid : Ui.Kind.Accent, OnOk, guard: false));
        }

        Task OnOk()
        {
            var vals = values.Select(v => v()).ToArray();
            var err = Validate?.Invoke(vals);
            if (err != null)
            {
                error.Text = err;
                error.IsVisible = true;
                return Task.CompletedTask;
            }
            return Close(vals);
        }

        public void AddFields(Field[] fields)
        {
            foreach (var f in fields)
            {
                body.Add(new Label { Text = f.Label, FontSize = 13, FontAttributes = FontAttributes.Bold, Margin = new Thickness(2, 14, 0, 6) }.Fg(Theme.Muted));
                if (f.Options != null)
                {
                    var picker = new Picker { Title = f.Label, ItemsSource = f.Options, FontSize = 15 };
                    picker.SetAppThemeColor(Picker.TextColorProperty, Theme.Text.Light, Theme.Text.Dark);
                    picker.SelectedIndex = Math.Max(Array.IndexOf(f.Options, f.Initial), 0);
                    values.Add(() => picker.SelectedItem as string ?? "");
                    body.Add(Box(picker));
                    continue;
                }
                var entry = new Entry { Text = f.Initial, Placeholder = f.Hint, ClearButtonVisibility = ClearButtonVisibility.WhileEditing }.Styled();
                values.Add(() => (entry.Text ?? "").Trim());
                if (firstInput == null) firstInput = entry;
                entry.Completed += (_, _) => Ui.Do(OnOk);
                body.Add(Box(entry));
            }
        }

        public static Border Box(View input, double height = 48)
        {
            var b = new Border { Content = input, StrokeThickness = 1, StrokeShape = Theme.Round(10), Padding = new Thickness(10, 0) }
                .Fill(Theme.InputBg).Stroke_(Theme.LineStrong);
            if (height > 0) b.HeightRequest = height;
            return b;
        }

        public void AddChoices((string Icon, string Title, string Description)[] options, int[]? indent)
        {
            for (int i = 0; i < options.Length; i++)
            {
                var (icon, head, desc) = options[i];
                var text = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children = { Ui.L(head, 15, bold: true) } };
                if (desc != "") text.Add(Ui.L(desc, 12, Theme.Muted));
                var grid = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
                if (icon != "") grid.Add(Ui.IconBox(icon, Theme.Accent, Theme.AccentSoft), 0);
                grid.Add(text, 1);
                var index = i.ToString();
                var level = indent != null && i < indent.Length ? indent[i] : 0;
                var card = new Border
                {
                    Content = grid, Padding = new Thickness(14, 10), Margin = new Thickness(level * 18, indent != null ? 8 : 12, 0, 0),
                    StrokeThickness = 1, StrokeShape = Theme.Round(12),
                }.Fill(Theme.Surface).Stroke_(Theme.Line).OnTap(() => Close([index]), guard: false);
                body.Add(card);
            }
        }

        public async Task<string[]?> Show()
        {
            var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
            if (nav == null) return null;
            await nav.PushModalAsync(this, false);
            if (firstInput != null)
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => firstInput.Focus());
            return await done.Task;
        }

        bool closing;

        async Task Close(string[]? result)
        {
            if (closing) return;
            closing = true;
            if (Navigation.ModalStack.LastOrDefault() == this) await Navigation.PopModalAsync(false);
            done.TrySetResult(result); // 完全關掉後才回傳，呼叫端接著開下一個對話框才不會撞在一起
        }

        protected override bool OnBackButtonPressed()
        {
            Ui.Do(() => Close(null));
            return true;
        }

        /// <summary>不是按按鈕關掉的（例如被系統關閉）才當作取消；按按鈕關的結果由 Close 設定。</summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (!closing) done.TrySetResult(null);
        }
    }
}
