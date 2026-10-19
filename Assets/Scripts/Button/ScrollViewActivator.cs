using UnityEngine;

public class ScrollViewManager : MonoBehaviour
{
    // 定义所有的Scroll View游戏对象
    public GameObject[] scrollViews; // 将每个Scroll View的游戏对象拖到此数组中

    // 为每个按钮定义一个方法来激活相应的Scroll View
    public void ActivateScrollView(int index)
    {
        for (int i = 0; i < scrollViews.Length; i++)
        {
            if (i == index)
            {
                scrollViews[i].SetActive(true); // 激活指定的Scroll View
            }
            else
            {
                scrollViews[i].SetActive(false); // 禁用其他的Scroll View
            }
        }
    }
}