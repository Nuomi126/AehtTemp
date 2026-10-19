using UnityEngine;

public class SceneOrientationManager : MonoBehaviour
{
    public enum OrientationMode
    {
        Portrait,
        LandscapeLeft,
        LandscapeRight,
        AutoRotate
    }

	[Header("设置该场景默认的屏幕方向")]
    public OrientationMode sceneOrientation = OrientationMode.LandscapeLeft;

	// 添加平台相关的状态栏设置
    private static int newStatusBarValue = 0;

	void Start()
    {
        ApplyOrientation();
  //      // 设置不显示全屏 20250801
		//Screen.fullScreen = false;
  //      Show();
	}

    void ApplyOrientation()
    {
        switch (sceneOrientation)
        {
            case OrientationMode.Portrait:
                Screen.orientation = ScreenOrientation.Portrait;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = true;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                break;

            case OrientationMode.LandscapeLeft:
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                break;

            case OrientationMode.LandscapeRight:
                Screen.orientation = ScreenOrientation.LandscapeRight;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                break;

            case OrientationMode.AutoRotate:
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = true;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                break;
        }
    }

	/// <summary>
	///  显示上方状态栏
	/// </summary>
	public static void Show()
	{
        setStatusBarValue(2048); // WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN
	}

	private static void setStatusBarValue(int value)
	{
		newStatusBarValue = value;
		using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			{
				activity.Call("runOnUiThread", new AndroidJavaRunnable(setStatusBarValueInThread));
			}
		}
	}

	private static void setStatusBarValueInThread()
	{
		using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			{
				using (var window = activity.Call<AndroidJavaObject>("getWindow"))
				{
					window.Call("setFlags", newStatusBarValue, newStatusBarValue);
				}
			}
		}
	}
}
