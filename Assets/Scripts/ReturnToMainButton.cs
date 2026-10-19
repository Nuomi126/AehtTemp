using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ReturnToMainButton : MonoBehaviour
{
    // 主界面场景的名称，默认为"MainScene"
    public string mainSceneName = "MainScene";
    
    // 按钮组件引用
    private Button returnButton;
    
    private void Start()
    {
        // 获取按钮组件
        returnButton = GetComponent<Button>();
        
        if (returnButton != null)
        {
            // 添加点击事件监听
            returnButton.onClick.AddListener(ReturnToMainScene);
        }
        else
        {
            Debug.LogError("返回按钮组件未找到，请确保此脚本挂载在Button对象上");
        }
    }
    
    private void OnDestroy()
    {
        // 移除事件监听，防止内存泄漏
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToMainScene);
        }
    }
    
    // 返回主界面方法
    public void ReturnToMainScene()
    {
        Debug.Log("正在返回主界面: " + mainSceneName);
        
        try
        {
            // 重置SceneLoader的当前选中场景为主界面
            SceneLoader.currentSelectedScene = mainSceneName;
            
            // 加载主界面场景
            SceneManager.LoadScene(mainSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("返回主界面失败: " + e.Message);
        }
    }
}