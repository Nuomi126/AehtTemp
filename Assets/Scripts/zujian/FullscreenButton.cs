using UnityEngine;
using UnityEngine.UI;

public class FullscreenButton : MonoBehaviour
{
    [Header("全屏按钮配置")]
    public Button fullscreenButton;
    
    [Header("需要全屏显示的滚动视图")]
    public ScrollRect scrollView;
    
    [Header("全屏设置")]
    [Tooltip("全屏时的固定大小")]
    public Vector2 fullscreenSize = new Vector2(1730, 800);  // 手机屏幕大小
    
    [Tooltip("全屏时的旋转角度（Z轴）")]
    public float fullscreenRotation = 90f;  // 旋转90度实现横屏
    
    [Tooltip("全屏时内容的缩放倍数")]
    public float contentScale = 1f;  // 不缩放，保持原始大小
    
    [Header("按钮文本")]
    public string enterFullscreenText = "全屏显示";
    public string exitFullscreenText = "退出全屏";
    
    [Header("可选：全屏时隐藏的UI元素")]
    public GameObject[] hideWhenFullscreen;
    
    // 状态变量
    private bool isFullscreen = false;
    
    // 保存ScrollView的原始状态
    private Vector2 scrollViewOriginalSize;
    private Vector2 scrollViewOriginalPosition;
    private Quaternion scrollViewOriginalRotation;
    private Vector2 scrollViewOriginalAnchorMin;
    private Vector2 scrollViewOriginalAnchorMax;
    private Vector2 scrollViewOriginalPivot;
    
    // 保存Content的原始状态
    private Vector3 contentOriginalScale;
    private Vector2 contentOriginalSize;
    private Vector2 contentOriginalPosition;

    private Vector2 contentOriginalAnchorMin;  // 新增
    private Vector2 contentOriginalAnchorMax;  // 新增
    private Vector2 contentOriginalPivot; 
    
    // 组件引用
    private RectTransform scrollViewRect;
    private RectTransform contentRect;
    // 在类的顶部添加保存子组件原始状态的变量
    private struct ChildOriginalState
    {
        public Vector2 sizeDelta;
        public Vector2 anchoredPosition;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
    }

    private ChildOriginalState[] childrenOriginalStates;
    // 在类的顶部添加保存全屏按钮原始状态的变量
    private Vector2 buttonOriginalPosition;
    private Quaternion buttonOriginalRotation;
    private Vector2 buttonOriginalAnchorMin;
    private Vector2 buttonOriginalAnchorMax;
    private Vector2 buttonOriginalPivot;
    private RectTransform buttonRect;
    
    void Start()
    {
        // 检查必要组件
        if (fullscreenButton == null)
        {
            Debug.LogError("全屏按钮未分配！");
            return;
        }
        
        if (scrollView == null)
        {
            Debug.LogError("滚动视图未分配！");
            return;
        }
        
        // 获取ScrollView的RectTransform
        scrollViewRect = scrollView.GetComponent<RectTransform>();
        if (scrollViewRect == null)
        {
            Debug.LogError("滚动视图缺少RectTransform组件！");
            return;
        }
        
        // 获取Content的RectTransform
        contentRect = scrollView.content;
        if (contentRect == null)
        {
            Debug.LogError("滚动视图缺少Content组件！");
            return;
        }
        
        // 获取全屏按钮的RectTransform
        buttonRect = fullscreenButton.GetComponent<RectTransform>();
        
        // 保存原始状态
        SaveOriginalState();

        InitializeChildrenToStandardSize();
        
        // 添加按钮点击事件
        fullscreenButton.onClick.AddListener(ToggleFullscreen);
        
        // 设置初始按钮文本
        UpdateButtonText();
    }
    
