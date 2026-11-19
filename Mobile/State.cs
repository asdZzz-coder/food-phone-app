using food.Models;
using food.Services;

namespace food.Mobile;

/// <summary>
/// 各分頁共用的狀態（對應電腦版 MainWindow 的欄位）：全部資料、餐廳清單目前看的分類、分類清單展開中的分類。
/// 任何修改後呼叫 <see cref="Save"/>：存檔並通知開著的分頁重新整理。
/// </summary>
public static class State
{
    public static FoodData Data { get; private set; } = DataStore.Load();

    /// <summary>餐廳清單目前看的分類（含子分類）；null = 全部餐廳</summary>
    public static string? SelCategory;

    /// <summary>分類分頁裡展開中的分類</summary>
    public static readonly HashSet<string> Expanded = [.. CategoryService.Children(Data, null).Select(c => c.Id)];

    public static event Action? Changed;

    /// <summary>存檔並通知各分頁；存檔失敗時丟出例外，由 Ui.Run 顯示訊息。</summary>
    public static void Save()
    {
        if (CategoryService.Get(Data, SelCategory) == null) SelCategory = null; // 被刪掉了 → 回到全部
        try { DataStore.Save(Data); }
        finally { Changed?.Invoke(); }
    }

    /// <summary>重新讀取 data.json（匯入取代後用）。</summary>
    public static void Reload()
    {
        Data = DataStore.Load();
        SelCategory = null;
        Expanded.Clear();
        foreach (var c in CategoryService.Children(Data, null)) Expanded.Add(c.Id);
        Changed?.Invoke();
    }

    public static string PathText(string? id) => id == null ? "全部餐廳" : CategoryService.PathText(Data, id);

    public static string LevelName(string id) => CategoryService.LevelName(CategoryService.Level(Data, id));

    /// <summary>分類名稱檢查的訊息（與電腦版相同）；沒問題回傳 null。</summary>
    public static string? NameMessage(string? parentId, string name, string? renaming = null) =>
        CategoryService.Validate(Data, parentId, name, renaming) switch
        {
            CategoryNameError.Empty => "請輸入名稱。",
            CategoryNameError.TooLong => $"名稱最多 {CategoryService.MaxNameLength} 個字。",
            CategoryNameError.Duplicate => "這裡已經有同名的分類了。",
            _ => null,
        };

    /// <summary>國家、縣市、地區的圖示。</summary>
    public static string LevelIcon(int level) => level switch { 0 => "🌏", 1 => "🏙", _ => "📍" };

    public static readonly string[] RatingWords = ["尚未評分", "不推薦", "普通", "還不錯", "好吃", "超推薦！"];
}
