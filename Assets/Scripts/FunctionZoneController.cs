using UnityEngine;
using UnityEngine.UI;

public class FunctionZoneController : MonoBehaviour
{
    public Button toggleButton; // 用于显示/隐藏的按钮
    public GameObject functionZone; // 功能区域游戏对象
    private bool isFunctionZoneVisible = false; // 跟踪功能区是否可见
    
    // 可选：添加动画过渡
    public bool useAnimation = false;
    public float animationDuration = 0.3f;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        if (toggleButton == null)
        {
            // 兜底：减少场景手工拖拽遗漏导致的空引用。
            toggleButton = GetComponent<Button>();
            if (toggleButton == null)
            {
                toggleButton = GetComponentInChildren<Button>(true);
            }
        }

        // 初始化按钮点击事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleFunctionZone);
        }

        // 获取CanvasGroup组件（如果使用动画）
        if (useAnimation && functionZone != null)
        {
            canvasGroup = functionZone.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = functionZone.AddComponent<CanvasGroup>();
            }
        }

        // 仅在有切换按钮时，才强制初始隐藏；避免按钮未绑定时把功能区误隐藏。
        if (toggleButton != null)
        {
            UpdateFunctionZoneVisibility(false);
        }
    }

    private void OnDestroy()
    {
        // 移除事件监听器，防止内存泄漏
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleFunctionZone);
        }
    }

    // 切换功能区域显示状态
    public void ToggleFunctionZone()
    {
        isFunctionZoneVisible = !isFunctionZoneVisible;
        UpdateFunctionZoneVisibility(isFunctionZoneVisible);
    }

    // 更新功能区的可见性
    private void UpdateFunctionZoneVisibility(bool isVisible)
    {
        if (functionZone == null)
        {
            Debug.LogError("FunctionZone 未分配。");
            return;
        }

        if (useAnimation && canvasGroup != null)
        {
            // 使用动画过渡
            StopAllCoroutines();
            StartCoroutine(AnimateFunctionZone(isVisible));
        }
        else
        {
            // 直接设置可见性
            functionZone.SetActive(isVisible);
        }
    }

    // 动画过渡协程
    private System.Collections.IEnumerator AnimateFunctionZone(bool isVisible)
    {
        functionZone.SetActive(true);
        
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = isVisible ? 1f : 0f;
        float time = 0;
        
        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = time / animationDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            
            // 如果是隐藏，同时调整交互性
            if (!isVisible)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        
        // 如果完全隐藏，则禁用游戏对象
        if (!isVisible)
        {
            functionZone.SetActive(false);
        }
    }
    
    // 直接显示功能区
    public void ShowFunctionZone()
    {
        if (!isFunctionZoneVisible)
        {
            isFunctionZoneVisible = true;
            UpdateFunctionZoneVisibility(true);
        }
    }
    
    // 直接隐藏功能区
    public void HideFunctionZone()
    {
        if (isFunctionZoneVisible)
        {
            isFunctionZoneVisible = false;
            UpdateFunctionZoneVisibility(false);
        }
    }
}