using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject panel1; // 第一个 Panel
    public GameObject panel2; // 第二个 Panel

    [Header("初始化设置")]
    public bool activatePanel2OnStart = false; // 控制panel2是否在开始时激活
    
    [Header("按钮设置")]
    public Button panel1Button; // panel1对应的按钮
    public Button panel2Button; // panel2对应的按钮
    
    [Header("颜色设置")]
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 1f); // #3399FF颜色
    public Color normalColor = Color.white; // 默认颜色
    
    // 记录当前激活的panel
    private GameObject currentActivePanel;

    private void Start()
    {		
        // 根据配置决定panel的初始状态
        if (activatePanel2OnStart)
        {
            SetPanelVisibility(panel1, false);
            SetPanelVisibility(panel2, true);
            currentActivePanel = panel2;
        }
        else
        {
            SetPanelVisibility(panel1, false);
            SetPanelVisibility(panel2, false);
            currentActivePanel = null;
        }
        
        // 立即更新按钮颜色
        UpdateButtonColors();

		// Debug 输出确认初始化状态
		UnityEngine.Debug.Log($"Panel1 initialized as hidden, Panel2 as {(activatePanel2OnStart ? "visible" : "hidden")}.");
        	
	}

	// 显示指定的 Panel 并隐藏其他 Panel
	public void ShowPanel(GameObject panelToShow)
    {
		UnityEngine.Debug.Log("Attempting to show panel: " + panelToShow.name);		

		// 更新当前激活的panel
		currentActivePanel = panelToShow;
        
        SetPanelVisibility(panel1, panelToShow == panel1);
        SetPanelVisibility(panel2, panelToShow == panel2);
        
        // 更新按钮颜色
        UpdateButtonColors();
	}
    
    // 更新按钮颜色以反映当前激活的panel
    private void UpdateButtonColors()
    {
		// 设置panel1按钮颜色
		if (panel1Button != null)
        {
            ColorBlock colors = panel1Button.colors;
            colors.normalColor = (currentActivePanel == panel1) ? selectedColor : normalColor;
            colors.highlightedColor = (currentActivePanel == panel1) ? selectedColor : normalColor;
            colors.selectedColor = (currentActivePanel == panel1) ? selectedColor : normalColor;
            panel1Button.colors = colors;
        }
        
        // 设置panel2按钮颜色
        if (panel2Button != null)
        {
            ColorBlock colors = panel2Button.colors;
            colors.normalColor = (currentActivePanel == panel2) ? selectedColor : normalColor;
            colors.highlightedColor = (currentActivePanel == panel2) ? selectedColor : normalColor;
            colors.selectedColor = (currentActivePanel == panel2) ? selectedColor : normalColor;
            panel2Button.colors = colors;
        }

		UnityEngine.Debug.Log($"Button colors updated - Panel1: {(currentActivePanel == panel1 ? "selected" : "normal")}, Panel2: {(currentActivePanel == panel2 ? "selected" : "normal")}");
		
	}

    // ���� Panel �Ŀɼ��Ժͽ����ԣ�ͬʱ���Կ����ڲ�����ļ���״̬
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
		if (panel != null)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                // ���û�� CanvasGroup��������һ���Ա��ڿ���
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;

            // ���������Ӷ����ҵ���Ҫ�ӳټ��������������伤��״̬
            foreach (Transform child in panel.transform)
            {
                if (child.gameObject.CompareTag("DelayActivation")) // ��������ʹ��tag����ʶ
                {
                    child.gameObject.SetActive(false); // Ĭ�ϲ�����
                }
            }

			UnityEngine.Debug.Log($"Panel {panel.name} visibility and interactivity set to {isVisible}");
        }
        else
        {
			UnityEngine.Debug.LogWarning("Panel is null!");
        }	
	}

    // ʾ�����������ڴ�����ť����¼�������ָ�������
    public void ActivateComponent(GameObject componentToActivate)
    {
		if (componentToActivate != null)
        {
            componentToActivate.SetActive(true);
			UnityEngine.Debug.Log($"Component {componentToActivate.name} activated.");
        }
        else
        {
			UnityEngine.Debug.LogWarning("Component to activate is null!");
        }
	}
}