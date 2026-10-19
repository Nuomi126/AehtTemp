// DailyReportUIController.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyReportUIController : MonoBehaviour
{
    [Header("Table Buttons")]
    [SerializeField] private Button voltageButton;
    [SerializeField] private Button effectButton;
    [SerializeField] private Button searchButton;

    [Header("Shift Selector")]
    [SerializeField] private Button banciButton;
    [SerializeField] private GameObject banciOptionsPanel;
    [SerializeField] private Button earlyShiftButton;
    [SerializeField] private Button middleShiftButton;
    [SerializeField] private Button nightShiftButton;
    [SerializeField] private TextMeshProUGUI banciText;

    [Header("Tables")]
    [SerializeField] private GameObject voltageTableObject;
    [SerializeField] private GameObject effectTableObject;

    [Header("Query Text")]
    [SerializeField] private TextMeshProUGUI gongchangText;
    [SerializeField] private TextMeshProUGUI gongquText;

    private XCharts.UI.Demo.Table_dy voltageTable;
    private XCharts.UI.Demo.Table_xy effectTable;
    private string selectedBanci = "早班";

    private void Start()
    {
        if (voltageTableObject != null)
        {
            voltageTable = voltageTableObject.GetComponent<XCharts.UI.Demo.Table_dy>();
            voltageTableObject.SetActive(false);
        }

        if (effectTableObject != null)
        {
            effectTable = effectTableObject.GetComponent<XCharts.UI.Demo.Table_xy>();
            effectTableObject.SetActive(false);
        }

        if (voltageButton != null)
        {
            voltageButton.onClick.AddListener(OnVoltageButtonClicked);
        }

        if (effectButton != null)
        {
            effectButton.onClick.AddListener(OnEffectButtonClicked);
        }

        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchButtonClicked);
        }

        if (banciButton != null)
        {
            banciButton.onClick.AddListener(ToggleBanciOptions);
        }

        if (earlyShiftButton != null)
        {
            earlyShiftButton.onClick.AddListener(OnEarlyShiftSelected);
        }

        if (middleShiftButton != null)
        {
            middleShiftButton.onClick.AddListener(OnMiddleShiftSelected);
        }

        if (nightShiftButton != null)
        {
            nightShiftButton.onClick.AddListener(OnNightShiftSelected);
        }

        SetBanci(selectedBanci, false);

        if (banciOptionsPanel != null)
        {
            banciOptionsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (voltageButton != null)
        {
            voltageButton.onClick.RemoveListener(OnVoltageButtonClicked);
        }

        if (effectButton != null)
        {
            effectButton.onClick.RemoveListener(OnEffectButtonClicked);
        }

        if (searchButton != null)
        {
            searchButton.onClick.RemoveListener(OnSearchButtonClicked);
        }

        if (banciButton != null)
        {
            banciButton.onClick.RemoveListener(ToggleBanciOptions);
        }

        if (earlyShiftButton != null)
        {
            earlyShiftButton.onClick.RemoveListener(OnEarlyShiftSelected);
        }

        if (middleShiftButton != null)
        {
            middleShiftButton.onClick.RemoveListener(OnMiddleShiftSelected);
        }

        if (nightShiftButton != null)
        {
            nightShiftButton.onClick.RemoveListener(OnNightShiftSelected);
        }
    }

    private void ToggleBanciOptions()
    {
        if (banciOptionsPanel == null)
        {
            return;
        }

        banciOptionsPanel.SetActive(!banciOptionsPanel.activeSelf);
    }

    public void OnEarlyShiftSelected()
    {
        SetBanci("早班", true);
    }

    public void OnMiddleShiftSelected()
    {
        SetBanci("中班", true);
    }

    public void OnNightShiftSelected()
    {
        SetBanci("晚班", true);
    }

    private void SetBanci(string banci, bool refreshData)
    {
        selectedBanci = banci;

        if (banciText != null)
        {
            banciText.text = selectedBanci;
        }

        if (banciOptionsPanel != null)
        {
            banciOptionsPanel.SetActive(false);
        }

        if (refreshData)
        {
            FetchReportsFromCurrentFilters();
        }
    }

    private void OnSearchButtonClicked()
    {
        FetchReportsFromCurrentFilters();
    }

    private void OnVoltageButtonClicked()
    {
        FetchReportsFromCurrentFilters();

        if (voltageTableObject != null)
        {
            voltageTableObject.SetActive(true);
        }

        if (effectTableObject != null)
        {
            effectTableObject.SetActive(false);
        }
    }

    private void OnEffectButtonClicked()
    {
        FetchReportsFromCurrentFilters();

        if (effectTableObject != null)
        {
            effectTableObject.SetActive(true);
        }

        if (voltageTableObject != null)
        {
            voltageTableObject.SetActive(false);
        }
    }

    private void FetchReportsFromCurrentFilters()
    {
        DailyReportManager.Instance.FetchDailyReports(
            GetText(gongchangText),
            GetText(gongquText),
            selectedBanci
        );
    }

    private static string GetText(TextMeshProUGUI text)
    {
        return text != null ? text.text : null;
    }

    public string GetSelectedBanci()
    {
        return selectedBanci;
    }
}
