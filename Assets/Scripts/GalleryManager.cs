using UnityEngine;
using UnityEngine.UI; // 用於 UI 元素，如 Image 和 Button
using System.Collections; // 用於協程

// 注意: UnityEngine.Android 命名空間是為了解決 Permission 類別的問題
// 此程式碼已針對 Permission.CanRequestPermission 的兼容性進行處理
using UnityEngine.Android; 

public class GalleryManager : MonoBehaviour
{
    [Header("UI References")]
    public Button selectImageButton;
    public Button saveScreenshotButton;
    public Image displayImage; // 用於顯示選擇的圖片
    public Text statusText; // 用於顯示權限或操作狀態

    [Header("Permission Settings")]
    [Tooltip("如果用戶首次拒絕權限，是否在點擊按鈕時再次彈出請求？")]
    public bool reRequestOnFirstDenial = true;
    [Tooltip("如果用戶勾選 '不再詢問' 並拒絕，是否引導他們到設定頁面？")]
    public bool guideToSettingsOnPermanentDenial = true;

    // 定義 Android 權限字符串
    // 請注意，對於 Android 13 (API 33) 及更高版本，READ_EXTERNAL_STORAGE 和 WRITE_EXTERNAL_STORAGE
    // 可能需要替換為更精細的 READ_MEDIA_IMAGES 和 READ_MEDIA_VIDEO。
    // NativeGallery 插件會自動處理這些新舊權限的映射。
    private const string CAMERA_PERMISSION = "android.permission.CAMERA"; 
    private const string READ_EXTERNAL_STORAGE = "android.permission.READ_EXTERNAL_STORAGE";
    private const string WRITE_EXTERNAL_STORAGE = "android.permission.WRITE_EXTERNAL_STORAGE";

    // 自定義標誌，用於模擬 Permission.CanRequestPermission 的行為
    // 如果用戶在本次會話中至少拒絕過一次，再次拒絕則視為永久拒絕
    private bool _permissionRequestedThisSession = false;
    private bool _lastPermissionDeniedPermanently = false;

    void Start()
    {
        // 初始化 UI 按鈕事件監聽器
        if (selectImageButton != null)
        {
            selectImageButton.onClick.AddListener(OnSelectImageClicked);
        }
        else
        {
            Debug.LogError("Select Image Button 未連結到 GalleryManager 腳本！");
        }

        if (saveScreenshotButton != null)
        {
            saveScreenshotButton.onClick.AddListener(OnSaveScreenshotClicked);
        }
        else
        {
            Debug.LogError("Save Screenshot Button 未連結到 GalleryManager 腳本！");
        }

        UpdateStatus("應用程式已啟動。");
    }

    /// <summary>
    /// 當「選擇圖片」按鈕被點擊時調用
    /// </summary>
    void OnSelectImageClicked()
    {
        UpdateStatus("嘗試選擇圖片...");
        StartCoroutine(RequestAndSelectImage());
    }

    /// <summary>
    /// 當「保存截圖」按鈕被點擊時調用
    /// </summary>
    void OnSaveScreenshotClicked()
    {
        UpdateStatus("嘗試保存截圖...");
        StartCoroutine(RequestAndSaveScreenshot());
    }

