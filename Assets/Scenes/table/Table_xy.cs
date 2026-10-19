using UnityEngine;
using System.Collections.Generic;
using System.IO;
using XCharts.Runtime.UI;
using XCharts.Runtime;

namespace XCharts.UI.Demo
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class Table_xy : MonoBehaviour
    {
        private UITable m_Table;
        private bool isInitialized = false;

        void Awake()
        {
            m_Table = GetComponent<UITable>();
            
            // 注册数据更新事件
            if (DailyReportManager.Instance != null)
            {
                DailyReportManager.Instance.OnDataUpdated.AddListener(LoadDataFromAPI);
            }
            
            // 初始化时隐藏表头
            //HideTableHeaders();
        }
        
        
        // 初始化空表格
        private void InitEmptyTable()
        {
            // 检查表格组件是否存在
            if (m_Table == null)
            {
                m_Table = GetComponent<UITable>();
                if (m_Table == null)
                {
                    Debug.LogError("无法获取UITable组件");
                    return;
                }
            }
            
            // 检查columns是否存在
            if (m_Table.columns == null || m_Table.columns.Count < 6)
            {
                Debug.LogError("表格列数不足");
                return;
            }
            
            try
            {
                // 设置表头
                m_Table.columns[0].content = "槽号";
                m_Table.columns[0].width = 200;
                //文字居中
                m_Table.columns[0].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[1].content = "效应次数";
                m_Table.columns[1].width = 200;
                //文字居中
                m_Table.columns[1].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[2].content = "闪烁效应次数";
                m_Table.columns[2].width = 200;
                //文字居中
                m_Table.columns[2].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[3].content = "持续时间";
                m_Table.columns[3].width = 200;
                //文字居中
                m_Table.columns[3].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                m_Table.columns[4].content = "均压";
                m_Table.columns[4].width = 200;
                //文字居中
                m_Table.columns[4].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;

                m_Table.columns[5].content = "功耗";
                m_Table.columns[5].width = 200;
                //文字居中
                m_Table.columns[5].headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;


                
                // 设置表头样式
                foreach (var column in m_Table.columns)
                {
                    if (column == null) continue;
                    
                    column.headerLabelStyle.show = true;
                    column.headerLabelStyle.textStyle.fontSize = 30;
                    column.dataLabelStyle.textStyle.fontSize = 30;
                
                    
                    // 安全获取颜色
                    if (m_Table.columnHeaderStyle != null && 
                        m_Table.columnHeaderStyle.defalutLabelStyle != null &&
                        m_Table.columnHeaderStyle.defalutLabelStyle.textStyle != null)
                    {
                        column.headerLabelStyle.textStyle.color = m_Table.columnHeaderStyle.defalutLabelStyle.textStyle.color;
                    }
                    
                    // 所有列标题居中显示
                    column.headerLabelStyle.textStyle.alignment = TextAnchor.MiddleCenter;
                }
                
                // 清空所有数据行
                m_Table.ClearData();
                
                // 刷新表格
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                isInitialized = true;
                Debug.Log("表格初始化成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"初始化表格时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // 从 API 加载数据的方法
        public void LoadDataFromAPI(DailyReportResponse data)
        {
            // 确保表格已初始化
            if (!isInitialized)
            {
                InitEmptyTable();
                
            }
            else
            {
                // 显示表头
                //ShowTableHeaders();
            }
            
            if (data == null || data.rows == null || data.rows.Length == 0)
            {
                Debug.LogError("API 返回的数据为空");
                return;
            }
            
            try
            {
                // 打印接口返回的数据到控制台
                Debug.Log($"接口返回数据总数: {data.rows.Length}");
                
                // 清空现有数据
                m_Table.ClearData();
                        
                // 确保表格有足够的行
                for (int i = 0; i < data.rows.Length; i++)
                {
                    m_Table.AddRow(new string[0]);
                }
                
                // 打印调试信息
                Debug.Log($"表格行数: {m_Table.data.Count}, 接口数据行数: {data.rows.Length}");
                
                // 填充数据 - 使用与Table08类似的方式
                for (int i = 0; i < data.rows.Length ; i++)
                {
                    var report = data.rows[i];
                    var row = m_Table.data[i];
                    
                    // 确保每行有足够的单元格
                    while (row.data.Count < 6)
                    {
                        row.data.Add(new TableCell());
                    }
                    
                    // 使用字符串值而不是数值，避免类型转换问题
                    row.data[0].doubleValue = report.caoh; // 槽号
                    row.data[1].doubleValue = report.xiaoyXiaoycsh; // 效应次数
                    row.data[2].doubleValue = report.xiaoyShanshxycsh; // 闪烁效应次数
                    row.data[3].doubleValue = report.xiaoyChixshj; // 持续时间
                    row.data[4].doubleValue = report.xiaoyPingjdy; // 均压
                    row.data[5].doubleValue = report.xiaoyGongh;//效应功耗
                    
                    
                }

                // 刷新表格
                m_Table.RefreshGraph();
                m_Table.RefreshTable();
                
                // 强制刷新Canvas
                Canvas.ForceUpdateCanvases();
                
                Debug.Log($"表格更新完成，共 {m_Table.data.Count} 行，{m_Table.columns.Count} 列");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载数据时出错: {e.Message}\n{e.StackTrace}");
            }
        }
        
       
        
        // 手动加载数据的方法（可通过按钮调用）
        public void LoadData()
        {
            if (DailyReportManager.Instance != null)
            {
                DailyReportManager.Instance.FetchDailyReports();
            }
        }
    }
}
