using UnityEngine;
using UnityEngine.UI; // 引入 UnityEngine.UI 用於 Button
using System.IO;    // 確保有 System.IO 用於處理文件路徑和圖片

// 注意：NativeCamera 插件通常不需要額外的 using 語句，因為它的類別在全局命名空間中。
// 如果您遇到 NativeCamera 相關錯誤，請確保插件已正確導入。

public class CameraHandler : MonoBehaviour
{
    // 公開變數，用於從 Unity Editor 拖曳 UI 元素
    public RawImage photoDisplayImage; // 用於顯示照片或影片縮圖
    public Text statusText;            // 用於顯示應用程式狀態和提示訊息

    [Header("UI Buttons")]
    public Button takePictureButton;   // 新增：拍照按鈕
    public Button recordVideoButton;   // 新增：錄影按鈕

    void Start()
    {
        // 確保 UI 元素已連結，並為按鈕添加點擊事件監聽器
        if (takePictureButton != null)
        {
            takePictureButton.onClick.AddListener(OnTakePictureClicked);
        }
        else
        {
            Debug.LogError("拍照按鈕未連結到 CameraHandler 腳本！");
        }

        if (recordVideoButton != null)
        {
            recordVideoButton.onClick.AddListener(OnRecordVideoClicked);
        }
        else
        {
            Debug.LogError("錄影按鈕未連結到 CameraHandler 腳本！");
        }

        statusText.text = "應用程式已啟動。點擊按鈕拍照或錄影。";
    }

    // 移除 Update 函數中的點擊判斷，改為按鈕觸發
    // void Update()
    // {
    //     // ... 點擊判斷邏輯已移除
    // }

    /// <summary>
    /// 當「拍照」按鈕被點擊時調用
    /// </summary>
    public void OnTakePictureClicked()
    {
        statusText.text = "檢查拍照權限中..."; // 顯示狀態提示

        // 檢查拍照權限
        NativeCamera.Permission permission = NativeCamera.CheckPermission(true);

        if (permission == NativeCamera.Permission.Granted)
        {
            statusText.text = "正在啟動相機 (拍照)..."; // 權限允許，準備拍照
            // 呼叫 NativeCamera 拍照功能
            NativeCamera.TakePicture(OnPhotoTaken, 1024, true, NativeCamera.PreferredCamera.Front);
        }
        else
        {
            // 權限被拒絕時，給出更詳細的提示
            statusText.text = "拍照權限被拒絕 (" + permission + ")。請在手機設定中開啟相機權限以使用此功能。";
            // 可選：提供一個按鈕，引導用戶去設定頁面
            // NativeCamera.OpenSettings(); // 如果您想在拒絕後立即打開設定
        }
    }

    /// <summary>
    /// 當「錄影」按鈕被點擊時調用
    /// </summary>
    public void OnRecordVideoClicked()
    {
        statusText.text = "檢查錄影權限中..."; // 顯示狀態提示

        // 檢查錄影權限
        NativeCamera.Permission permission = NativeCamera.CheckPermission(false);

        if (permission == NativeCamera.Permission.Granted)
        {
            statusText.text = "正在啟動相機 (錄影)..."; // 權限允許，準備錄影
            // 呼叫 NativeCamera 錄影功能
            NativeCamera.RecordVideo(OnVideoRecorded, NativeCamera.Quality.High);
        }
        else
        {
            // 權限被拒絕時，給出更詳細的提示
            statusText.text = "錄影權限被拒絕 (" + permission + ")。請在手機設定中開啟相機權限以使用此功能。";
            // 可選：提供一個按鈕，引導用戶去設定頁面
            // NativeCamera.OpenSettings(); // 如果您想在拒絕後立即打開設定
        }
    }


    /// <summary>
    /// 拍照完成後的回調函數
    /// </summary>
    /// <param name="path">保存圖片的路徑，如果取消或失敗則為 null</param>
    private void OnPhotoTaken(string path)
    {
        // 清理前一個圖片紋理，避免記憶體洩漏
        if (photoDisplayImage.texture != null)
        {
            Destroy(photoDisplayImage.texture);
            photoDisplayImage.texture = null;
        }

        if (path != null)
        {
            statusText.text = "照片已拍攝：\n" + Path.GetFileName(path); // 顯示照片路徑
            // 加載圖片並顯示
            Texture2D texture = NativeCamera.LoadImageAtPath(path, 1024, false); // 1024 是 maxTextureSize，false 是 markTextureNonReadable
            if (texture != null)
            {
                photoDisplayImage.texture = texture;
                photoDisplayImage.SetNativeSize(); // 根據圖片原始尺寸調整 RawImage 大小
                // 可選：根據圖片方向旋轉 RawImage
                // NativeCamera.ImageProperties properties = NativeCamera.GetImageProperties(path);
                // if (properties.orientation == NativeCamera.ImageOrientation.Rotate90 ||
                //     properties.orientation == NativeCamera.ImageOrientation.Rotate270)
                // {
                //     photoDisplayImage.rectTransform.localEulerAngles = new Vector3(0, 0, 90); // 簡單旋轉示例
                // }
                // else
                // {
                //     photoDisplayImage.rectTransform.localEulerAngles = Vector3.zero;
                // }
            }
            else
            {
                statusText.text = "錯誤：無法加載拍攝的照片。"; // 加載失敗提示
            }
        }
        else
        {
            statusText.text = "拍照已取消或失敗。"; // 用戶取消或操作失敗提示
        }
    }

    /// <summary>
    /// 錄影完成後的回調函數
    /// </summary>
    /// <param name="path">保存影片的路徑，如果取消或失敗則為 null</param>
    private void OnVideoRecorded(string path)
    {
        // 清理前一個圖片紋理
        if (photoDisplayImage.texture != null)
        {
            Destroy(photoDisplayImage.texture);
            photoDisplayImage.texture = null;
        }

        if (path != null)
        {
            statusText.text = "影片已錄製：\n" + Path.GetFileName(path); // 顯示影片路徑
            // 處理影片錄製完成後的邏輯，例如顯示影片縮圖
            Texture2D thumbnail = NativeCamera.GetVideoThumbnail(path, 512); // 512 是 desiredTextureWidth
            if (thumbnail != null)
            {
                photoDisplayImage.texture = thumbnail;
                photoDisplayImage.SetNativeSize(); // 根據縮圖尺寸調整 RawImage 大小
            }
            else
            {
                statusText.text = "錯誤：無法生成影片縮圖。"; // 縮圖生成失敗提示
            }

            // 可選：在這裡播放影片或進一步處理
            // Debug.Log("影片路徑: " + path);
        }
        else
        {
            statusText.text = "錄影已取消或失敗。"; // 用戶取消或操作失敗提示
        }
    }
}