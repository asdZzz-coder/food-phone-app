# 餐廳收藏

收藏餐廳的 Windows 桌面程式（.NET 10 WPF）。分類分成三層：**國家 › 縣市 › 地區**，可以隨時手動新增、重新命名、刪除。

## 下載安裝

1. 到 [Releases](https://github.com/asdZzz-coder/food/releases/latest) 下載 `FoodKeeper-ClickOnce.zip`
2. 解壓縮後執行「安裝.cmd」
3. 安裝完成後，開始功能表與桌面會有「餐廳收藏」捷徑

不需要另外安裝 .NET（已內建執行環境）。之後有新版時，開啟程式就會詢問是否更新，也可以按右上角「檢查更新」。

## 功能

- **分類**：左側樹狀清單。「新增國家」在左上角；滑過分類（或按右鍵）可以「新增下一層 / 重新命名 / 刪除」；雙擊可展開或收合
- **刪除分類**：底下的子分類一起刪除，餐廳移到上一層；刪除國家時，底下的餐廳也會一起刪除（刪除前會先確認）
- **餐廳**：名稱、國家 / 縣市 / 地區、地址（可直接開 Google 地圖）、電話、評分、備註；可以放在任一層分類
- 點上層分類時，會列出底下所有子分類的餐廳；可以搜尋名稱、地址、電話、備註
- **Excel 匯出 / 匯入**：欄位為「國家、縣市、地區、餐廳名稱、地址、電話、評分、備註」，也可以自己在 Excel 依這個格式大量輸入後匯入
- 淺色 / 深色 / 跟隨系統主題

資料存在 `%AppData%\FoodKeeper\data.json`。換電腦時用「匯出 Excel」再「匯入 Excel」，或直接複製這個檔案。

## Android 版

`Mobile/` 是手機版（.NET MAUI，只有 Android），功能與電腦版相同：三層分類、餐廳清單與搜尋、評分、備註、
開 Google 地圖、撥電話、Excel 匯出 / 匯入、淺色 / 深色。手機和電腦的資料各自分開，用 Excel 匯出 / 匯入互相搬資料。

手機版另外有「可愛 🎀」主題（更多 → 外觀）：粉嫩配色、圓角更圓，餐廳頭像依名稱猜成食物圖案（拉麵 🍜、火鍋 🍲、咖啡 ☕…），
還有六組圖案可以換：小動物、甜點、花園、海洋、星空、水果（分頁小圖、分類圖示、評分、對話框、大卡片漸層都跟著換）。
圖案組與食物關鍵字在 `Mobile/Cute.cs`。換主題或圖案組會重建整個畫面（`App.Rebuild`），因為顏色是建立元件時綁定的。

- 下載：[food-phone-app 的 Releases](https://github.com/asdZzz-coder/food-phone-app/releases/latest) 的 `FoodKeeper-Android-vX.Y.Z.apk`，用手機點開安裝
- 更新：App 裡「更多 → 檢查更新」，下載新版 APK 覆蓋安裝，資料保留
- 共用程式：`Mobile.csproj` 直接編譯 `food/Models/Restaurant.cs`、`food/Services/CategoryService.cs`、`DataStore.cs`、`ExcelService.cs`，
  所以分類規則、data.json 與 Excel 格式兩邊一樣。這四個檔案不能用到 WPF。介面全部用 C# 組成（沒有 XAML），配色在 `Mobile/Theme.cs`

```bash
# 編譯 APK（Android SDK、JDK 不在預設位置時加上 -p:AndroidSdkDirectory=… -p:JavaSdkDirectory=…）
dotnet publish Mobile/Mobile.csproj -f net10.0-android -c Release -o dist/android
```

倉庫有兩個遠端：`origin` = asdZzz-coder/food（電腦版發布），`phone` = asdZzz-coder/food-phone-app（手機版發布），
兩個發布流程都只在自己的倉庫執行。手機版發布：

```bash
git tag v1.0.1
git push phone main v1.0.1
```

APK 一定要用同一把金鑰簽章，新版才能覆蓋安裝舊版。金鑰放在 food-phone-app 的 Secrets：
`ANDROID_KEYSTORE_BASE64`（金鑰檔的 base64）與 `ANDROID_KEYSTORE_PASSWORD`（別名 `foodkeeper`）。不要重新產生金鑰。

## 開發

```bash
dotnet build food/food.csproj
dotnet test food.Tests/food.Tests.csproj
```

### 發佈新版

推送 `v` 開頭的標籤後，GitHub Actions 會先跑測試，再用 ClickOnce 打包並上傳到 Releases：

```bash
git tag v1.0.1
git push origin v1.0.1
```
