using food.Models;
using food.Services;
using static food.Mobile.State;

namespace food.Mobile;

/// <summary>清單的一列（顏色依目前深色/淺色預先算好）。</summary>
public record RestItem(Restaurant Model, string Name, string Initial, Brush Avatar, string Path, string Detail, bool HasDetail,
                       string Note, bool HasNote, string Rating, bool HasRating, Color RatingFg, Color RatingBg);

/// <summary>餐廳：目前看的分類（大卡片，點一下換分類）、搜尋、餐廳清單、右下角「+」新增。</summary>
public class RestaurantsPage : TabPage
{
    readonly CollectionView list = new() { SelectionMode = SelectionMode.None };
    readonly Label subText = Ui.L("", 13, Theme.Muted);
    readonly Label heroLabel = Ui.L1("", 13, Theme.OnHeroMuted);
    readonly Label heroPath = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap }.Fg(Theme.OnHero);
    readonly Label heroCount = Ui.L("", 13, Theme.OnHeroMuted);
    readonly Button allBtn;
    readonly Entry search = new Entry { Placeholder = "搜尋名稱、地址、電話、備註", ClearButtonVisibility = ClearButtonVisibility.WhileEditing }.Styled();
    readonly Label countText = Ui.L1("", 13, Theme.Muted);
    readonly Label emptyText = new Label { HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(20, 40), FontSize = 14 }.Fg(Theme.Subtle);

    public RestaurantsPage() : base("餐廳")
    {
        // ---- 大卡片：目前看的分類 ----
        allBtn = Ui.Btn("看全部", Ui.Kind.OnHero, () => { SelCategory = null; Refresh(); return Task.CompletedTask; }, 34);
        allBtn.HorizontalOptions = LayoutOptions.Start;
        var heroGrid = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        heroGrid.Add(new VerticalStackLayout { Spacing = 2, Children = { heroLabel, heroPath, heroCount } }, 0);
        heroGrid.Add(new Label { Text = "▾", FontSize = 18, VerticalOptions = LayoutOptions.Start }.Fg(Theme.OnHero), 1);
        var hero = Ui.HeroCard(new VerticalStackLayout { Spacing = 10, Children = { heroGrid, allBtn } })
            .OnTap(async () => { if (await Ops.PickCategory("要看哪個分類的餐廳？", SelCategory, allowAll: true) is { } pick) { SelCategory = pick.Id; Refresh(); } });

        // ---- 搜尋 ----
        search.TextChanged += (_, _) => RefreshList();
        var searchGrid = new Grid { ColumnSpacing = 6, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        searchGrid.Add(new Label { Text = "🔍", FontSize = 15, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(4, 0, 0, 0) }, 0);
        searchGrid.Add(search, 1);

        list.Header = new VerticalStackLayout
        {
            Spacing = 12, Padding = new Thickness(16, 16, 16, 4),
            Children = { Header("餐廳收藏", 0, subText), hero, Dialogs.InputBox(searchGrid), countText },
        };
        list.Footer = new BoxView { HeightRequest = 90, Color = Colors.Transparent };
        list.EmptyView = emptyText;
        list.ItemTemplate = new DataTemplate(Row);

        Content = new Grid { Children = { list, Ui.Fab(() => Navigation.PushAsync(new RestaurantPage(null))) } };
    }

    View Row()
    {
        // 左：名稱第一個字的彩色頭像（可愛主題是粉嫩底色加食物圖案）
        var initial = new Label { FontSize = Theme.CuteMode ? 24 : 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        initial.SetBinding(Label.TextProperty, static (RestItem i) => i.Initial);
        var avatar = new Border { WidthRequest = 44, HeightRequest = 44, StrokeThickness = 0, StrokeShape = Theme.Round(13), VerticalOptions = LayoutOptions.Start, Content = initial };
        avatar.SetBinding(Border.BackgroundProperty, static (RestItem i) => i.Avatar);

        var name = Ui.L1("", 16, bold: true);
        name.SetBinding(Label.TextProperty, static (RestItem i) => i.Name);
        var ratingText = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
        ratingText.SetBinding(Label.TextProperty, static (RestItem i) => i.Rating);
        ratingText.SetBinding(Label.TextColorProperty, static (RestItem i) => i.RatingFg);
        var rating = new Border { Content = ratingText, Padding = new Thickness(8, 2), StrokeThickness = 0, StrokeShape = Theme.Round(10), VerticalOptions = LayoutOptions.Center };
        rating.SetBinding(Border.BackgroundColorProperty, static (RestItem i) => i.RatingBg);
        rating.SetBinding(IsVisibleProperty, static (RestItem i) => i.HasRating);
        var line1 = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        line1.Add(name, 0);
        line1.Add(rating, 1);

        var path = Ui.L1("", 12, Theme.Accent);
        path.SetBinding(Label.TextProperty, static (RestItem i) => i.Path);
        var detail = Ui.L1("", 13, Theme.Muted);
        detail.SetBinding(Label.TextProperty, static (RestItem i) => i.Detail);
        detail.SetBinding(IsVisibleProperty, static (RestItem i) => i.HasDetail);
        var note = Ui.L("", 13, Theme.Subtle);
        note.MaxLines = 2;
        note.LineBreakMode = LineBreakMode.TailTruncation;
        note.SetBinding(Label.TextProperty, static (RestItem i) => i.Note);
        note.SetBinding(IsVisibleProperty, static (RestItem i) => i.HasNote);

        var grid = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        grid.Add(avatar, 0);
        grid.Add(new VerticalStackLayout { Spacing = 3, Children = { line1, path, detail, note } }, 1);
        var card = new Border { Content = grid, Padding = new Thickness(14, 12), Margin = new Thickness(16, 0, 16, 8), StrokeThickness = 1, StrokeShape = Theme.Round(14) }
            .Fill(Theme.Surface).Stroke_(Theme.Line);
        card.OnTap(async () => { if (card.BindingContext is RestItem it) await ItemMenu(it.Model); });
        return card;
    }

    async Task ItemMenu(Restaurant r)
    {
        var call = r.Phone.Trim() != "" ? $"撥打 {r.Phone.Trim()}" : null;
        var actions = new List<string> { "查看 / 編輯", "開啟 Google 地圖" };
        if (call != null) actions.Add(call);
        actions.Add("刪除");
        var act = await Dialogs.Menu(r.Name, [.. actions]);
        if (act == "查看 / 編輯") await Navigation.PushAsync(new RestaurantPage(r));
        else if (act == "開啟 Google 地圖") await Ops.OpenMap(r.Name, r.Address);
        else if (act == call && call != null) await Ops.Call(r.Phone);
        else if (act == "刪除") await Ops.DeleteRestaurant(r);
    }

    public override void Refresh()
    {
        subText.Text = $"{Data.Restaurants.Count} 間餐廳 · {CategoryService.Children(Data, null).Count()} 個國家 · {Data.Categories.Count} 個分類";
        heroLabel.Text = SelCategory == null ? "目前顯示" : $"目前顯示 · {LevelName(SelCategory)}";
        heroPath.Text = PathText(SelCategory);
        heroCount.Text = $"{CategoryService.RestaurantsUnder(Data, SelCategory).Count()} 間餐廳 · 點這裡換分類";
        allBtn.IsVisible = SelCategory != null;
        RefreshList();
    }

    /// <summary>清單只顯示目前分類（含子分類）裡、符合搜尋文字的餐廳，新加的在上面（與電腦版相同）。</summary>
    void RefreshList()
    {
        var keyword = (search.Text ?? "").Trim();
        var rows = CategoryService.RestaurantsUnder(Data, SelCategory)
            .Select(r => (Model: r, Path: CategoryService.PathText(Data, r.CategoryId)))
            .Where(x => keyword.Length == 0 ||
                new[] { x.Model.Name, x.Model.Address, x.Model.Phone, x.Model.Note, x.Path }
                    .Any(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.Model.CreatedAt)
            .Select(x => Item(x.Model, x.Path))
            .ToList();
        list.ItemsSource = rows;
        countText.Text = rows.Count == 0 ? "" : $"共 {rows.Count} 間 · 點一間可以查看、編輯、開地圖或撥電話";
        emptyText.Text = Data.Categories.Count == 0
            ? Cute.Empty("還沒有任何分類。\n先到「分類」分頁新增一個國家吧！", 0)
            : keyword.Length > 0
                ? Cute.Empty("找不到符合的餐廳。", 2)
                : Cute.Empty("這裡還沒有餐廳。\n按右下角的「+」新增一間。", 1);
    }

    static RestItem Item(Restaurant r, string path)
    {
        var detail = string.Join(" · ", new[] { r.Address, r.Phone }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new RestItem(r, r.Name, Theme.Initial(r.Name, r.Note), Theme.Avatar(r.Name), path == "" ? "未分類" : path,
            detail, detail != "", r.Note.Trim(), r.Note.Trim() != "", Ui.RatingTag(r.Rating), r.Rating > 0, Theme.Star.Now, Theme.StarSoft.Now);
    }
}
