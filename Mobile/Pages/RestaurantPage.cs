using food.Models;
using food.Services;
using static food.Mobile.State;

namespace food.Mobile;

/// <summary>
/// 新增 / 編輯餐廳（對應電腦版右側的表單）：名稱、國家 › 縣市 › 地區、地址（開地圖）、電話（撥打）、評分、備註。
/// </summary>
public class RestaurantPage : SubPage
{
    const string NoneText = "不指定";

    readonly Restaurant? editing;
    readonly Entry name = new Entry { Placeholder = "例如：阿明牛肉麵" }.Styled();
    readonly Entry address = new Entry { Placeholder = "地址（可空白）" }.Styled();
    readonly Entry phone = new Entry { Placeholder = "電話（可空白）", Keyboard = Keyboard.Telephone }.Styled();
    readonly Editor note = new Editor { Placeholder = "想點的菜、營業時間、停車…", AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 96 }.Styled();
    readonly Picker country = NewPicker("國家"), city = NewPicker("縣市"), district = NewPicker("地區");
    readonly Label noCategory = Ui.L("還沒有任何國家，請先按下方「＋ 新增分類」新增一個國家。", 13, Theme.Danger);
    readonly HorizontalStackLayout stars = new() { Spacing = 2 };
    readonly Label ratingLabel = Ui.L1("", 13, Theme.Muted);
    int rating;

    // 三個下拉選單目前的選項（縣市、地區的第一項是 null =「不指定」）
    List<Category> countries = [];
    List<Category?> cities = [], districts = [];
    bool filling;

