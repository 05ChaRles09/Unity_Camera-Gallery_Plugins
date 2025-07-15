專案技術文件撰寫：NativeCamera/NativeGallery 應用程式

文件目標:
清晰描述專案的目標與功能。
解釋核心模組 (GalleryManager 和 CameraHandler) 的設計與運作原理。
提供環境配置、插件集成 (.aar) 和 UI 連接的詳細步驟。
方便未來維護和功能擴展。

為什麼需要 .aar 檔案？
模組化與重用性
簡化依賴管理
整合原生 Android 功能到 Unity
封裝複雜性
保護原始碼
避免建置衝突與環境差異
主要應用場景：
將 Unity 內容嵌入到現有的原生 Android 應用程式中。
作為一個獨立的遊戲模組或功能，被其他 Android 應用程式引用。
在複雜的 Android 構建流程中，作為一個可重用的組件。

1. 專案概述 (Project Overview)
   
    1.1 專案目標
    此專案旨在展示如何在 Unity 應用程式中集成手機的原生相機與圖庫功能，允許用戶拍照、錄影、從圖庫選擇圖片及保存應用程式截圖。

    1.2 核心功能
    列出應用程式提供的主要功能：
    透過按鈕觸發原生相機進行拍照並顯示預覽。
    透過按鈕觸發原生相機進行錄影並顯示影片縮圖。
    透過按鈕從原生圖庫選擇圖片並顯示。
    透過按鈕保存應用程式截圖到圖庫。
    處理 Android 運行時權限請求。

    1.3 開發技術環境
    Android Studio 'Narwhal' 2025.1.1
    遊戲引擎： Unity 2021.3.41f1
    JDK: 系統推薦 (預設)
    Gradle: 系統推薦 (預設)
    程式語言： C#, Java, Kotlin
    UI 系統： Unity UGUI (TextMeshPro)
    主要插件：
        1. Native Camera (v1.4.3): (提供者：yasirkula) 用於拍照和錄影。https://github.com/yasirkula/UnityNativeCamera/releases/tag/v1.4.3
        2. Native Gallery (v1.9.1): (提供者：yasirkula) 用於從圖庫選擇圖片和保存截圖。https://github.com/yasirkula/UnityNativeGallery/releases/tag/v1.9.1

2. 環境與設定 (Environment & Setup)
    2.1 開發環境要求
    Android Studio 'Narwhal' 2025.1.1
    Unity Hub
    Unity Editor 2021.3.41f1 (確保有安裝 Android Build Support, 包括 Android SDK, NDK Tools & OpenJDK)
    Visual Studio or Visual Studio Code
    Android SDK & NDK (透過 Unity Hub 安裝)
    Gradle 

    2.2 插件安裝與配置
        2.2.1 
        首先要先 Build .AAR 檔案
        在 "Project" 視窗中
        修改 Android Studio 中的 build.gradle (Module :app) 檔案
        修改 alias(libs.plugins.android.application) 成 id("com.android.library")
        刪除 or 註解 applicationId = "bla.bla.bla"
        刪除 versionCode = 1 & versionName = "1.0"
        dependencies 增加所需要的依賴
        app/src/main/java/com/example/YourProjectName 新增你所要的插件動作，通常是使用 JAVA or KOTLIN
            **注意： 預設情況下，Android Studio 可能找不到 com.unity3d.player.UnityPlayer。這是因為 Unity 的執行時庫沒有被添加到 Android 專案的依賴中。請到8.註解查看解決方法 註1** 
        在 Android Studio 頂部菜單欄，選擇 Build -> Assemble Project or 在 Android Studio 中的 Terminal 打上 ./gradlew build (如要刪除: ./gradlew clean)
        將位於專案檔案夾的 app/build/outputs/aar/app-debug.aar 複製到 Unity 專案 Assets/Plugins/Android裡 (Plugins 和 Android 需自己建立)
        從 Unity Asset Store (Unity 較新版才有) or GitHub 下載並導入這兩個插件。(GitHub 下載 .unitypackage 檔)
        Project 右鍵 Import Package -> Custom Package... -> 剛剛下載的 .unitypackage 檔
        將 Native Camera & Native Gallery 導入
        寫 C# 調用 Native Camera & Native Gallery 功能

        2.2.2 Unity Player Settings
        導航路徑：Flie > Build Settings (可以先點選 Android 標籤點右下角的 Switch Platform)> Player Settings > Android 標籤

        Other Settings：
        Scripting Backend：IL2CPP (推薦 Android)
        API Compatibility Level：.NET Standard 2.1 (或更高)
        Target Architectures：勾選 ARMv7 和 ARM64
        Write Permission：External (SDCard) (Requires Android 6.0+ permission)
        Target API Level：API level 34 or Automatic(highest installed)
        Minimum API Level：Android 7.0 'Nougat'(API level 24)
        勾選 Override Default Package Name, Package Name 必須與 Android Studio 寫的 aar 相同，ex:com.yourcompany.   unitycameraplugin

        Publishing Settings：
        勾選 Custom Main Manifest
        勾選 Custom Gradle Properties Template
        說明 gradleTemplate.properties 內容：

        打開 gradleTemplate.properties 並加上
        android.useAndroidX=true
        android.enableJetifier=true
        (這兩行是 AndroidX 和 Jetifier 兼容性所必需的)

        2.2.3 AndroidManifest.xml 權限配置
        文件路徑：Assets/Plugins/Android/AndroidManifest.xml
        XML
        <uses-permission android:name="android.permission.CAMERA" />
        <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
        <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
        <uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
        <uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />

