using food.Models;
using food.Services;
using static food.Mobile.State;

namespace food.Mobile;

/// <summary>
/// 各頁共用的操作（對應電腦版 MainWindow 的 _Click），訊息與規則與電腦版相同。
/// 規則本身在共用的 CategoryService / ExcelService 裡，兩邊改規則時記得一起看。
/// </summary>
public static class Ops
{
    // ---------- 分類 ----------

    /// <summary>在 parentId 底下新增分類（null = 新增國家）；回傳新分類，取消回傳 null。</summary>
    public static async Task<Category?> AddCategory(string? parentId)
    {
        int level = parentId == null ? 0 : CategoryService.Level(Data, parentId) + 1;
        var levelName = CategoryService.LevelName(level);
        var where = parentId == null ? "" : $"位於：{CategoryService.PathText(Data, parentId)}";
        var v = await Dialogs.Prompt($"新增{levelName}", where, [new Field($"{levelName}名稱")], vals => NameMessage(parentId, vals[0]), "新增");
        if (v == null) return null;
        var category = CategoryService.Add(Data, parentId, v[0]);
        if (parentId != null) Expanded.Add(parentId);
        Save();
        return category;
    }

    public static async Task RenameCategory(string id)
    {
        var category = CategoryService.Get(Data, id)!;
        var levelName = LevelName(id);
        var v = await Dialogs.Prompt($"重新命名{levelName}", "", [new Field($"{levelName}名稱", category.Name)],
            vals => NameMessage(category.ParentId, vals[0], id), "儲存");
        if (v == null || v[0] == category.Name) return;
        CategoryService.Rename(Data, id, v[0]);
        Save();
    }

    public static async Task DeleteCategory(string id)
    {
        var category = CategoryService.Get(Data, id)!;
        var levelName = LevelName(id);
        var preview = CategoryService.Preview(Data, id);

        var message = $"確定要刪除{levelName}「{category.Name}」嗎？";
        if (preview.Categories > 1) message += $"\n\n底下 {preview.Categories - 1} 個子分類會一起刪除。";
        if (preview.MovedTo != null && preview.Moved > 0)
            message += $"\n底下 {preview.Moved} 間餐廳會移到「{preview.MovedTo.Name}」。";
        if (preview.Deleted > 0) message += $"\n\n⚠ 底下 {preview.Deleted} 間餐廳也會一起刪除！";
        if (!await Dialogs.Confirm($"刪除{levelName}", message, "刪除", danger: true)) return;

        bool viewing = SelCategory != null && CategoryService.DescendantIds(Data, id).Contains(SelCategory);
        var result = CategoryService.Delete(Data, id);
        if (viewing) SelCategory = result.MovedTo?.Id;
        Save();
    }

    /// <summary>選一個分類（餐廳清單換分類用）；取消回傳 null，選「全部餐廳」回傳 Id 為 null 的結果。</summary>
    public static async Task<CategoryPick?> PickCategory(string title, string? current, bool allowAll)
    {
        var ids = new List<string?>();
        var options = new List<(string, string, string)>();
        var indent = new List<int>();
        if (allowAll)
        {
            ids.Add(null);
            options.Add(("🍽", "全部餐廳", $"{Data.Restaurants.Count} 間" + (current == null ? " · 目前" : "")));
            indent.Add(0);
        }
        void Walk(Category c, int level)
        {
            ids.Add(c.Id);
            int count = CategoryService.RestaurantsUnder(Data, c.Id).Count();
            options.Add((LevelIcon(level), c.Name, $"{CategoryService.LevelName(level)} · {count} 間" + (c.Id == current ? " · 目前" : "")));
            indent.Add(level);
            foreach (var child in CategoryService.Children(Data, c.Id).ToList()) Walk(child, level + 1);
        }
        foreach (var country in CategoryService.Children(Data, null).ToList()) Walk(country, 0);
        if (ids.Count == (allowAll ? 1 : 0))
        {
            await Dialogs.Info(title, "還沒有任何分類。\n請到「分類」分頁新增一個國家。");
            return null;
        }
        var choice = await Dialogs.Choose(title, "", [.. options], [.. indent]);
        return choice is int i ? new CategoryPick(ids[i]) : null;
    }

    /// <param name="Id">null = 全部餐廳</param>
    public record CategoryPick(string? Id);

    // ---------- 餐廳 ----------

    /// <summary>刪除前確認；真的刪除回傳 true。</summary>
    public static async Task<bool> DeleteRestaurant(Restaurant r)
    {
        if (!await Dialogs.Confirm("刪除餐廳", $"確定要刪除「{r.Name}」嗎？", "刪除", danger: true)) return false;
        Data.Restaurants.Remove(r);
        Save();
        return true;
    }