    /// <summary>
    /// 請求讀取儲存權限並啟動圖片選擇器
    /// </summary>
    IEnumerator RequestAndSelectImage()
    {
#if UNITY_ANDROID
        // 首先檢查並請求讀取外部儲存權限
        if (!Permission.HasUserAuthorizedPermission(READ_EXTERNAL_STORAGE))
        {
            yield return RequestPermissionFlow(READ_EXTERNAL_STORAGE, "讀取圖庫");

            // 權限仍未授予，退出操作
            if (!Permission.HasUserAuthorizedPermission(READ_EXTERNAL_STORAGE))
            {
                UpdateStatus("讀取圖庫權限被拒絕。無法選擇圖片。");
                yield break;
            }
        }
#endif

        // 權限已授予，現在調用 NativeGallery 從圖庫獲取圖片
        // 注意：這裡移除了 maxTextureSize 和 MediaType.Image 參數，以兼容你的 NativeGallery 版本
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // 從指定路徑加載圖片
                // maxTextureSize 已移除，加載原始尺寸圖片
                Texture2D texture = NativeGallery.LoadImageAtPath(path); 
                if (texture != null)
                {
                    // 將加載的圖片顯示在 UI Image 組件上
                    if (displayImage != null)
                    {
                        // 創建 Sprite 並賦值給 Image
                        displayImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        // SetNativeSize 會將 Image 的 Rect Transform 尺寸調整為圖片的原始像素尺寸
                        // 結合 UI Image 組件中勾選的 Preserve Aspect，即可實現等比例縮放適應容器
                        displayImage.SetNativeSize(); 
                    }
                    UpdateStatus("圖片選擇成功！");
                }
                else
                {
                    UpdateStatus("無法加載選擇的圖片。");
                }
            }
            else
            {
                UpdateStatus("未選擇圖片。");
            }
        }, "選擇圖片", "image/jpeg,image/png"); // 允許選擇 JPEG 和 PNG 格式的圖片
    }


    /// <summary>
    /// 請求寫入儲存權限並保存截圖
    /// </summary>
    IEnumerator RequestAndSaveScreenshot()
    {
#if UNITY_ANDROID
        // 首先檢查並請求寫入外部儲存權限
        if (!Permission.HasUserAuthorizedPermission(WRITE_EXTERNAL_STORAGE))
        {
            yield return RequestPermissionFlow(WRITE_EXTERNAL_STORAGE, "保存截圖");

            // 權限仍未授予，退出操作
            if (!Permission.HasUserAuthorizedPermission(WRITE_EXTERNAL_STORAGE))
            {
                UpdateStatus("保存截圖權限被拒絕。無法保存。");
                yield break;
            }
        }
#endif
        // 權限已授予，等待當前幀渲染完成後截圖
        yield return new WaitForEndOfFrame(); 

        Texture2D screenshotTexture = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshotTexture != null)
        {
            // 使用 NativeGallery 保存截圖到相簿
            NativeGallery.SaveImageToGallery(screenshotTexture, "MyUnityApp", "Screenshot_{0}.png", (success, path) =>
            {
                if (success)
                {
                    UpdateStatus($"截圖已保存到: {path}");
                }
                else
                {
                    UpdateStatus("截圖保存失敗。");
                }
                Destroy(screenshotTexture); // 銷毀截圖紋理以釋放記憶體
            });
        }
        else
        {
            UpdateStatus("無法捕獲截圖。");
        }
    }

    /// <summary>
    /// 處理 Android 運行時權限請求的通用流程
    /// </summary>
    /// <param name="permissionName">要請求的 Android 權限字符串 (例如 "android.permission.READ_EXTERNAL_STORAGE")</param>
    /// <param name="featureName">用於 UI 提示的權限相關功能名稱 (例如 "讀取圖庫")</param>
    IEnumerator RequestPermissionFlow(string permissionName, string featureName)
    {
        // 檢查應用程式是否已經有該權限
        if (Permission.HasUserAuthorizedPermission(permissionName))
        {
            Debug.Log($"{featureName} 權限已授予。");
            yield break; // 權限已存在，直接返回
        }

        // 如果上次該權限被標記為永久拒絕，且設定允許引導，則直接提示用戶去設定
        if (_lastPermissionDeniedPermanently && guideToSettingsOnPermanentDenial)
        {
            UpdateStatus($"請在設定中手動開啟 {featureName} 權限。");
            ShowGoToSettingsDialog(featureName);
            yield break;
        }

        // 請求權限
        Debug.Log($"請求 {featureName} 權限...");
        _permissionRequestedThisSession = true; // 標記為本次應用程式會話中已發起權限請求

        Permission.RequestUserPermission(permissionName);

        // 等待一小段時間，讓系統的權限彈窗有時間顯示並讓用戶作出選擇
        // Unity 的 RequestUserPermission 調用後不會立即返回結果，需要延遲檢查
        yield return new WaitForSeconds(0.5f); 

        // 再次檢查權限狀態
        if (Permission.HasUserAuthorizedPermission(permissionName))
        {
            Debug.Log($"{featureName} 權限已授予。");
            _lastPermissionDeniedPermanently = false; // 權限已授予，重置永久拒絕標誌
            yield break;
        }

        // 如果用戶拒絕了權限
        Debug.LogWarning($"{featureName} 權限被拒絕。");

        // 由於 Permission.CanRequestPermission 可能報錯，我們使用自定義邏輯判斷是否為永久拒絕
        // 如果在本次會話中已經請求過權限，並且用戶再次拒絕，則假定為永久拒絕
        if (_permissionRequestedThisSession && !Permission.HasUserAuthorizedPermission(permissionName))
        {
            _lastPermissionDeniedPermanently = true; // 假定為永久拒絕
            Debug.LogWarning($"用戶對 {featureName} 權限的第二次或更多次拒絕，視為永久拒絕。");
            if (guideToSettingsOnPermanentDenial)
            {
                UpdateStatus($"請在設定中手動開啟 {featureName} 權限。");
                ShowGoToSettingsDialog(featureName); // 彈出對話框引導用戶
            }
            else
            {
                UpdateStatus($"{featureName} 權限未授予，功能受限。");
            }
        }
        else if (reRequestOnFirstDenial)
        {
            // 用戶首次拒絕（沒有勾選"不再詢問"），允許再次彈出請求
            Debug.Log($"用戶單純拒絕了 {featureName} 權限，可以再次嘗試請求。");
            UpdateStatus($"請確認是否授予 {featureName} 權限。");
            ShowReRequestDialog(permissionName, featureName); // 彈出對話框解釋並再次請求
        }
    }

    /// <summary>
    /// 顯示一個自定義對話框，解釋為什麼需要權限，並提供再次請求的選項。
    /// </summary>
    void ShowReRequestDialog(string permissionName, string featureName)
    {
        // 在實際的應用程式中，您應該在這裡實現一個美觀的 Unity UI 彈窗。
        // 這個彈窗應包含：
        // 1. 解釋文字：說明為什麼應用程式需要此權限。
        // 2. 「再次請求」按鈕：用戶點擊後調用 StartCoroutine(DelayReRequest(...))。
        // 3. 「取消」或「不允許」按鈕：讓用戶拒絕，然後應用程式可能禁用相關功能。

        Debug.Log($"[UI Dialog]: 我們需要 {featureName} 權限來啟用相關功能。請問您是否願意再次授予？(這裡僅為演示，會自動模擬點擊 '是')");

        // 模擬用戶點擊「是」再次請求（在真實應用中，這是 UI 按鈕的事件觸發）
        StartCoroutine(DelayReRequest(permissionName, featureName));
    }

    /// <summary>
    /// 延遲後再次發起權限請求。
    /// </summary>
    IEnumerator DelayReRequest(string permissionName, string featureName)
    {
        yield return new WaitForSeconds(2f); // 模擬用戶閱讀提示的時間
        UpdateStatus($"再次請求 {featureName} 權限...");
        Permission.RequestUserPermission(permissionName); // 再次發起系統權限請求

        // 等待並重新檢查權限狀態
        yield return new WaitForSeconds(0.5f);
        if (Permission.HasUserAuthorizedPermission(permissionName))
        {
            Debug.Log($"再次請求 {featureName} 權限成功。");
            _lastPermissionDeniedPermanently = false; // 權限已授予，重置永久拒絕標誌

            // 根據初始調用者的需求，重新觸發對應的功能
            if (permissionName == READ_EXTERNAL_STORAGE)
            {
                StartCoroutine(RequestAndSelectImage());
            }
            else if (permissionName == WRITE_EXTERNAL_STORAGE)
            {
                StartCoroutine(RequestAndSaveScreenshot());
            }
        }
        else
        {
            Debug.LogWarning($"再次請求 {featureName} 權限仍被拒絕。");
            UpdateStatus($"{featureName} 權限再次被拒絕。");
            // 如果再次被拒絕，這次我們就強制視為永久拒絕
            _lastPermissionDeniedPermanently = true;
            if (guideToSettingsOnPermanentDenial)
            {
                UpdateStatus($"請在設定中手動開啟 {featureName} 權限。");
                ShowGoToSettingsDialog(featureName);
            }
            else
            {
                UpdateStatus($"{featureName} 權限未授予，功能受限。");
            }
        }
    }

    /// <summary>
    /// 顯示一個自定義對話框，引導用戶前往應用程式設定頁面手動開啟權限。
    /// </summary>
    void ShowGoToSettingsDialog(string featureName)
    {
        // 在實際的應用程式中，您應該在這裡實現一個美觀的 Unity UI 彈窗。
        // 這個彈窗應包含：
        // 1. 解釋文字：說明權限已被永久拒絕，需要手動開啟。
        // 2. 「前往設定」按鈕：用戶點擊後調用 StartCoroutine(DelayOpenAppSettings())。
        // 3. 「取消」或「關閉」按鈕：讓用戶關閉彈窗。

        Debug.LogError($"[UI Dialog]: {featureName} 權限已被永久拒絕。請前往手機設定 > 應用程式 > [你的應用名稱] > 權限，手動開啟。 (這裡僅為演示，會自動模擬點擊 '前往設定')");

        // 模擬用戶點擊「前往設定」按鈕（在真實應用中，這是 UI 按鈕的事件觸發）
        StartCoroutine(DelayOpenAppSettings());
    }

    /// <summary>
    /// 延遲後打開應用程式的設定頁面。
    /// </summary>
    IEnumerator DelayOpenAppSettings()
    {
        yield return new WaitForSeconds(2f); // 模擬用戶閱讀提示的時間
        OpenAppSettings();
    }

    /// <summary>
    /// 打開當前應用程式在 Android 系統中的設定頁面。
    /// </summary>
    void OpenAppSettings()
    {
#if UNITY_ANDROID // 僅在 Android 平台上編譯和執行
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.CallStatic<AndroidJavaObject>("currentActivity");
            var packageName = currentActivity.Call<string>("getPackageName");

            using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
            {
                intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                intent.Call<AndroidJavaObject>("setData", new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null));
                intent.Call("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                currentActivity.Call("startActivity", intent);
            }
        }
#endif
    }

    /// <summary>
    /// 更新 UI 狀態文本並在 Debug Log 中輸出訊息。
    /// </summary>
    /// <param name="message">要顯示的訊息。</param>
    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Debug.Log($"Status: {message}");
        }
    }
}