3. 程式碼架構與核心模組 (Code Architecture & Core Modules)
    3.1 概覽
    專案主要由兩個獨立的 MonoBehaviour 腳本組成，分別處理相機和圖庫功能。
    這兩個腳本都依賴於 yasirkula 的原生插件來與設備交互。
    權限管理邏輯被抽象到各腳本內部，確保在執行相關操作前獲得用戶授權。

    3.2 GalleryManager.cs (圖庫管理模組)
    功能： 負責從設備圖庫選擇圖片和保存應用程式截圖。
    公開變數 (Inspector 連結)：
    selectImageButton: UnityEngine.UI.Button
    saveScreenshotButton: UnityEngine.UI.Button
    displayImage: UnityEngine.UI.Image (用於顯示選擇的圖片)
    statusText: UnityEngine.UI.Text (用於顯示狀態/權限訊息)

    核心方法：
    OnSelectImageClicked()：觸發選擇圖片流程。
    RequestAndSelectImage() (協程)：檢查並請求 READ_EXTERNAL_STORAGE 權限，然後呼叫 NativeGallery.GetImageFromGallery()。
    OnSaveScreenshotClicked()：觸發保存截圖流程。
    RequestAndSaveScreenshot() (協程)：檢查並請求 WRITE_EXTERNAL_STORAGE 權限，然後使用 ScreenCapture.CaptureScreenshotAsTexture() 截圖並通過 NativeGallery.SaveImageToGallery() 保存。
    RequestPermissionFlow() (協程)：通用的 Android 運行時權限請求邏輯，處理首次拒絕和永久拒絕的情況。
    ShowReRequestDialog() / ShowGoToSettingsDialog() / OpenAppSettings()：權限拒絕時的用戶引導邏輯 (目前為 Debug Log 模擬)。

    圖片顯示邏輯：
    displayImage.sprite = Sprite.Create(texture, ...)
    displayImage.SetNativeSize() (配合 UI Image 組件的 Preserve Aspect 實現圖片等比例適應容器)。

    3.3 CameraHandler.cs (相機管理模組)
    功能： 負責通過設備原生相機進行拍照和錄影。

    公開變數 (Inspector 連結)：
    photoDisplayImage: UnityEngine.UI.RawImage (用於顯示照片或影片縮圖)
    statusText: UnityEngine.UI.Text (用於顯示狀態/權限訊息)
    takePictureButton: UnityEngine.UI.Button (新加入的拍照按鈕)
    recordVideoButton: UnityEngine.UI.Button (新加入的錄影按鈕)

    核心方法：
    OnTakePictureClicked()：檢查並請求相機權限，然後呼叫 NativeCamera.TakePicture()。
    OnPhotoTaken(string path) (回調)：處理拍照結果，加載圖片到 Texture2D 並顯示在 photoDisplayImage。
    OnRecordVideoClicked()：檢查並請求相機權限，然後呼叫 NativeCamera.RecordVideo()。
    OnVideoRecorded(string path) (回調)：處理錄影結果，加載影片縮圖到 Texture2D 並顯示在 photoDisplayImage。

