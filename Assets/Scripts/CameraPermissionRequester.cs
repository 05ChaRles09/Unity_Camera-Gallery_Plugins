using UnityEngine;
using UnityEngine.Android;
using System.Collections;
using System.Collections.Generic;

public class CameraPermissionRequester : MonoBehaviour
{
    // 欲請求的權限清單
    private readonly string[] RequiredPermissions =
    {
        Permission.Camera,
#if UNITY_ANDROID && !UNITY_EDITOR
        "android.permission.READ_EXTERNAL_STORAGE",
        "android.permission.WRITE_EXTERNAL_STORAGE"
#endif
    };

    void Start()
    {
        StartCoroutine(RequestPermissionsFlow());
    }

    IEnumerator RequestPermissionsFlow()
    {
        List<string> permissionsToRequest = new();

        foreach (string permission in RequiredPermissions)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                permissionsToRequest.Add(permission);
            }
        }

        if (permissionsToRequest.Count == 0)
        {
            Debug.Log("所有權限都已授予！");
            StartCamera();
            yield break;
        }

        Debug.Log("請求以下權限: " + string.Join(", ", permissionsToRequest));
        foreach (string permission in permissionsToRequest)
        {
            Permission.RequestUserPermission(permission);
            yield return new WaitForSeconds(1f); // 等待使用者回應
        }

        bool allGranted = true;
        foreach (string permission in RequiredPermissions)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                Debug.LogWarning($"{permission} 權限未授予。");
                allGranted = false;
            }
        }

        if (allGranted)
        {
            Debug.Log("所有權限已成功授予！");
            StartCamera();
        }
        else
        {
            Debug.LogError("權限被拒絕，將引導使用者前往設定手動開啟。");
            OpenAppSettings();
        }
    }

    void OpenAppSettings()
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.CallStatic<AndroidJavaObject>("currentActivity");
            var packageName = currentActivity.Call<string>("getPackageName");

            using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
            {
                intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                var uri = new AndroidJavaClass("android.net.Uri")
                    .CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null);
                intent.Call<AndroidJavaObject>("setData", uri);
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                currentActivity.Call("startActivity", intent);
            }
        }
#endif
    }

    void StartCamera()
    {
        Debug.Log("相機與儲存空間功能啟用！");
        // TODO: 加入實際功能
    }
}
