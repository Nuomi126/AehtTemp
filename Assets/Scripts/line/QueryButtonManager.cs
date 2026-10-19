using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using XCharts.Example; // 添加这个命名空间引用

public class QueryButtonManager : MonoBehaviour
{
    public Button queryButton1;
    public Button queryButton2;
    public Button queryButton3;
    
    [SerializeField] private Line_LocalDB targetLineDB;
    
    void Start()
    {
        // 查找Line_LocalDB组件（如果未分配）
        if (targetLineDB == null)
        {
            targetLineDB = FindObjectOfType<Line_LocalDB>();
            if (targetLineDB == null)
            {
                Debug.LogError("未找到Line_LocalDB组件，按钮功能将无法正常工作");
                return;
            }
        }
        
        // 初始化按钮
        InitButtons();
    }
    
    private void InitButtons()
    {
        if (queryButton1 != null)
        {
            queryButton1.onClick.RemoveAllListeners();
            queryButton1.onClick.AddListener(() => {
                Debug.Log("查询按钮1被点击 - 执行查询类型1");
                StartCoroutine(ExecuteQuery(1));
            });
        }
        
        if (queryButton2 != null)
        {
            queryButton2.onClick.RemoveAllListeners();
            queryButton2.onClick.AddListener(() => {
                Debug.Log("查询按钮2被点击 - 执行查询类型2");
                StartCoroutine(ExecuteQuery(2));
            });
        }
        
        if (queryButton3 != null)
        {
            queryButton3.onClick.RemoveAllListeners();
            queryButton3.onClick.AddListener(() => {
                Debug.Log("查询按钮3被点击 - 执行查询类型3");
                StartCoroutine(ExecuteQuery(3));
            });
        }
    }
    
    private IEnumerator ExecuteQuery(int queryType)
    {
        // 设置查询类型
        targetLineDB.SetQueryType(queryType);
        
        // 等待一帧，确保设置生效
        yield return null;
        
        // 加载数据
        targetLineDB.LoadData();
    }
}