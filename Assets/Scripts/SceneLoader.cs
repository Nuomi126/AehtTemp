using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    // 静态变量记住当前选中的场景
    public static string currentSelectedScene = "";

    public string targetSceneName;
    
    [Header("按钮颜色设置")]
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color normalColor = Color.white;
    // 保留字段以兼容现有场景数据；菜单切换不再人为等待。
    public float delayBeforeSceneLoad = 0.2f;
    
    [Header("默认选中设置")]
    public bool isDefaultSelected = false; // 是否为默认选中状态
    
    private Button button;
    private static bool isSceneLoading;
    
    private void Start()
    {
        // 新场景激活后恢复菜单可用状态。
        isSceneLoading = false;
        button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        
        // 如果当前场景是主界面且没有设置选中场景，重置为主界面
        if (SceneManager.GetActiveScene().name == "MainScene" && string.IsNullOrEmpty(currentSelectedScene))
        {
            currentSelectedScene = "MainScene";
        }
        
        // 如果是默认选中，设置静态变量
        if (isDefaultSelected)
        {
            currentSelectedScene = targetSceneName;
        }
        
        // 检查是否是当前选中的场景
        UpdateButtonColor();
    }
    
    private void UpdateButtonColor()
    {
        if (button != null)
        {
            bool isSelected = (targetSceneName == currentSelectedScene) || isDefaultSelected;
            Color targetColor = isSelected ? selectedColor : normalColor;
            
            ColorBlock colors = button.colors;
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.selectedColor = targetColor;
            button.colors = colors;
            
            Debug.Log($"SceneLoader button ({targetSceneName}) color set to: {(isSelected ? "selected (3399FF)" : "normal")}");
        }
    }
    
    private void OnButtonClick()
    {
        if (isSceneLoading)
        {
            return;
        }

        // 以实际激活场景为准，避免静态选中状态异常时重复加载。
        if (SceneManager.GetActiveScene().name == targetSceneName)
        {
            return;
        }

        // 先变色
        SetButtonColor(selectedColor);
        LoadTargetScene();
    }
    
    private void SetButtonColor(Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.selectedColor = color;
            button.colors = colors;
        }
    }
    
    public void LoadTargetScene()
    {
        BeginLoadScene(targetSceneName);
    }

    public void LoadScene(string sceneName)
    {
        BeginLoadScene(sceneName);
    }

    private void BeginLoadScene(string sceneName)
    {
        if (isSceneLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("场景名称不能为空！");
            return;
        }

        isSceneLoading = true;
        currentSelectedScene = sceneName;
        if (button != null) button.interactable = false;

        Debug.Log("正在异步加载场景: " + sceneName);
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            isSceneLoading = false;
            if (button != null) button.interactable = true;
            Debug.LogError("无法启动场景加载: " + sceneName);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }
    }
    
    public void ReturnToMainMenu()
    {
        BeginLoadScene("MainScene");
    }
    
    public void ReloadCurrentScene()
    {
        BeginLoadScene(SceneManager.GetActiveScene().name);
    }
}