    /// <param name="restaurant">null = 新增（分類預設為餐廳清單目前看的分類）</param>
    public RestaurantPage(Restaurant? restaurant) : base(restaurant == null ? "新增餐廳" : "編輯餐廳")
    {
        editing = restaurant;

        country.SelectedIndexChanged += (_, _) => { if (!filling) { filling = true; FillCities(null); FillDistricts(null); filling = false; } };
        city.SelectedIndexChanged += (_, _) => { if (!filling) { filling = true; FillDistricts(null); filling = false; } };

        for (int i = 1; i <= 5; i++)
        {
            int value = i;
            var star = new Label { Text = "★", FontSize = 34, Padding = new Thickness(2, 0) };
            // 點第幾顆就是幾分；再點一次目前的分數則清除評分
            star.OnTap(() => { SetRating(value == rating ? 0 : value); return Task.CompletedTask; }, guard: false);
            stars.Add(star);
        }
        ratingLabel.VerticalOptions = LayoutOptions.Center;
        ratingLabel.Margin = new Thickness(8, 0, 0, 0);
        stars.Add(ratingLabel);
        if (Application.Current is { } app) app.RequestedThemeChanged += (_, _) => SetRating(rating);

        var mapBtn = Ui.Btn("🗺 地圖", Ui.Kind.Soft, () => Ops.OpenMap(name.Text ?? "", address.Text ?? ""), 48);
        var callBtn = Ui.Btn("📞 撥打", Ui.Kind.Soft, () => Ops.Call(phone.Text ?? ""), 48);

        var form = new VerticalStackLayout
        {
            Spacing = 0, Padding = new Thickness(16, 4, 16, 32),
            Children =
            {
                Caption("餐廳名稱"), Dialogs.InputBox(name),
                Caption("分類（國家 › 縣市 › 地區）"),
                new VerticalStackLayout { Spacing = 8, Children = { noCategory, Dialogs.InputBox(country), Dialogs.InputBox(city), Dialogs.InputBox(district) } },
                Small(Ui.Btn("＋ 新增分類", Ui.Kind.Ghost, AddCategory, 36)),
                Caption("地址"), WithButton(Dialogs.InputBox(address), mapBtn),
                Caption("電話"), WithButton(Dialogs.InputBox(phone), callBtn),
                Caption("評分"), stars,
                Caption("備註"), Dialogs.InputBox(note, height: 0),
            },
        };
        if (editing != null)
            form.Add(Ui.L($"收藏於 {editing.CreatedAt:yyyy/MM/dd}", 12, Theme.Subtle).Also(l => l.Margin = new Thickness(2, 12, 0, 0)));

        // ---- 底部按鈕 ----
        var bottom = new Grid { ColumnSpacing = 10, Padding = new Thickness(16, 8, 16, 14) };
        bottom.SetAppThemeColor(BackgroundColorProperty, Theme.Bg.Light, Theme.Bg.Dark);
        if (editing != null)
        {
            bottom.ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star)];
            bottom.Add(Ui.Btn("刪除", Ui.Kind.Danger, Delete, 48), 0);
            bottom.Add(Ui.Btn("儲存", Ui.Kind.Accent, SaveForm, 48), 1);
        }
        else bottom.Add(Ui.Btn("新增", Ui.Kind.Accent, SaveForm, 48));

        var page = new Grid { RowDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        page.Add(new ScrollView { Content = form }, 0, 0);
        page.Add(bottom, 0, 1);
        Body = page;

        // ---- 填入資料 ----
        if (editing != null)
        {
            name.Text = editing.Name;
            address.Text = editing.Address;
            phone.Text = editing.Phone;
            note.Text = editing.Note;
        }
        SetRating(editing?.Rating ?? 0);
        SetCategory(editing?.CategoryId ?? SelCategory);
        if (editing == null) Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () => name.Focus());
    }

    static Picker NewPicker(string title)
    {
        var p = new Picker { Title = title, FontSize = 15 };
        p.SetAppThemeColor(Picker.TextColorProperty, Theme.Text.Light, Theme.Text.Dark);
        p.SetAppThemeColor(Picker.TitleColorProperty, Theme.Subtle.Light, Theme.Subtle.Dark);
        return p;
    }

    static Label Caption(string text) => new Label
    {
        Text = text, FontSize = 13, FontAttributes = FontAttributes.Bold, Margin = new Thickness(2, 16, 0, 6),
    }.Fg(Theme.Muted);

    static View Small(Button b)
    {
        b.HorizontalOptions = LayoutOptions.Start;
        b.Margin = new Thickness(0, 8, 0, 0);
        b.FontSize = 13;
        return b;
    }

    static View WithButton(View input, Button button)
    {
        var grid = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(input, 0);
        grid.Add(button, 1);
        return grid;
    }

    // ---------- 評分：五顆星 ----------

    void SetRating(int value)
    {
        rating = Math.Clamp(value, 0, 5);
        for (int i = 0; i < 5; i++)
            ((Label)stars[i]).Fg(i < rating ? Theme.Star : Theme.StarEmpty);
        ratingLabel.Text = rating == 0 ? RatingWords[0] : $"{rating} 分 · {RatingWords[rating]}";
    }

    // ---------- 分類：國家 › 縣市 › 地區 三個連動的下拉選單 ----------

    /// <summary>把三個下拉選單設成這個分類的路徑；沒有指定時選第一個國家。</summary>
    void SetCategory(string? categoryId)
    {
        var path = CategoryService.PathOf(Data, categoryId).Select(c => c.Id).ToList();
        string? At(int i) => i < path.Count ? path[i] : null;
        filling = true;
        countries = [.. CategoryService.Children(Data, null)];
        country.ItemsSource = countries.Select(c => c.Name).ToList();
        country.SelectedIndex = countries.Count == 0 ? -1 : Math.Max(countries.FindIndex(c => c.Id == At(0)), 0);
        country.IsEnabled = countries.Count > 0;
        noCategory.IsVisible = countries.Count == 0;
        FillCities(At(1));
        FillDistricts(At(2));
        filling = false;
    }

    void FillCities(string? select)
    {
        var parent = country.SelectedIndex >= 0 && country.SelectedIndex < countries.Count ? countries[country.SelectedIndex].Id : null;
        cities = Options(parent);
        Fill(city, cities, select);
    }

    void FillDistricts(string? select)
    {
        var parent = city.SelectedIndex > 0 && city.SelectedIndex < cities.Count ? cities[city.SelectedIndex]?.Id : null;
        districts = Options(parent);
        Fill(district, districts, select);
    }

    static List<Category?> Options(string? parentId) =>
        [null, .. parentId == null ? [] : CategoryService.Children(Data, parentId)];

    static void Fill(Picker picker, List<Category?> options, string? select)
    {
        picker.ItemsSource = options.Select(c => c?.Name ?? NoneText).ToList();
        picker.SelectedIndex = Math.Max(options.FindIndex(c => c?.Id == select), 0);
        picker.IsEnabled = options.Count > 1;
        picker.Opacity = picker.IsEnabled ? 1 : 0.55;
    }

    /// <summary>選到的最深一層分類；連國家都沒有時為 null。</summary>
    string? FormCategoryId
    {
        get
        {
            if (district.SelectedIndex > 0 && district.SelectedIndex < districts.Count) return districts[district.SelectedIndex]!.Id;
            if (city.SelectedIndex > 0 && city.SelectedIndex < cities.Count) return cities[city.SelectedIndex]!.Id;
            return country.SelectedIndex >= 0 && country.SelectedIndex < countries.Count ? countries[country.SelectedIndex].Id : null;
        }
    }

    /// <summary>不用離開這個頁面就能新增國家、或在目前選的國家 / 縣市底下新增一層。</summary>
    async Task AddCategory()
    {
        var current = FormCategoryId;
        var parents = new List<string?> { null };
        var options = new List<string> { "新增國家" };
        foreach (var c in CategoryService.PathOf(Data, current))
        {
            int level = CategoryService.Level(Data, c.Id);
            if (level >= CategoryService.MaxLevel) continue;
            parents.Add(c.Id);
            options.Add($"在「{c.Name}」底下新增{CategoryService.LevelName(level + 1)}");
        }
        var act = await Dialogs.Menu("新增分類", [.. options]);
        if (act == null) return;
        if (await Ops.AddCategory(parents[options.IndexOf(act)]) is { } added) SetCategory(added.Id);
    }

    // ---------- 儲存 / 刪除 ----------

    async Task SaveForm()
    {
        var n = (name.Text ?? "").Trim();
        if (n.Length == 0)
        {
            await Dialogs.Info("提示", "請輸入餐廳名稱。");
            name.Focus();
            return;
        }
        if (FormCategoryId is not { } categoryId)
        {
            await Dialogs.Info("提示", "請先新增一個國家，再選擇餐廳所在的分類。");
            return;
        }
        var r = editing ?? new Restaurant();
        r.Name = n;
        r.CategoryId = categoryId;
        r.Address = (address.Text ?? "").Trim();
        r.Phone = (phone.Text ?? "").Trim();
        r.Rating = rating;
        r.Note = (note.Text ?? "").Trim();
        if (editing == null)
        {
            Data.Restaurants.Add(r);
            // 存到別的分類時，清單切到那個分類，才看得到剛新增的這間
            if (!CategoryService.RestaurantsUnder(Data, SelCategory).Contains(r)) SelCategory = r.CategoryId;
        }
        Save();
        await Navigation.PopAsync();
    }

    async Task Delete()
    {
        if (editing == null || !await Ops.DeleteRestaurant(editing)) return;
        await Navigation.PopAsync();
    }
}

static class ViewExtensions
{
    public static T Also<T>(this T view, Action<T> action)
    {
        action(view);
        return view;
    }
}
