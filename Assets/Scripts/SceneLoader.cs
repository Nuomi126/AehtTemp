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
    public float delayBeforeSceneLoad = 0.2f;
    
    [Header("默认选中设置")]
    public bool isDefaultSelected = false; // 是否为默认选中状态
    
    private Button button;
    
    private void Start()
    {
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
		// 如果当前场景和目标场景相同，则不切换
		if (currentSelectedScene == targetSceneName)
		{
			//Debug.Log("OnButtonClick 当前场景和目标场景相同，不切换！");
            return;
		}
		
        // 先变色
		SetButtonColor(selectedColor);

        
        // 延迟后切换场景
        StartCoroutine(LoadSceneWithDelay());
    }
    
    private IEnumerator LoadSceneWithDelay()
    {        
        yield return new WaitForSeconds(delayBeforeSceneLoad);
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
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            // 记住选中的场景
            currentSelectedScene = targetSceneName;
            
            Debug.Log("正在尝试加载场景: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("目标场景名称未设置！");
        }
    }
    
    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            currentSelectedScene = sceneName;
            Debug.Log("正在尝试加载场景: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("场景名称不能为空！");
        }
    }
    
    public void ReturnToMainMenu()
    {
        currentSelectedScene = "MainScene";
        SceneManager.LoadScene("MainScene");
    }
    
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}