    /// <summary>
    /// 保存ScrollView和Content的原始状态
    /// </summary>
    private void SaveOriginalState()
    {
        // 保存ScrollView的原始状态
        scrollViewOriginalSize = scrollViewRect.sizeDelta;
        scrollViewOriginalPosition = scrollViewRect.anchoredPosition;
        scrollViewOriginalRotation = scrollViewRect.localRotation;
        scrollViewOriginalAnchorMin = scrollViewRect.anchorMin;
        scrollViewOriginalAnchorMax = scrollViewRect.anchorMax;
        scrollViewOriginalPivot = scrollViewRect.pivot;
        
        // 保存Content的原始状态
        contentOriginalScale = contentRect.localScale;
        contentOriginalSize = contentRect.sizeDelta;
        contentOriginalPosition = contentRect.anchoredPosition;
        contentOriginalAnchorMin = contentRect.anchorMin;  // 新增
        contentOriginalAnchorMax = contentRect.anchorMax;  // 新增
        contentOriginalPivot = contentRect.pivot;          // 新增
        
        // 保存滚动设置
        originalVertical = scrollView.vertical;
        originalHorizontal = scrollView.horizontal;
        originalScrollSensitivity = scrollView.scrollSensitivity;
    
        // 保存全屏按钮的原始状态
        if (buttonRect != null)
        {
            buttonOriginalPosition = buttonRect.anchoredPosition;
            buttonOriginalRotation = buttonRect.localRotation;
            buttonOriginalAnchorMin = buttonRect.anchorMin;
            buttonOriginalAnchorMax = buttonRect.anchorMax;
            buttonOriginalPivot = buttonRect.pivot;
        }
    
        // 保存所有子组件的原始状态
        if (contentRect != null)
        {
            childrenOriginalStates = new ChildOriginalState[contentRect.childCount];
            for (int i = 0; i < contentRect.childCount; i++)
            {
                Transform child = contentRect.GetChild(i);
                RectTransform childRect = child.GetComponent<RectTransform>();
                
                if (childRect != null)
                {
                    childrenOriginalStates[i] = new ChildOriginalState
                    {
                        sizeDelta = childRect.sizeDelta,
                        anchoredPosition = childRect.anchoredPosition,
                        anchorMin = childRect.anchorMin,
                        anchorMax = childRect.anchorMax,
                        pivot = childRect.pivot
                    };
                }
            }
        }
    }
    
    /// <summary>
    /// 切换全屏状态
    /// </summary>
    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        
        if (isFullscreen)
        {
            EnterFullscreen();
        }
        else
        {
            ExitFullscreen();
        }
        
