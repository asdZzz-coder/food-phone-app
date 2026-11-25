namespace food.Mobile;

/// <summary>
/// 可愛主題的圖案組（小動物、甜點、花園、海洋、星空、水果）：每組有自己的分頁小圖、標題裝飾、
/// 分類層級圖示、評分圖示、對話框圖示、空清單圖案，以及大卡片的漸層。選擇存在資料夾的 cute.txt。
/// 餐廳頭像在可愛主題改成依名稱猜出來的食物圖案（<see cref="Food"/>）。
/// emoji 在每支手機都看得到，不受螢幕色彩、護眼模式影響。
/// </summary>
public static class Cute
{
    /// <param name="Mascots">分頁右上角的小圖：餐廳、分類、更多</param>
    /// <param name="Levels">分類層級的圖示：國家、縣市、地區</param>
    /// <param name="Dialog">對話框圖示：資訊、成功、錯誤、詢問、輸入</param>
    /// <param name="Empty">空清單的圖案：還沒有分類、還沒有餐廳、找不到</param>
    public sealed record StickerSet(
        string Key, string Name, string Icon, string Description, Brush Hero,
        string Title, string Section, string HeroDeco, string PageDeco,
        string[] Mascots, string[] Levels, string Rating, string[] Dialog, string[] Empty);

    public static readonly StickerSet[] Sets =
    [
        new("animals", "小動物", "🐰", "🐰 🐻 🐱 🐶 🐥　小兔、小熊、小貓", Theme.Gradient("#FF9EC4", "#C9A8FF"),
            "🌸", "♡", "✨🐾", "🐾",
            ["🐰", "🐻", "🐱"], ["🐘", "🐶", "🐥"], "💗", ["🐰", "🎉", "🥺", "🤔", "✏️"], ["🐻", "🐰", "🙈"]),
        new("sweets", "甜點", "🍰", "🍰 🍩 🍮 🧁 🍪　蛋糕、甜甜圈、布丁", Theme.Gradient("#FF9EC4", "#FFC98F"),
            "🍬", "🍓", "🍭🧁", "🍰",
            ["🍰", "🍩", "🍮"], ["🎂", "🧁", "🍪"], "🍓", ["🍮", "🎂", "🥲", "🍬", "🖍️"], ["🍪", "🧁", "🍩"]),
        new("flowers", "花園", "🌷", "🌷 🌻 🌼 🌸 🌱　滿滿的小花和葉子", Theme.Gradient("#FF9EC4", "#A8E6C1"),
            "🌷", "✿", "🌸🌼", "🌷",
            ["🌻", "🌷", "🌼"], ["🌳", "🌿", "🌱"], "🌸", ["🌼", "💐", "🥀", "🌱", "✏️"], ["🌱", "🌷", "🍀"]),
        new("ocean", "海洋", "🐳", "🐳 🐙 🐬 🐠 🐚　鯨魚、章魚、海豚游來游去", Theme.Gradient("#FFB0CF", "#9FD4FF"),
            "🐚", "〜", "🐳💦", "🐠",
            ["🐳", "🐙", "🐬"], ["🏝️", "🐚", "💧"], "🐟", ["🐬", "🎉", "🦀", "🐙", "✏️"], ["🐢", "🐠", "🦀"]),
        new("space", "星空", "🌙", "🌙 🚀 👽 ⭐ 🪐　月亮、火箭和小外星人", Theme.Gradient("#D7A8FF", "#9FB4FF"),
            "⭐", "☆", "🌙✨", "🚀",
            ["🚀", "🌙", "👽"], ["🌏", "🌙", "⭐"], "🌟", ["🌙", "🎆", "💫", "🛸", "✏️"], ["🔭", "🛸", "🌠"]),
        new("fruits", "水果", "🍓", "🍓 🍑 🍋 🍉 🍒　草莓、水蜜桃、檸檬", Theme.Gradient("#FF9EB0", "#FFE58F"),
            "🍒", "🍊", "🍓🍑", "🍉",
            ["🍓", "🍑", "🍋"], ["🍉", "🍎", "🍒"], "🍒", ["🍊", "🍉", "🍋", "🍐", "✏️"], ["🍎", "🍓", "🍇"]),
    ];

