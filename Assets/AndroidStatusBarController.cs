using UnityEngine;

public class AndroidStatusBarController : MonoBehaviour
{
    // 是否显示状态栏
    public bool showStatusBar = true;
    // 是否显示导航栏
    public bool showNavigationBar = true;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 确保在Android平台上运行
        UpdateSystemBars();
#endif
    }

    // 更新系统栏显示状态
    public void UpdateSystemBars()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 获取当前Activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                // 获取Window对象
                AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                
                // 设置全屏标志
                int flag = 0;
                
                if (!showStatusBar)
                {
                    // 隐藏状态栏的标志
                    flag |= 1024; // WindowManager.LayoutParams.FLAG_FULLSCREEN
                }
                
                if (!showNavigationBar)
                {
                    // 隐藏导航栏的标志
                    flag |= 2048; // WindowManager.LayoutParams.FLAG_HIDE_NAVIGATION
                    flag |= 4096; // WindowManager.LayoutParams.FLAG_IMMERSIVE_STICKY
                }
                
                // 应用标志
                window.Call("setFlags", flag, flag);
                
                // 设置布局参数以适应系统栏
                AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");
                int systemUiVisibility = decorView.Call<int>("getSystemUiVisibility");
                
                if (showStatusBar)
                {
                    systemUiVisibility &= ~(1 << 0); // View.SYSTEM_UI_FLAG_FULLSCREEN
                }
                
                if (showNavigationBar)
                {
                    systemUiVisibility &= ~(1 << 2); // View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                    systemUiVisibility &= ~(1 << 3); // View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                }
                
                decorView.Call("setSystemUiVisibility", systemUiVisibility);
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogError("设置Android系统栏失败: " + e.Message);
        }
#endif
    }
}
