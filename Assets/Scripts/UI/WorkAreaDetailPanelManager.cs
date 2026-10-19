using UnityEngine;
using UnityEngine.UI;
using TMPro;
using XCharts.Runtime.UI;
using System.Collections.Generic;

/// <summary>
/// ����/����/ϵ�� ������������
/// ���ͳ�Ʊ�������к���ʾ���������ָ���ƽ��ֵ����ʽͬ�������飨2x2���񣩡�
/// </summary>
public class WorkAreaDetailPanelManager : MonoBehaviour
{
    [Header("�������")]
    public GameObject listPanel;         // Ҫ���ص��б����(�� dangrilist �� lishilist)
    public GameObject areaDetailPanel;   // ���������(�� panelworkareadetail)

    [Header("UI�������")]
    public Button btnBack;               // ���ذ�ť
    public TMP_Text textTitle;           // ����
    public TMP_Text textDateRange;       // ���ڷ�Χ�ı�
    
    [Header("�����ͼ��")]
    public UITable detailTable;          // ����չʾ�˹�������ϸ���ݱ���

    private TMP_FontAsset m_ChineseFont;

    private void Start()
    {
        if (areaDetailPanel != null) areaDetailPanel.SetActive(false);

        if (btnBack != null)
        {
            btnBack.onClick.AddListener(OnBackButtonClicked);
        }
        
        // ���Ի�ȡȫ������
        var srm = FindObjectOfType<StatisticsReportManager>();
        if (srm != null) m_ChineseFont = srm.tableFont;
        
        InitDetailTable();
    }
    
    private void InitDetailTable()
    {
        if (detailTable == null) return;
        
        detailTable.ClearData();

        // ����ԭ�б�ͷ
        if (detailTable.columnHeaderStyle != null)
            detailTable.columnHeaderStyle.show = false;

        if (detailTable.rowHeaderStyle != null)
            detailTable.rowHeaderStyle.show = false;

        detailTable.SetColumns(new List<string> { "", "", "", "" });

        if (m_ChineseFont != null)
        {
            // ��ÿ��������Ĭ��������ʽ
            if (detailTable.columnStyle != null && detailTable.columnStyle.defaultLabelStyle != null)
            {
#if dUI_TextMeshPro
                detailTable.columnStyle.defaultLabelStyle.textStyle.tmpFont = m_ChineseFont;
                detailTable.columnStyle.defaultLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                detailTable.columnStyle.defaultLabelStyle.textStyle.font = m_ChineseFont;
#endif
                detailTable.columnStyle.defaultLabelStyle.textStyle.fontSize = 28;
            }

            for (int i = 0; i < detailTable.columns.Count && i < 4; i++)
            {
                var column = detailTable.columns[i];
                if (column != null && column.dataLabelStyle != null)
                {
                    column.dataLabelStyle.show = true;
#if dUI_TextMeshPro
                    column.dataLabelStyle.textStyle.tmpFont = m_ChineseFont;
                    column.dataLabelStyle.textStyle.tmpFontStyle = FontStyles.Normal;
#else
                    column.dataLabelStyle.textStyle.font = m_ChineseFont;
#endif
                    column.dataLabelStyle.textStyle.fontSize = 28;
                    column.dataLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                }
            }
        }
    }

    public void ShowDetail(string levelType, string currentWorkArea, string dateRange, StatisticsReportManager.StatisticsReportResponse reportData, string currentTab)
    {
        if (listPanel != null) listPanel.SetActive(false);
        if (areaDetailPanel != null) areaDetailPanel.SetActive(true);

        if (textTitle != null)
        {
            textTitle.text = $"���ƣ�{currentWorkArea}"; // ͬ���ۣ�������ʾ��ǰ�㼶����
        }
        
        if (textDateRange != null)
        {
            textDateRange.text = $"ͳ��ʱ�䣺\n{dateRange}";
        }

        if (detailTable == null)
        {
            Debug.LogWarning("[WorkAreaDetailPanel] detailTable δ���ã��޷���ʾ�������ݡ�");
            return;
        }
        
        if (m_ChineseFont == null)
        {
            var srm = FindObjectOfType<StatisticsReportManager>();
            if (srm != null) m_ChineseFont = srm.tableFont;
            InitDetailTable();
        }

        FillDetailTable(levelType, reportData);
    }

