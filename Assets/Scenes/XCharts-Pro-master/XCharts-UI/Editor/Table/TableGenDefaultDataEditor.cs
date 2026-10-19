using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XCharts.Runtime.UI;

namespace XCharts.Editor.UI
{
    public class TableGenDefaultDataEditor : UnityEditor.EditorWindow
    {
        [SerializeField][Range(1, 20)] private int m_Column = 5;
        [SerializeField][Range(0, 1000)] private int m_Row = 10;

        private static UITable s_Table;
        private static TableGenDefaultDataEditor window;

        public static void ShowWindow()
        {
            window = GetWindow<TableGenDefaultDataEditor>();
            window.titleContent = new GUIContent("GenDefaultData");
            window.minSize = new Vector2(450, 300);
            window.Focus();
            window.Show();
        }

        public static void UpdateData(UITable table)
        {
            s_Table = table;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (s_Table == null)
            {
                Close();
                return;
            }
            EditorGUILayout.LabelField("Gen Table Default Data:");
            m_Column = EditorGUILayout.IntField("Column", m_Column);
            m_Column = Mathf.Clamp(m_Column, 1, 20);
            m_Row = EditorGUILayout.IntField("Row", m_Row);
            m_Row = Mathf.Clamp(m_Row, 0, 1000);

            var style = GUILayout.Width(60);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview:");
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < m_Column; i++)
            {
                EditorGUILayout.LabelField("Column" + i, style);
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < m_Row; i++)
            {
                if (i < 10 || i == m_Row - 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < m_Column; j++)
                    {
                        EditorGUILayout.LabelField("cell_" + i + "_" + j, style);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (i == m_Row - 2)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("...");
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Gen"))
            {
                s_Table.columns.Clear();
                for (int i = 0; i < m_Column; i++)
                {
                    s_Table.columns.Add(new TableColumn() { content = "Column" + i });
                }

                s_Table.data.Clear();
                for (int i = 0; i < m_Row; i++)
                {
                    var data = new List<TableCell>();
                    for (int j = 0; j < m_Column; j++)
                    {
                        data.Add(new TableCell() { stringValue = "cell_" + i + "_" + j });
                    }
                    s_Table.data.Add(new TableRow() { data = data });
                }
                s_Table.RefreshAllComponent();
                s_Table.RefreshGraph();
                Close();
            }
            EditorGUILayout.Space();
        }
    }
}