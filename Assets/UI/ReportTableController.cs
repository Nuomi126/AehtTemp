using UnityEngine;
using UnityEngine.UI;

public class ReportTabController : MonoBehaviour
{
    [Header("����")]
    public GameObject dangriPanel;   // ���� dangribaobiao
    public GameObject lishiPanel;    // ���� lishitongji

    [Header("��ť��")]
    public Button btnDangri;         // ���� "���ձ���" ��Button
    public Button btnLishi;          // ���� "��ʷ����" ��Button

    [Header("��ɫ���� (��ѡ)")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.9f, 1f); // ѡ��ʱ����ɫ

    private void Start()
    {
        // �󶨰�ť����¼�
        if (btnDangri != null) btnDangri.onClick.AddListener(ShowDangri);
        if (btnLishi != null) btnLishi.onClick.AddListener(ShowLishi);

        // Ĭ����ʾ�������
        ShowDangri();
    }

    private void ShowDangri()
    {
        if (dangriPanel) dangriPanel.SetActive(true);
        if (lishiPanel) lishiPanel.SetActive(false);
        UpdateButtonColor(btnDangri, true);
        UpdateButtonColor(btnLishi, false);

        RefreshData("dangri");
    }

    private void ShowLishi()
    {
        if (dangriPanel) dangriPanel.SetActive(false);
        if (lishiPanel) lishiPanel.SetActive(true);
        UpdateButtonColor(btnDangri, false);
        UpdateButtonColor(btnLishi, true);

        RefreshData("lishi");
    }

    private void RefreshData(string mode)
    {
        // 1. 刷新工区
        var workArea = FindObjectOfType<WorkAreaSelector>();
        if (workArea != null)
        {
            workArea.RefreshWorkAreaData();
        }

        // 2. 刷新日期 (如果有需要重置日期的逻辑，可在此添加)
        var dateSelector = FindObjectOfType<riqSelector>();
        // 若有针对日期的刷新需求可调用 dateSelector 对应方法

        // 3. 刷新表格
        var statManager = StatisticsReportManager.Instance;
        if (statManager != null)
        {
            // 单日强制用当天日期
            if (mode == "dangri")
            {
                statManager.SetQueryDate(System.DateTime.Now.ToString("yyyyMMdd"));
            }
            // 历史报表就用用户上次选好的日期
            
            // 重新请求数据刷新表格
            statManager.FetchStatisticsReport();
        }
        else
        {
            // 兼容性尝试: 也可能由 EfficiencyReportManager 负责该报表
            var effManager = FindObjectOfType<EfficiencyReportManager>();
            if (effManager != null)
            {
                effManager.FetchData(mode);
            }
        }
    }

    private void UpdateButtonColor(Button btn, bool isSelected)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = isSelected ? selectedColor : normalColor;
        }
    }
}