    private void FillDetailTable(string levelType, StatisticsReportManager.StatisticsReportResponse reportData)
    {
        detailTable.ClearData();
        InitDetailTable();

        if (reportData == null || reportData.rows == null || reportData.rows.Length == 0)
        {
            detailTable.AddRow(new[] { "����״̬", "�޿�������", "", "" });
            UpdateTableFont();
            return;
        }

        var rows = reportData.rows;
        int count = rows.Length;

        // ����ƽ��ֵ
        double avg_sheddy = 0;
        double avg_pingjdy = 0;
        double avg_gongzdy = 0;
        double avg_xiedl = 0;
        double avg_biaozdy = 0;

        double avg_zhenzhen = 0;
        double avg_baidong = 0;
        double avg_zonggonghao = 0;
        double avg_baidongchixu = 0;
        double avg_dianyayichangchixu = 0;

        double avg_xialiaocishu = 0;
        double avg_xialiaoliang = 0;
        double avg_fuyanxialiao = 0;
        double avg_xialiaojiange = 0;

        double avg_xiaoyingcishu = 0;
        double avg_xiaoyingchixu = 0;
        double avg_xiaoyingjuny = 0;

        foreach (var r in rows)
        {
            avg_sheddy += r.sheddy;
            avg_pingjdy += r.pingjdy;
            avg_gongzdy += r.gongzdy;
            avg_xiedl += r.xiedl;
            avg_biaozdy += r.biaozdy;

            avg_zhenzhen += r.zhenzhen;
            avg_baidong += r.baidong;
            avg_zonggonghao += r.zonggonghao;
            avg_baidongchixu += r.baidongchixu;
            avg_dianyayichangchixu += r.dianyayichangchixu;

            avg_xialiaocishu += r.xialiaocishu;
            avg_xialiaoliang += r.xialiaoliang;
            avg_fuyanxialiao += r.fuyanxialiao;
            avg_xialiaojiange += r.xialiaojiange;

            avg_xiaoyingcishu += r.xiaoyingcishu;
            avg_xiaoyingchixu += r.xiaoyingchixu;
            avg_xiaoyingjuny += r.xiaoyingjuny;
        }

        avg_sheddy /= count;
        avg_pingjdy /= count;
        avg_gongzdy /= count;
        avg_xiedl /= count;
        avg_biaozdy /= count;

        avg_zhenzhen /= count;
        avg_baidong /= count;
        avg_zonggonghao /= count;
        avg_baidongchixu /= count;
        avg_dianyayichangchixu /= count;

        avg_xialiaocishu /= count;
        avg_xialiaoliang /= count;
        avg_fuyanxialiao /= count;
        avg_xialiaojiange /= count;

        avg_xiaoyingcishu /= count;
        avg_xiaoyingchixu /= count;
        avg_xiaoyingjuny /= count;

        // �񵥲�һ������4������
        detailTable.AddRow("�趨��ѹ", (avg_sheddy / 1000.0).ToString("F3") + "V", 
                           "ƽ����ѹ", (avg_pingjdy / 1000.0).ToString("F3") + "V");

        detailTable.AddRow("������ѹ", (avg_gongzdy / 1000.0).ToString("F3") + "V", 
                           "ϵ�е���", (avg_xiedl / 1000.0).ToString("F0") + "KA");

        detailTable.AddRow("��׼��ѹ", (avg_biaozdy / 1000.0).ToString("F3") + "V", 
                           "����", avg_zhenzhen.ToString("F1"));

        detailTable.AddRow("�ڶ�", avg_baidong.ToString("F1"), 
                           "�ܹ���", (avg_zonggonghao / 1000.0).ToString("F1") + "KW");

        detailTable.AddRow("���ϴ���", avg_xialiaocishu.ToString("F1"), 
                           "������", avg_xialiaoliang.ToString("F1") + "Kg");

        // ���ݱ�����û���ṩ���ֶΣ�Ϊ��ά�ֺ�ͼ2��ʽ��ȫһ�£�ʹ��0����--ռλ
        detailTable.AddRow("����ʱ��", "--h", "����ʱ��", "--h");
        detailTable.AddRow("����ʱ��", "--h", "ЧӦ���", "--h");
        detailTable.AddRow("�������", "--h", "�������", "--h");

        detailTable.AddRow("���Ͼ��", "--h", 
                           "�������ϴ���", avg_fuyanxialiao.ToString("F1"));

        detailTable.AddRow("������", "--m��", 
                           "��׼���ϼ��", avg_xialiaojiange.ToString("F1") + "min");

        detailTable.AddRow("�����½���", "--mm", "�趨ЧӦ���", "--h");
        detailTable.AddRow("��ѹ����", "--V", "��ѹ����", "--V");
        
        detailTable.AddRow("ЧӦ��ѹ", (avg_xiaoyingjuny / 1000.0).ToString("F3") + "V", 
                           "ЧӦ�ȴ�����", "--s");

        detailTable.AddRow("��ѹ�쳣����", avg_dianyayichangchixu.ToString("F1") + "s", 
                           "�ڶ�����", avg_baidongchixu.ToString("F1") + "s");

        detailTable.AddRow("ЧӦ����", avg_xiaoyingcishu.ToString("F1"), 
                           "ЧӦ����ʱ��", avg_xiaoyingchixu.ToString("F1") + "s");

        UpdateTableFont();
    }

    /// <summary>
    /// ���±�������Ϊ�������壨������ݺ���ã�
    /// </summary>
    private void UpdateTableFont()
    {
        if (detailTable == null || m_ChineseFont == null) return;

        // ��ȡ��ǰ���� TextMeshPro ���
        var texts = detailTable.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            if (t.font != m_ChineseFont)
            {
                t.font = m_ChineseFont;
            }
        }

        // ǿ��ˢ��
        detailTable.SetAllDirty();
        detailTable.RefreshGraph();
    }

    private void OnBackButtonClicked()
    {
        if (areaDetailPanel != null) areaDetailPanel.SetActive(false);
        if (listPanel != null) listPanel.SetActive(true);
    }
}