    static string SetFile => Path.Combine(food.Services.DataStore.DataDirectory, "cute.txt");

    static StickerSet? set;

    /// <summary>目前的圖案組（沒選過是小動物）。</summary>
    public static StickerSet Set
    {
        get
        {
            if (set != null) return set;
            string key = "";
            try { if (File.Exists(SetFile)) key = File.ReadAllText(SetFile).Trim().ToLowerInvariant(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return set = Sets.FirstOrDefault(s => s.Key == key) ?? Sets[0];
        }
    }

    /// <summary>換圖案組；回傳是否換了（換了而且正在用可愛主題時，畫面要重建）。</summary>
    public static bool Choose(StickerSet s)
    {
        if (s == Set) return false;
        set = s;
        Theme.Write(SetFile, s.Key);
        return true;
    }

    // ---------- 給各頁面用的小工具（不是可愛主題時回傳原本的樣子） ----------

    /// <summary>頁面大標題：可愛主題在後面加這組的裝飾。</summary>
    public static string Title(string title) => Theme.CuteMode ? $"{title} {Set.Title}" : title;

    /// <summary>每個分頁右上角的小圖（0 = 餐廳、1 = 分類、2 = 更多）。</summary>
    public static string Mascot(int tab) => Set.Mascots[Math.Clamp(tab, 0, Set.Mascots.Length - 1)];

    /// <summary>空清單的提示：可愛主題在上面放一個圖案（0 = 還沒有分類、1 = 還沒有餐廳、2 = 找不到）。</summary>
    public static string Empty(string text, int kind) => Theme.CuteMode ? $"{Set.Empty[Math.Clamp(kind, 0, 2)]}\n{text}" : text;

    /// <summary>對話框的小圖示換成這組的 emoji。</summary>
    public static string DialogIcon(string icon) => !Theme.CuteMode ? icon : icon switch
    {
        "i" => Set.Dialog[0], "✓" => Set.Dialog[1], "!" => Set.Dialog[2], "?" => Set.Dialog[3], "✎" => Set.Dialog[4], _ => icon,
    };

    // ---------- 餐廳頭像的食物圖案：名稱或備註裡有關鍵字就用對應的圖案，猜不到就依名稱固定挑一個 ----------
    // 順序有意義：比較特定的放前面（「麵包」要比「麵」先比對、「雞肉飯」要比「雞」先比對）
    static readonly (string Emoji, string[] Words)[] FoodWords =
    [
        ("🍩", ["甜甜圈", "donut", "doughnut"]),
        ("🥐", ["麵包", "烘焙", "吐司", "可頌", "貝果", "bakery", "bread", "croissant", "bagel", "boulangerie"]),
        ("🍝", ["義大利麵", "義式", "焗烤", "燉飯", "pasta", "spaghetti", "italian", "trattoria"]),
        ("🍕", ["披薩", "比薩", "pizza"]),
        ("🍔", ["漢堡", "burger", "麥當勞", "mcdonald"]),
        ("🌭", ["熱狗", "hot dog", "hotdog"]),
        ("🍟", ["薯條", "fries"]),
        ("🥪", ["三明治", "潛艇堡", "sandwich", "subway"]),
        ("🌮", ["墨西哥", "塔可", "捲餅", "taco", "burrito", "mexican"]),
        ("🥩", ["牛排", "排餐", "steak"]),
        ("🍖", ["燒肉", "烤肉", "bbq", "yakiniku", "barbecue"]),
        ("🍱", ["便當", "定食", "bento"]),
        ("🦀", ["螃蟹", "蟹", "crab"]),
        ("🦪", ["生蠔", "蚵", "oyster"]),
        ("🦑", ["章魚", "魷魚", "花枝", "烏賊", "squid"]),
        ("🍣", ["壽司", "生魚片", "刺身", "sushi", "sashimi", "omakase"]),
        ("🍤", ["天婦羅", "天丼", "炸蝦", "tempura"]),
        ("🍛", ["咖哩", "印度", "curry", "indian"]),
        ("🥘", ["韓式", "韓國", "泡菜", "部隊鍋", "拌飯", "korean", "bibimbap", "西班牙", "paella"]),
        ("🍲", ["火鍋", "鍋物", "麻辣", "涮涮", "薑母鴨", "羊肉爐", "壽喜燒", "hot pot", "hotpot", "shabu", "sukiyaki", "鍋"]),
        ("🥟", ["餃", "小籠包", "湯包", "包子", "點心", "港式", "飲茶", "鍋貼", "燒賣", "dim sum", "dumpling"]),
        ("🍜", ["牛肉麵", "拉麵", "ラーメン", "烏龍", "蕎麥", "河粉", "米粉", "麵線", "ramen", "udon", "soba", "pho", "noodle", "麵"]),
        ("🍙", ["飯糰", "日式", "和食", "日本料理", "onigiri", "japanese"]),
        ("🍚", ["雞肉飯", "滷肉飯", "魯肉飯", "燒臘", "炒飯", "丼", "飯", "rice"]),
        ("🥣", ["粥", "congee", "porridge"]),
        ("🏮", ["居酒屋", "izakaya"]),
        ("🍢", ["關東煮", "串燒", "烤串", "燒烤", "鹽酥雞", "炸物", "黑輪", "yakitori", "oden"]),
        ("🍺", ["啤酒", "酒吧", "酒館", "精釀", "beer", "pub", "bar"]),
        ("🍷", ["紅酒", "葡萄酒", "酒莊", "wine", "bistro"]),
        ("🍸", ["調酒", "cocktail", "lounge"]),
        ("🧋", ["珍奶", "珍珠", "手搖", "飲料", "奶茶", "boba", "bubble tea"]),
        ("☕", ["咖啡", "珈琲", "cafe", "café", "coffee", "starbucks", "星巴克", "espresso"]),
        ("🍵", ["抹茶", "茶", "matcha", "tea"]),
        ("🥤", ["果汁", "冰沙", "juice", "smoothie"]),
        ("🍦", ["冰淇淋", "霜淇淋", "gelato", "ice cream"]),
        ("🍧", ["刨冰", "雪花冰", "豆花", "仙草", "愛玉", "剉冰", "冰"]),
        ("🥞", ["鬆餅", "格子餅", "pancake", "waffle"]),
        ("🍮", ["布丁", "焦糖", "pudding"]),
        ("🍫", ["巧克力", "chocolate"]),
        ("🍰", ["蛋糕", "甜點", "甜品", "法式", "千層", "cake", "dessert", "patisserie", "pâtisserie", "sweets"]),
        ("🍪", ["餅乾", "cookie"]),
        ("🍳", ["早午餐", "早餐", "蛋餅", "brunch", "breakfast", "diner"]),
        ("🥗", ["沙拉", "蔬食", "素食", "輕食", "salad", "vegan", "vegetarian", "素"]),
        ("🥥", ["泰式", "泰國", "越南", "越式", "南洋", "東南亞", "thai", "vietnam"]),
        ("🦐", ["海鮮", "蝦", "龍蝦", "seafood", "shrimp", "lobster"]),
        ("🐟", ["鰻魚", "魚", "fish", "unagi"]),
        ("🍗", ["炸雞", "雞排", "雞腿", "chicken", "kfc", "肯德基", "雞"]),
        ("🥓", ["豬排", "排骨", "炸豬", "pork", "tonkatsu", "豬"]),
        ("🧀", ["起司", "乳酪", "cheese"]),
        ("🍓", ["草莓", "水果", "fruit", "strawberry"]),
        ("🥚", ["蛋", "egg"]),
    ];

    /// <summary>猜不到的時候，依名稱固定挑一個。</summary>
    static readonly string[] MoreFood = ["🍙", "🍡", "🍘", "🍥", "🥠", "🍤", "🥧", "🍯", "🥨", "🥮", "🧁", "🍭", "🍿", "🥞", "🍳", "🍱"];

    public static string Food(string name, string note = "")
    {
        var text = $"{name} {note}".ToLowerInvariant();
        foreach (var (emoji, words) in FoodWords)
            if (words.Any(w => text.Contains(w, StringComparison.Ordinal))) return emoji;
        uint h = 2166136261;
        foreach (var c in name.Trim().ToUpperInvariant()) { h ^= c; h *= 16777619; }
        return MoreFood[h % (uint)MoreFood.Length];
    }
}
