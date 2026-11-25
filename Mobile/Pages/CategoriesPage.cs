using food.Models;
using food.Services;
using static food.Mobile.State;

namespace food.Mobile;

/// <summary>
/// 分類：國家 › 縣市 › 地區 的樹狀清單（對應電腦版左側）。點箭頭展開或收合；
/// 點分類可以查看這裡的餐廳、新增下一層、重新命名或刪除。
/// </summary>
public class CategoriesPage : TabPage
{
    readonly VerticalStackLayout tree = new() { Spacing = 6 };
    readonly Label subText = Ui.L("", 13, Theme.Muted);
    readonly Label emptyText = new Label { Text = "還沒有任何分類。\n按上面的「＋ 新增國家」開始吧！", HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(20, 30), FontSize = 14 }.Fg(Theme.Subtle);

    public CategoriesPage() : base("分類")
    {
        var add = Ui.Btn("＋ 新增國家", Ui.Kind.Accent, async () => { if (await Ops.AddCategory(null) != null) Refresh(); }, 40);
        var top = Header("分類", 1, subText, add);
        emptyText.Text = Cute.Empty(emptyText.Text, 0);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 12, Padding = new Thickness(16, 16, 16, 32),
                Children =
                {
                    top,
                    tree,
                    emptyText,
                    Ui.L("點分類可以查看這裡的餐廳、新增下一層、重新命名或刪除；點左邊的箭頭展開或收合。\n" +
                         "刪除分類時，底下的子分類一起刪除，餐廳移到上一層；刪除國家時，底下的餐廳也會一起刪除。", 12, Theme.Subtle),
                },
            },
        };
    }

    public override void Refresh()
    {
        subText.Text = $"{CategoryService.Children(Data, null).Count()} 個國家 · {Data.Categories.Count} 個分類 · 國家 › 縣市 › 地區";
        tree.Children.Clear();
        void Walk(Category c, int level)
        {
            var children = CategoryService.Children(Data, c.Id).ToList();
            bool open = Expanded.Contains(c.Id);
            tree.Add(Row(c, level, children.Count > 0, open));
            if (open) foreach (var child in children) Walk(child, level + 1);
        }
        foreach (var country in CategoryService.Children(Data, null).ToList()) Walk(country, 0);
        emptyText.IsVisible = Data.Categories.Count == 0;
    }

    View Row(Category c, int level, bool hasChildren, bool open)
    {
        var arrow = new Label
        {
            Text = hasChildren ? (open ? "▾" : "▸") : "", FontSize = 18, WidthRequest = 30, HeightRequest = 40,
            HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center,
        }.Fg(Theme.Muted);
        if (hasChildren)
            arrow.OnTap(() =>
            {
                if (!Expanded.Remove(c.Id)) Expanded.Add(c.Id);
                Refresh();
                return Task.CompletedTask;
            });

        int count = CategoryService.RestaurantsUnder(Data, c.Id).Count();
        var grid = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(arrow, 0);
        grid.Add(Ui.IconBox(LevelIcon(level), Theme.Accent, level == 0 ? Theme.AccentSoft : Theme.Ghost, 32), 1);
        grid.Add(new VerticalStackLayout
        {
            Spacing = 1, VerticalOptions = LayoutOptions.Center,
            Children = { Ui.L1(c.Name, 15, bold: level == 0), Ui.L1(CategoryService.LevelName(level), 11, Theme.Subtle) },
        }, 2);
        grid.Add(Ui.Pill($"{count} 間", count > 0 ? Theme.Accent : Theme.Subtle, count > 0 ? Theme.AccentSoft : Theme.Ghost), 3);

        var card = new Border
        {
            Content = grid, Padding = new Thickness(4, 6, 12, 6), Margin = new Thickness(level * 20, 0, 0, 0),
            StrokeThickness = 1, StrokeShape = Theme.Round(12),
        }.Fill(Theme.Surface).Stroke_(Theme.Line);
        return card.OnTap(() => Menu(c, level));
    }

    async Task Menu(Category c, int level)
    {
        const string View = "查看這裡的餐廳", Rename = "重新命名", Delete = "刪除";
        var addChild = level < CategoryService.MaxLevel ? "新增" + CategoryService.LevelName(level + 1) : null;
        var actions = new List<string> { View };
        if (addChild != null) actions.Add(addChild);
        actions.Add(Rename);
        actions.Add(Delete);
        var act = await Dialogs.Menu($"{c.Name}（{CategoryService.LevelName(level)}）", [.. actions]);
        if (act == View)
        {
            SelCategory = c.Id;
            await Shell.Current.GoToAsync("//restaurants");
        }
        else if (act == addChild && addChild != null)
        {
            if (await Ops.AddCategory(c.Id) != null) Refresh();
        }
        else if (act == Rename) await Ops.RenameCategory(c.Id);
        else if (act == Delete) await Ops.DeleteCategory(c.Id);
    }
}