4. UI 界面設計與連接 (UI Design & Connection)
    Scene切到2D比較方便拖曳和設計
    4.1
    框架:
```text
    ├── Main Camera
    │   └── (Unity 場景中的主要攝影機，用於渲染遊戲畫面)
    │
    ├── Directional Light
    │   └── (Unity 場景中的預設方向光，提供基本照明)
    │
    ├── NativeCamera (空 GameObject)
    │   └── 掛載: CameraHandler.cs 腳本
    │       └── (負責處理所有與原生相機相關的邏輯，包括拍照和錄影)
    │
    ├── Canvas (相機功能 UI 畫布)
    │   └── (所有相機功能相關的 UI 元素都將放置在此 Canvas 下)
    │       ├── RawImage
    │       │   └── (用於顯示透過原生相機拍攝的照片或錄影的縮圖)
    │       ├── StatusText
    │       │   └── (用於顯示相機功能相關的狀態訊息或權限提示)
    │       ├── TakePictureButton
    │       │   └── (觸發拍照功能的 UI 按鈕)
    │       │   └── Text (Legacy)
    │       │       └── (按鈕上的文字標籤，例如 "拍照")
    │       └── RecordVideoButton
    │           └── (觸發錄影功能的 UI 按鈕)
    │           └── Text (Legacy)
    │               └── (按鈕上的文字標籤，例如 "錄影")
    │
    ├── EventSystem
    │   └── (Unity UI 系統的必要組件，用於處理使用者輸入事件，如按鈕點擊)
    │
    ├── PermissionManager (空 GameObject)
    │   └── 掛載: CameraPermissionManager.cs 腳本
    │       └── (**可選**：如果你選擇將權限管理邏輯從 `CameraHandler` 和 `GalleryManager` 中抽離，此 GameObject 將負責集中處理相機和圖庫的運行時權限請求)
    │
    ├── GalleryManagerObject (空 GameObject)
    │   └── 掛載: GalleryManager.cs 腳本
    │       └── (負責處理所有與原生圖庫相關的邏輯，包括從圖庫選擇圖片和保存截圖)
    │
    └── Canvas (圖庫功能 UI 畫布)
        └── (所有圖庫功能相關的 UI 元素都將放置在此 Canvas 下)
            ├── SelectImageButton
            │   └── (觸發從圖庫選擇圖片功能的 UI 按鈕)
            │   └── Text (Legacy)
            │       └── (按鈕上的文字標籤，例如 "選擇圖片")
            ├── SaveScreenshotButton
            │   └── (觸發保存應用程式截圖功能的 UI 按鈕)
            │   └── Text (Legacy)
            │       └── (按鈕上的文字標籤，例如 "保存截圖")
            ├── StatusText
            │   └── (用於顯示圖庫功能相關的狀態訊息或權限提示)
            └── Display Image
                └── (用於顯示從圖庫選擇的圖片)
```
    4.2 Inspector 連接步驟
    為每個腳本 (GalleryManager.cs 和 CameraHandler.cs) 創建獨立的 GameObject (例如 GalleryManagerObject 和  CameraHandlerObject)。
    將對應的 UI 元素 (按鈕、圖片、文本) 從 Hierarchy 拖曳到 Inspector 中腳本的公共變數欄位。