        UpdateButtonText();
        HandleUIVisibility();
    }
    
    /// <summary>
    /// 进入全屏模式
    /// </summary>
    private void EnterFullscreen()
    {
        // 1. 设置ScrollView为全屏大小并居中
        scrollViewRect.sizeDelta = fullscreenSize;  // 1730*800
        scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
        scrollViewRect.anchoredPosition = Vector2.zero;
        
        // 2. 旋转ScrollView实现横屏效果
        scrollViewRect.localRotation = Quaternion.Euler(0, 0, fullscreenRotation);  // 旋转90度
        
        // 3. 强制设置Content的宽度为1730，不缩放Content本身
        contentRect.localScale = Vector3.one;
        
        // 4. 设置Content宽度为1730，高度根据内容动态计算
        // 由于旋转了90度，实际显示时1730对应屏幕宽度
        float contentWidth = 1730f;
        
        // 计算Content需要的高度（根据子组件数量和大小）
        int childCount = contentRect.childCount;
        float contentHeight = Mathf.Max(childCount * 800f, fullscreenSize.x * 1.5f); // 确保能够滚动
        
        contentRect.sizeDelta = new Vector2(contentWidth, contentHeight);
        
        // 5. 遍历Content的所有子组件，强制设置为1730*800
        for (int i = 0; i < contentRect.childCount; i++)
        {
            Transform child = contentRect.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            
            if (childRect != null)
            {
                // 强制设置每个子组件的大小为1730*800
                childRect.sizeDelta = new Vector2(1730f, 800f);
                
                // 设置锚点和轴心到左上角
                childRect.anchorMin = new Vector2(0f, 1f);  // 左上角
                childRect.anchorMax = new Vector2(0f, 1f);  // 左上角
                childRect.pivot = new Vector2(0f, 1f);      // 左上角
                
                // 设置子组件的位置（从左上角开始垂直排列）
                childRect.anchoredPosition = new Vector2(0, -i * 800f);
            }
        }
        
        // 6. 设置全屏按钮位置到右上角并旋转
        if (buttonRect != null)
        {
            // 设置按钮锚点到右上角
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            
            // 旋转按钮（与ScrollView同步旋转）
            buttonRect.localRotation = Quaternion.Euler(0, 0, fullscreenRotation);
            
            // 设置按钮位置（考虑旋转后的位置调整）
            // 由于旋转了90度，需要调整位置以确保按钮在可见区域
            buttonRect.anchoredPosition = new Vector2(-50f, -50f);  // 距离右上角50像素
            
            // 确保按钮显示在最上层
            buttonRect.SetAsLastSibling();
        }
        
        // 7. 确保滚动方向正确
        scrollView.vertical = true;
        scrollView.horizontal = false;
        
        // 8. 重置Content位置到顶部
        contentRect.anchoredPosition = Vector2.zero;
        
        // 9. 确保ScrollView显示在最上层（但按钮要在更上层）
        scrollView.transform.SetAsLastSibling();
        if (buttonRect != null)
        {
            buttonRect.SetAsLastSibling();
        }
        
        // 10. 调整滚动敏感度
        scrollView.scrollSensitivity = 20f;
        
        Debug.Log($"进入横屏全屏模式 - Content大小: {contentRect.sizeDelta}, 子组件数量: {childCount}, 按钮位置: 右上角");
    }
    private void InitializeChildrenToStandardSize()
{
    if (contentRect == null) return;
    
    int childCount = contentRect.childCount;
    float componentHeight = 460f;
    float componentWidth = 800f;
    
    for (int i = 0; i < childCount; i++)
    {
        Transform child = contentRect.GetChild(i);
        RectTransform childRect = child.GetComponent<RectTransform>();
        
        if (childRect != null)
        {
            childRect.sizeDelta = new Vector2(componentWidth, componentHeight);
            childRect.anchorMin = new Vector2(0f, 1f);
            childRect.anchorMax = new Vector2(0f, 1f);
            childRect.pivot = new Vector2(0f, 1f);
            childRect.anchoredPosition = new Vector2(0, -i * componentHeight);
        }
    }
    
    // 更新Content高度
    float totalContentHeight = childCount * componentHeight;
    float minContentHeight = scrollViewOriginalSize.y * 1.2f;
    float finalContentHeight = Mathf.Max(totalContentHeight, minContentHeight);
    contentRect.sizeDelta = new Vector2(componentWidth, finalContentHeight);
}
    /// <summary>
    /// 退出全屏模式
    /// </summary>
    private void ExitFullscreen()
    {
        // 1. 恢复ScrollView的原始状态
    scrollViewRect.sizeDelta = scrollViewOriginalSize;
    scrollViewRect.anchoredPosition = scrollViewOriginalPosition;
    scrollViewRect.localRotation = scrollViewOriginalRotation;
    scrollViewRect.anchorMin = scrollViewOriginalAnchorMin;
    scrollViewRect.anchorMax = scrollViewOriginalAnchorMax;
    scrollViewRect.pivot = scrollViewOriginalPivot;
    
    // 2. 恢复Content的基本状态
    contentRect.localScale = contentOriginalScale;
    contentRect.anchoredPosition = contentOriginalPosition;
    contentRect.anchorMin = contentOriginalAnchorMin;
    contentRect.anchorMax = contentOriginalAnchorMax;
    contentRect.pivot = contentOriginalPivot;
    
    // 3. 强制设置所有子组件大小为800*460并垂直排列
    int childCount = contentRect.childCount;
    float componentHeight = 460f;
    float componentWidth = 800f;
    
    for (int i = 0; i < childCount; i++)
    {
        Transform child = contentRect.GetChild(i);
        RectTransform childRect = child.GetComponent<RectTransform>();
        
        if (childRect != null)
        {
            // 强制设置组件大小为800*460
            childRect.sizeDelta = new Vector2(componentWidth, componentHeight);
            
            // 设置锚点和轴心到左上角
            childRect.anchorMin = new Vector2(0f, 1f);
            childRect.anchorMax = new Vector2(0f, 1f);
            childRect.pivot = new Vector2(0f, 1f);
            
            // 垂直排列组件
            childRect.anchoredPosition = new Vector2(0, -i * componentHeight);
        }
    }
    
    // 4. 根据组件总高度设置Content高度
    float totalContentHeight = childCount * componentHeight;
    // 确保Content高度至少比ScrollView的Viewport大一些，以支持滚动
    float minContentHeight = scrollViewOriginalSize.y * 1.2f;
    float finalContentHeight = Mathf.Max(totalContentHeight, minContentHeight);
    
    contentRect.sizeDelta = new Vector2(componentWidth, finalContentHeight);
    
    // 5. 恢复全屏按钮的原始状态
    if (buttonRect != null)
    {
        buttonRect.anchoredPosition = buttonOriginalPosition;
        buttonRect.localRotation = buttonOriginalRotation;
        buttonRect.anchorMin = buttonOriginalAnchorMin;
        buttonRect.anchorMax = buttonOriginalAnchorMax;
        buttonRect.pivot = buttonOriginalPivot;
    }
    
    // 6. 恢复原始滚动设置
    scrollView.vertical = originalVertical;
    scrollView.horizontal = originalHorizontal;
    scrollView.scrollSensitivity = originalScrollSensitivity;
    
    Debug.Log($"退出全屏模式 - 组件大小: 800x460, Content高度: {finalContentHeight}, 子组件数量: {childCount}");
    }
    
    /// <summary>
    /// 更新按钮文本
    /// </summary>
    private void UpdateButtonText()
    {
        // 支持普通Text组件
        Text buttonText = fullscreenButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = isFullscreen ? exitFullscreenText : enterFullscreenText;
        }
        
        // 支持TextMeshPro组件
        TMPro.TextMeshProUGUI tmpText = fullscreenButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = isFullscreen ? exitFullscreenText : enterFullscreenText;
        }
    }
    
    /// <summary>
    /// 处理UI元素的显示/隐藏
    /// </summary>
    private void HandleUIVisibility()
    {
        if (hideWhenFullscreen != null && hideWhenFullscreen.Length > 0)
        {
            foreach (GameObject obj in hideWhenFullscreen)
            {
                if (obj != null)
                {
                    obj.SetActive(!isFullscreen);
                }
            }
        }
    }
    
    /// <summary>
    /// 强制进入全屏模式
    /// </summary>
    public void ForceEnterFullscreen()
    {
        if (!isFullscreen)
        {
            ToggleFullscreen();
        }
    }
    
    /// <summary>
    /// 强制退出全屏模式
    /// </summary>
    public void ForceExitFullscreen()
    {
        if (isFullscreen)
        {
            ToggleFullscreen();
        }
    }
    
    /// <summary>
    /// 设置全屏时的固定大小
    /// </summary>
    public void SetFullscreenSize(Vector2 newSize)
    {
        fullscreenSize = newSize;
        
        if (isFullscreen)
        {
            scrollViewRect.sizeDelta = fullscreenSize;
        }
    }
    
    /// <summary>
    /// 设置全屏时的旋转角度
    /// </summary>
    public void SetFullscreenRotation(float newRotation)
    {
        fullscreenRotation = newRotation;
        
        if (isFullscreen)
        {
            scrollViewRect.localRotation = Quaternion.Euler(0, 0, fullscreenRotation);
        }
    }
    
    /// <summary>
    /// 设置内容缩放倍数
    /// </summary>
    public void SetContentScale(float newScale)
    {
        float oldScale = contentScale;
        contentScale = newScale;
        
        if (isFullscreen)
        {
            // 更新内容缩放
            contentRect.localScale = Vector3.one * contentScale;
            
            // 更新Content大小
            Vector2 scaledContentSize = contentOriginalSize * contentScale;
            contentRect.sizeDelta = scaledContentSize;
            
            // 调整滚动敏感度
            scrollView.scrollSensitivity = (scrollView.scrollSensitivity / oldScale) * contentScale;
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件监听
        if (fullscreenButton != null)
        {
            fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
        }
        
        // 如果处于全屏状态，强制退出
        if (isFullscreen)
        {
            ForceExitFullscreen();
        }
    }
    
    private void OnDisable()
    {
        // 组件禁用时退出全屏
        if (isFullscreen)
        {
            ForceExitFullscreen();
        }
    }
    
    // 保存ScrollView的原始滚动设置
    private bool originalVertical;
    private bool originalHorizontal;
    private float originalScrollSensitivity;
}
