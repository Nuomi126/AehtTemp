using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    // 加载指定名称的场景
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 返回主菜单
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainScene"); // 假设主界面场景名为 "MainScene"
    }
}