5. 運行與測試 (Running & Testing)
    5.1 Build 設定
    Build Settings (File > Build Settings)
    確保 Android 平台已選擇。
    將相關的場景添加到 Scenes In Build。
    Run Device 選擇要測試的設備。
    Build And Run.
    (or Build 之後複製 APK 到設備裡安裝)

    5.2 測試步驟
    在 Android 設備上運行應用程式。
    逐步測試每個按鈕功能：
    點擊「選擇圖片」，觀察權限彈窗和圖庫選擇器。
    選擇圖片後，確認圖片是否等比例顯示在右下角。
    點擊「保存截圖」，觀察截圖是否成功保存到圖庫。
    點擊「拍照」，觀察權限彈窗和相機應用。
    拍照後，確認照片是否顯示。
    點擊「錄影」，觀察權限彈窗和相機應用。
    錄影後，確認影片縮圖是否顯示。
    權限拒絕測試： 故意拒絕權限，觀察 StatusText 的提示和引導邏輯是否正確。

6. 潛在問題與解決方案 (Known Issues & Solutions)
    有遇到任何Unity本身編譯、轉碼問題請刪除 Library、Temp、obj 等資料夾。
    裝置有任何報錯都可以查閱 Android Studio 中的 Logcat 找尋問題出處。
    Native Camera/Gallery 插件報錯 (找不到類別/方法)：
    解決方案： 確認插件是否正確且乾淨地導入，並檢查 Unity Player Settings 和 AndroidManifest.xml。也可以把 Assets 裡的插件資料夾刪除，再重新Import.
    權限問題 (功能無法使用)： 再次確認 AndroidManifest.xml 中的權限聲明是否完整，並提醒用戶在手機設定中手動開啟權限。
    圖片顯示不正確 (拉伸/不全)： 確認 DisplayImage 的 Image 組件中是否勾選了 Preserve Aspect。
    UI 佈局在不同設備上錯位：
    解決方案： 再次檢查 Canvas 的 UI Scale Mode 和 Reference Resolution 設置，以及各 UI 元素的錨點設置。
    自動排版(垂直分佈、水平分佈、Grid Layout Group, Vertical Layout Group) 輸出會白畫面。也有可能是圖層問題。 **註2**

7. 未來擴展 (Future Enhancements)
    在權限拒絕時，實現更友好的 Unity UI 彈窗，而不是僅通過 Debug.Log 模擬。
    添加影片播放功能，而不僅僅是顯示縮圖。
    為 NativeCamera.LoadImageAtPath 和 NativeGallery.LoadImageAtPath 加入 maxTextureSize 參數，以優化記憶體使用。
    改善 UI 樣式和響應式佈局。

8. 註解
    可以寫 Widget, 是 Android Studio 專案 (AAR) 的一部分來實現的。

    **註1** 代表插件需要引用 UnityPlayer, 通常是寫到了import com.unity3d.player.UnityPlayer;
    在 build.gradle (Module :app) 中的 dependencies 要加上
    compileOnly(files("C:\\Program Files\\Unity\\Hub\\Editor\\ **YourUnityVersion** \\Editor\\Data\\PlaybackEngines\\AndroidPlayer\\Variations\\il2cpp\\Release\\Classes\\classes.jar"))
    不太能使用implementation, 因為最後.aar導進 Unity, Build的時候會報檔案重疊的錯。

    **註2**
    寫過的這個框架(如下)，加上 Grid Layout Group, Vertical Layout Group 最後 Build And Run 成果會是全白畫面，並且功能全無。