    /// <summary>用 Google 地圖搜尋「名稱 地址」（有裝 Google 地圖 App 會直接用 App 開）。</summary>
    public static async Task OpenMap(string name, string address)
    {
        var query = $"{name.Trim()} {address.Trim()}".Trim();
        if (query.Length == 0)
        {
            await Dialogs.Info("地圖", "請先輸入餐廳名稱或地址。");
            return;
        }
        await Launcher.Default.OpenAsync("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query));
    }

    /// <summary>打開撥號畫面（不會直接撥出）。</summary>
    public static async Task Call(string phone)
    {
        phone = phone.Trim();
        if (phone.Length == 0)
        {
            await Dialogs.Info("撥打電話", "請先輸入電話。");
            return;
        }
        if (!PhoneDialer.Default.IsSupported)
        {
            await Dialogs.Info("撥打電話", "這支手機不能打電話。");
            return;
        }
        PhoneDialer.Default.Open(phone);
    }

    // ---------- Excel 匯出 / 匯入（格式與電腦版相同，共用 ExcelService） ----------

    public static async Task Export()
    {
        var name = $"餐廳收藏-{DateTime.Now:yyyyMMdd}.xlsx";
        var dir = Path.Combine(FileSystem.CacheDirectory, "export");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        await Task.Run(() => ExcelService.Export(Data, path));
        await Share.Default.RequestAsync(new ShareFileRequest { Title = name, File = new ShareFile(path) });
    }

    static readonly FilePickerFileType ExcelType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        // 有些檔案管理員把 .xlsx 標成 octet-stream，也讓它可以選；選到後再檢查副檔名
        [DevicePlatform.Android] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream"],
    });

    public static async Task Import()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "選擇 Excel 檔（.xlsx）", FileTypes = ExcelType });
        if (file == null) return;
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            await Dialogs.Error("匯入 Excel", "請選擇 Excel 活頁簿（.xlsx）。");
            return;
        }
        // 先複製到暫存資料夾，共用的 ExcelService 讀的是檔案路徑
        var dir = Path.Combine(FileSystem.CacheDirectory, "import");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "import.xlsx");
        await using (var src = await file.OpenReadAsync())
        await using (var dst = File.Create(path))
            await src.CopyToAsync(dst);

        List<ImportRow> rows;
        try { rows = await Task.Run(() => ExcelService.Import(path)); }
        catch (Exception ex)
        {
            await Dialogs.Error("匯入失敗", ex.Message);
            return;
        }
        if (rows.Count == 0)
        {
            await Dialogs.Info("匯入 Excel", "檔案裡沒有可以匯入的資料。\n第一列是標題，第一欄（國家）必須有值。");
            return;
        }

        int restaurants = rows.Count(r => r.Restaurant != null);
        var mode = await Dialogs.Choose("匯入 Excel", $"檔案裡有 {restaurants} 間餐廳。要怎麼匯入？",
        [
            ("＋", "加到目前的資料", "同一個分類裡已有同名的餐廳會略過"),
            ("⚠", "清空後改用檔案內容", "目前所有分類與餐廳都會刪除"),
        ]);
        if (mode == null) return;
        bool replace = mode == 1;
        if (replace && !await Dialogs.Confirm("匯入 Excel", "確定要清空目前所有分類與餐廳嗎？這個動作無法復原。", "清空並匯入", danger: true))
            return;

        int added = ExcelService.Apply(Data, rows, replace);
        foreach (var country in CategoryService.Children(Data, null)) Expanded.Add(country.Id);
        if (replace) SelCategory = null;
        Save();
        await Dialogs.Success("匯入完成", $"新增了 {added} 間餐廳。");
    }

    // ---------- 檢查更新 ----------

    public static async Task CheckUpdate(bool silent)
    {
        UpdateInfo? info;
        try { info = await Updater.CheckAsync(); }
        catch (Exception ex)
        {
            if (!silent) await Dialogs.Error("檢查更新", Updater.Explain(ex));
            return;
        }
        if (info == null)
        {
            if (!silent) await Dialogs.Success("檢查更新", $"目前已是最新版本（v{Updater.CurrentVersion}）。");
            return;
        }
        var notes = info.Notes.Length > 400 ? info.Notes[..400] : info.Notes;
        if (notes == "") notes = "（無說明）";
        var how = info.PackageUrl != null
            ? "按「下載」會用瀏覽器下載新版 APK，下載完點開它安裝即可（會直接覆蓋舊版，餐廳資料不會不見）。"
            : "按「下載」會打開下載頁面。";
        if (await Dialogs.Confirm("發現新版本", $"有新版本 {info.Version} 可用（目前 v{Updater.CurrentVersion}）。\n\n{notes}\n\n{how}", "下載"))
            await Launcher.Default.OpenAsync(info.PackageUrl ?? info.Url);
    }
}