```text
        ├── Canvas (畫布 - 基礎)
        |   - 元件：Canvas Scaler
        |       - 設置：UI Scale Mode: Scale With Screen Size
        |       - 設置：Reference Resolution: 1920x1080
        |       - 設置：Screen Match Mode: Match Width Or Height 0.5
        |
        ├── GlobalPanel (全局背景面板 - 總容器與主題背景)
        |   - 元件：Rect Transform (錨點: Stretch, Left/Top/Right/Bottom: 0)
        |   - 元件：Image (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        |
        ├── SystemInfoPanel (系統資訊面板 - 左上角)
        │   - 元件：Rect Transform (錨點: Top-Left, Pos X/Y: 依設計調整, Width/Height: 依設計調整)
        │   - 元件：Image (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │   - 元件：Vertical Layout Group
        │       - 設置：Padding (依需求調整邊距)
        │       - 設置：Spacing (依需求調整子元素間距)
        │       - 設置：Child Alignment (子元素對齊方式)
        │       - 勾選：Control Child Size Width
        │       - 勾選：Control Child Size Height
        │       - 勾選：Use Child Force Expand Height
        │
        │   ├── BatteryInfoText (TMP_Text - 電池資訊)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │   ├── StorageInfoText (TMP_Text - 儲存資訊)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │   └── RamInfoText (TMP_Text - RAM 資訊)
        │       - 元件：Rect Transform (由父級 Layout Group 控制)
        │       - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │
        ├── QuickControlsPanel (快捷控制面板 - 中間或右側)
        │   - 元件：Rect Transform (錨點: Center-Right 或 Center-Stretch, Pos X/Y: 依設計調整, Width/Height: 依設計調整)
        │   - 元件：Image (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │   - 元件：Grid Layout Group (推薦用於按鈕排列)
        │       - 設置：Padding (網格內邊距)
        │       - 設置：Cell Size (網格單元格大小，即每個按鈕的大小)
        │       - 設置：Spacing (單元格之間的間距)
        │       - 設置：Start Corner (網格起始角落)
        │       - 設置：Start Axis (網格延伸方向)
        │       - 設置：Child Alignment (子元素在單元格內的對齊方式)
        │       - 設置：Constraint: Fixed Column Count (固定列數)
        │       - 設置：Constraint Count: (例如 2 或 3)
        │
        │   ├── WifiButton (按鈕 - Wi-Fi 開關)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表 - 如果背景需要變色)
        │   │   └── WifiStatusText (TMP_Text - Wi-Fi 狀態)
        │   │       - 元件：Rect Transform (在按鈕內部調整位置)
        │   │       - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │   ├── BluetoothButton (按鈕 - 藍牙開關)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │   │   └── BluetoothStatusText (TMP_Text - 藍牙狀態)
        │   │       - 元件：Rect Transform (在按鈕內部調整位置)
        │   │       - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │   ├── FlashlightButton (按鈕 - 手電筒開關)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │   │   └── FlashlightStatusText (TMP_Text - 手電筒狀態)
        │   │       - 元件：Rect Transform (在按鈕內部調整位置)
        │   │       - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │   ├── DisplaySettingsButton (按鈕 - 開啟顯示設定)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button
        │   ├── MobileDataButton (按鈕 - 開啟行動數據設定)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button
        │   ├── AirplaneModeButton (按鈕 - 開啟飛航模式設定)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button
        │   ├── BatterySaverButton (按鈕 - 開啟省電模式設定)
        │   │   - 元件：Rect Transform (由父級 Layout Group 控制)
        │   │   - 元件：Button
        │   └── DoNotDisturbButton (按鈕 - 勿擾模式開關)
        │       - 元件：Rect Transform (由父級 Layout Group 控制)
        │       - 元件：Button (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │       └── DoNotDisturbStatusText (TMP_Text - 勿擾模式狀態)
        │           - 元件：Rect Transform (在按鈕內部調整位置)
        │           - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容)
        │
        ├── DarkModeToggle (主題切換開關 - 右上角)
        │   - 元件：Rect Transform (錨點: Top-Right, Pos X/Y: 依設計調整, Width/Height: 依設計調整)
        │   - 元件：Toggle (拖曳此 Toggle 的 Background Image 到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
        │   └── Label (TMP_Text - 標籤文字)
        │       - 元件：TextMeshPro - Text (UI) (設置字體, 顏色, 內容 "深色模式")
        │
        └── VolumeControlPanel (音量控制面板 - 預留給未來，底部)
            - 元件：Rect Transform (錨點: Bottom-Stretch, Pos X/Y: 依設計調整, Width/Height: 依設計調整)
            - 元件：Image (拖曳此 Image 元件到 AndroidSystemBridge 腳本的 Themed Graphics 列表)
            - 元件：Vertical Layout Group 或 Horizontal Layout Group (根據音量滑條方向)
            └── (未來這裡將放置音量控制相關的 Slider, Text 等 UI 元件)
```