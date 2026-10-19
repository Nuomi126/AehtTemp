using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XUGL;

namespace XCharts.Runtime.Treemaps
{
    [UnityEngine.Scripting.Preserve]
    internal sealed class TreemapHandler : SerieHandler<Treemap>
    {
        private const string c_Ellipsis = "...";
        private SerieData m_HighlightSerieData;
        private SerieData m_SelectedSerieData;

        private bool m_IsInGoUpLevelButton;
        private Rect m_GoUpLevelButtomRect = Rect.zero;

        public override void Update()
        {
            base.Update();
            if (serie.customGoUpLevelButton != null)
            {
                ChartHelper.SetActive(serie.customGoUpLevelButton, CanGoUpALevel());
            }
        }

        public override void UpdateSerieContext()
        {
            var needCheck = chart.isPointerInChart && serie.context.rect.Contains(chart.pointerPos);
            if (!needCheck)
            {
                if (m_LastCheckContextFlag != needCheck)
                {
                    m_LastCheckContextFlag = needCheck;
                    serie.context.pointerItemDataIndex = -1;
                    serie.context.pointerEnter = false;
                    foreach (var serieData in serie.data)
                    {
                        serieData.context.highlight = false;
                    }
                    chart.RefreshPainter(serie);
                }
                return;
            }
            m_LastCheckContextFlag = needCheck;
            serie.context.pointerEnter = false;
            serie.context.pointerItemDataIndex = -1;
            var highlighted = false;
            if (m_SelectedSerieData != null && m_GoUpLevelButtomRect.Contains(chart.pointerPos))
            {
                m_IsInGoUpLevelButton = true;
                serie.context.pointerEnter = true;
                serie.context.pointerItemDataIndex = int.MaxValue;
                // chart.tooltip.UpdateContentPos(chart.pointerPos + chart.tooltip.offset);
                // var content = "Go up a level";
                // TooltipHelper.SetContentAndPosition(chart.tooltip, content.TrimStart(), chart.chartRect);
                // chart.tooltip.SetActive(true);
                // break;
            }
            else
            {
                m_IsInGoUpLevelButton = false;
                var level = m_SelectedSerieData == null ? 0 : 1;
                var data = m_SelectedSerieData == null ? serie.context.rootData : m_SelectedSerieData.context.children;
                var serieData = GetHighlightSerieData(data, chart.pointerPos, level);
                if (serieData != null)
                {
                    highlighted = true;
                    serie.context.pointerEnter = true;
                    serie.context.pointerItemDataIndex = serieData.index;
                    if (m_HighlightSerieData != serieData)
                    {
                        if (m_HighlightSerieData != null)
                            m_HighlightSerieData.context.highlight = false;
                        m_HighlightSerieData = serieData;
                        chart.RefreshPainter(serie);
                    }
                }
            }
            if (!m_IsInGoUpLevelButton && !highlighted && m_HighlightSerieData != null)
            {
                m_HighlightSerieData.context.highlight = false;
                m_HighlightSerieData = null;
            }
        }

        public override void UpdateTooltipSerieParams(int dataIndex, bool showCategory, string category,
            string marker, string itemFormatter, string numericFormatter, string ignoreDataDefaultContent,
            ref List<SerieParams> paramList, ref string title)
        {
            if (m_IsInGoUpLevelButton)
            {
                title = null;
                var param = serie.context.param;
                param.columns.Clear();
                param.columns.Add("");
                param.columns.Add("Go up a level");
                paramList.Add(param);
            }
            else
                UpdateItemSerieParams(ref paramList, ref title, dataIndex, category,
                    marker, itemFormatter, numericFormatter, ignoreDataDefaultContent);
        }

        public override void DrawSerie(VertexHelper vh)
        {
            if (serie.dataCount == 0) return;
            if (!serie.show) return;
            serie.OnDataUpdate();
            SerieHelper.UpdateRect(serie, chart.chartPosition, chart.chartWidth, chart.chartHeight);
            Squarity(vh, serie);
        }

        private SerieData GetHighlightSerieData(List<SerieData> data, Vector2 local, int level)
        {
            foreach (var serieData in data)
            {
                if (level < serie.leafDepth - 1)
                {
                    if (serieData.context.children.Count > 0)
                    {
                        var target = GetHighlightSerieData(serieData.context.children, local, level + 1);
                        if (target != null) return target;
                    }
                }
                if (serieData.context.rect.Contains(local))
                {
                    serieData.context.highlight = true;
                    return serieData;
                }
                else
                {
                    serieData.context.highlight = false;
                }
            }
            return null;
        }

        internal void SetSelectedSerieData(SerieData serieData)
        {
            if (serieData != null && serieData.context.level == serie.leafDepth - 1 &&
                serieData.context.children.Count > 0)
            {
                m_SelectedSerieData = serieData;
                ChartHelper.HideAllObject(chart.transform.Find("label"));
                chart.RefreshChart();
            }
        }

        internal void GoUpALevel()
        {
            if (m_SelectedSerieData != null)
            {
                if (m_SelectedSerieData.context.parent != null)
                    m_SelectedSerieData = m_SelectedSerieData.context.parent;
                else
                    m_SelectedSerieData = null;
                chart.RefreshChart();
            }
        }

        internal bool CanGoUpALevel()
        {
            return m_SelectedSerieData != null && m_SelectedSerieData.context.parent != null;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (m_IsInGoUpLevelButton)
            {
                GoUpALevel();
            }
            else
            {
                SetSelectedSerieData(m_HighlightSerieData);
            }
        }

        public override void RefreshLabelInternal()
        {
            if (!m_InitedLabel)
                return;
            ChartHelper.HideAllObject(labelObject);
            var level = 0;
            if (m_SelectedSerieData == null)
            {
                var data = serie.context.rootData;

                for (int n = 0; n < data.Count; n++)
                {
                    var serieData = data[n];
                    if (!serieData.show) continue;
                    DrawLabel(serie, serieData, level);
                }
            }
            else
            {
                DrawLabel(serie, m_SelectedSerieData, level);
                foreach (var child in m_SelectedSerieData.context.children)
                {
                    DrawLabel(serie, child, level + 1);
                }
            }
        }

        private void DrawLabel(Treemap serie, SerieData serieData, int level)
        {
            if (serieData == null) return;
            if (serieData.labelObject == null) return;
            if (level > serie.leafDepth - 1)
            {
                serieData.SetLabelActive(false);
                return;
            }
            if (serieData.context.children.Count > 0)
            {
                serieData.SetLabelActive(false);
                foreach (var childData in serieData.context.children)
                {
                    DrawLabel(serie, childData, level + 1);
                }
            }
            var isUpperLabel = false;
            var serieLabel = SerieHelper.GetSerieLabel(serie, serieData);
            if (serieData.context.children.Count > 0 && level < serie.leafDepth - 1)
            {
                isUpperLabel = true;
                serieLabel = serie.GetLevelUpperLabel(level);
            }
            else
            {
                serieLabel = serie.GetLevelLabel(level);
            }
            var showLabel = (serieLabel != null && serieLabel.show) && serieData.context.canShowLabel;
            if (serieData.context.rect.width < 10 || serieData.context.rect.height < 10)
                showLabel = false;
            if (showLabel)
            {
                serieData.labelObject.SetActive(true);
                serieData.SetLabelActive(showLabel);
                var color = ChartHelper.IsClearColor(serie.label.textStyle.color) ? Color.white : serie.label.textStyle.color;
                var fontSize = serieLabel.textStyle.GetFontSize(chart.theme.common);
                var fontStyle = serieLabel.textStyle.fontStyle;
                serieData.labelObject.text.SetColor(color);
                serieData.labelObject.text.SetFontSize(fontSize);
                serieData.labelObject.text.SetFontStyle(fontStyle);
                serieData.labelObject.SetTextRotate(serieLabel.rotate);
                string content;
                if (!string.IsNullOrEmpty(serieLabel.formatter))
                {
                    var value = serieData.data[1];
                    var total = serie.yTotal;
                    content = SerieLabelHelper.GetFormatterContent(serie, serieData, value, total, serieLabel, color);
                }
                else
                {
                    if (serieData == m_SelectedSerieData)
                        content = GetLinkName(serieData);
                    else
                        content = serieData.name;
                }
                if (serieData.context.children.Count > 0 && level == serie.leafDepth - 1)
                    content = serie.drillDownIcon + content;
                var fixedText = serieData.labelObject.text.GetPreferredText(content, c_Ellipsis, serieData.context.rect.width - 5);
                if (serieData.labelObject.SetText(fixedText)) chart.RefreshPainter(serie);

                //var position = SerieLabel.Position.Center;
                var position = serieLabel.position;
                if (isUpperLabel)
                {
                    if (serieLabel.position != LabelStyle.Position.Center)
                        position = LabelStyle.Position.Left;
                }
                if (position == LabelStyle.Position.Left)
                {
                    var rect = serieData.context.rect;
                    var halfLabelWidth = serieData.GetLabelWidth() / 2;
                    var halfLabelHight = serieData.GetLabelHeight() / 2;
                    var tl = new Vector2(rect.x + halfLabelWidth, rect.y + rect.height - halfLabelHight);
                    serieData.context.labelPosition = tl;
                }
                else
                {
                    serieData.context.labelPosition = new Vector3(serieData.context.rect.center.x, serieData.context.rect.center.y);
                }
                serieData.labelObject.SetPosition(serieData.context.labelPosition + serieLabel.offset);
            }
            else
            {
                serieData.labelObject.SetActive(false);
                serieData.SetLabelActive(false);
            }
        }

        private string GetLinkName(SerieData serieData)
        {
            var str = serieData.name;
            while (serieData.context.parent != null)
            {
                str = serieData.context.parent.name + " | " + str;
                serieData = serieData.context.parent;
            }
            return str;
        }

        private void Squarity(VertexHelper vh, Treemap serie)
        {
            //var level = m_SelectedSerieData != null ? m_SelectedSerieData.runtimeLevel : 0;
            var level = 0;
            var itemStyle = serie.GetLevelItemStyle(level);
            var upperLabel = serie.GetLevelUpperLabel(level);

            var areaWidth = Mathf.Max(0, serie.context.width - itemStyle.borderWidth);
            var areaHeight = Mathf.Max(0, serie.context.height - itemStyle.borderWidth);
            var emptyArea = new Rect(serie.context.x, serie.context.y, areaWidth, areaHeight);

            if (m_SelectedSerieData == null)
            {
                Squarify(serie, emptyArea, null, serie.context.rootData, level);
                CutRectBorder(serie.context.rect, serie, serie.context.rootData, level);
                DrawAllRect(vh, null, serie.context.rootData, level);
                DrawAllBorder(vh, serie, itemStyle.borderWidth, itemStyle.borderColor);
            }
            else
            {
                var titleHig = upperLabel.show ? upperLabel.distance : 0;
                var subRect = GetCutTitleLabelRect(emptyArea, titleHig);
                m_SelectedSerieData.context.rect = emptyArea;
                m_SelectedSerieData.context.subRect = subRect;
                Squarify(serie, subRect, m_SelectedSerieData, m_SelectedSerieData.context.children, level + 1);
                CutRectBorder(subRect, serie, m_SelectedSerieData.context.children, level + 1);

                UGL.DrawRectangle(vh, emptyArea,
                    m_SelectedSerieData.context.highlight ?
                    ChartHelper.GetHighlightColor(m_SelectedSerieData.context.color) :
                    m_SelectedSerieData.context.color);
                DrawAllRect(vh, m_SelectedSerieData, m_SelectedSerieData.context.children, level + 1);
                DrawAllBorder(vh, serie, itemStyle.borderWidth, itemStyle.borderColor);
                DrawGoUpButtom(vh, emptyArea);
            }
        }

        private void DrawGoUpButtom(VertexHelper vh, Rect rect)
        {
            if (serie.customGoUpLevelButton != null) return;
            var btnMargin = 15f;
            var btnWidth = 60f;
            var btnColor = Color.white;
            m_GoUpLevelButtomRect = new Rect(rect.xMax - btnWidth - btnMargin, rect.yMax - btnWidth - btnMargin, btnWidth, btnWidth);
            UGL.DrawCricle(vh, m_GoUpLevelButtomRect.center, btnWidth / 2, btnColor);

            var arrowMargin = 10f;
            var arrowStartPos = new Vector3(m_GoUpLevelButtomRect.center.x, m_GoUpLevelButtomRect.center.y - (btnWidth / 2 - arrowMargin));
            var arrowEndPos = new Vector3(m_GoUpLevelButtomRect.center.x, m_GoUpLevelButtomRect.center.y + btnWidth / 2 - arrowMargin);
            UGL.DrawLine(vh, arrowStartPos, arrowEndPos, 4, Color.black);
            UGL.DrawArrow(vh, arrowStartPos, arrowEndPos, 30, 30, 0, 5, Color.black);
        }

        private void DrawAllRect(VertexHelper vh, SerieData parent, List<SerieData> data, int level)
        {
            int colorIndex = 0;
            foreach (var serieData in data)
            {
                if (parent == null)
                {
                    serieData.context.color = chart.theme.GetColor(colorIndex++);
                }
                else
                {
                    serieData.context.color = GetLevelColor(parent.context.color, level);
                }
                UGL.DrawRectangle(vh, serieData.context.rect,
                    serieData.context.highlight ? ChartHelper.GetHighlightColor(serieData.context.color) : serieData.context.color);
                if (serieData.context.children.Count > 0)
                {
                    DrawRect(vh, serieData.context.children, level + 1, serieData.context.color);
                }
            }
        }

        private void DrawAllBorder(VertexHelper vh, Treemap serie, float border, Color32 borderColor)
        {
            int level = 0;

            if (m_SelectedSerieData != null)
            {
                //level = m_SelectedSerieData.context.level;
                UGL.DrawBorder(vh, m_SelectedSerieData.context.rect, border, borderColor);
                if (m_SelectedSerieData.context.children.Count > 0)
                {
                    DrawBorder(vh, m_SelectedSerieData.context.subRect, serie, m_SelectedSerieData.context.children, level + 1);
                }
            }
            else
            {
                foreach (var serieData in serie.context.rootData)
                {
                    UGL.DrawBorder(vh, serieData.context.rect, border, borderColor);
                    if (serieData.context.children.Count > 0)
                    {
                        DrawBorder(vh, serieData.context.subRect, serie, serieData.context.children, level + 1);
                    }
                }
            }
        }

        private float c_SubRectExtraWid = 0.1f;
        private void CutRectBorder(Rect parent, Treemap serie, List<SerieData> data, int level)
        {
            var itemStyle = serie.GetLevelItemStyle(level);
            var upperLabel = serie.GetLevelUpperLabel(level);
            var border = itemStyle.borderWidth;
            var titleHig = upperLabel.show ? upperLabel.distance : 0f;
            if (border == 0) return;
            foreach (var serieData in data)
            {
                if (parent == Rect.zero)
                {
                    serieData.context.rect = Rect.zero;
                    CutRectBorder(serieData.context.rect, serie, serieData.context.children, level + 1);
                }
                else
                {
                    var rect = serieData.context.rect;
                    var halfWid = Math.Max(0, rect.width - border) / 2;
                    var halfHig = Math.Max(0, rect.height - border) / 2;
                    var xMin = rect.center.x - halfWid;
                    var xMax = rect.center.x + halfWid;
                    var yMin = rect.center.y - halfHig;
                    var yMax = rect.center.y + halfHig;
                    if (xMin < parent.xMin) xMin = parent.xMin;
                    if (xMax > parent.xMax) xMax = parent.xMax;
                    if (yMin < parent.yMin) yMin = parent.yMin;
                    if (yMax > parent.yMax) yMax = parent.yMax;
                    if (xMin >= xMax || yMin >= yMax)
                    {
                        serieData.context.rect = Rect.zero;
                        CutRectBorder(serieData.context.rect, serie, serieData.context.children, level + 1);
                    }
                    else
                    {
                        serieData.context.rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

                        if (titleHig > 0)
                        {
                            if (titleHig * 2 < xMax - xMin && titleHig * 2 < yMax - yMin)
                            {
                                serieData.context.subRect = Rect.MinMaxRect(
                                    xMin + titleHig,
                                    yMin + titleHig,
                                    xMax - titleHig,
                                    yMax - titleHig);
                            }
                            else
                            {
                                serieData.context.subRect = Rect.zero;
                            }
                            CutRectBorder(serieData.context.subRect, serie, serieData.context.children, level + 1);
                        }
                        else
                        {
                            serieData.context.subRect = serieData.context.rect;
                            CutRectBorder(serieData.context.rect, serie, serieData.context.children, level + 1);
                        }
                    }
                }
            }
        }

        private void DrawRect(VertexHelper vh, List<SerieData> data, int level, Color32 rectColor)
        {
            int maxLevel = serie.leafDepth - 1;
            if (level > maxLevel) return;
            var levelColor = GetLevelColor(rectColor, level);

            foreach (var serieData in data)
            {
                serieData.context.color = levelColor;
                UGL.DrawRectangle(vh, serieData.context.rect, 0, serieData.context.highlight ? ChartHelper.GetHighlightColor(levelColor) : levelColor);
                if (serieData.context.children.Count > 0)
                    DrawRect(vh, serieData.context.children, level + 1, levelColor);
            }
        }

        private Color32 GetLevelColor(Color32 color, int level)
        {
            var rate = 0.1f;
            var r = (byte)Math.Min(255, (color.r * (1 + level * rate)));
            var g = (byte)Math.Min(255, (color.g * (1 + level * rate)));
            var b = (byte)Math.Min(255, (color.b * (1 + level * rate)));
            return new Color32(r, g, b, 255);
        }

        private void DrawBorder(VertexHelper vh, Rect parent, Treemap serie, List<SerieData> data, int level)
        {
            if (level > serie.leafDepth - 1) return;
            var itemStyle = serie.GetLevelItemStyle(level);
            var upperLabel = serie.GetLevelUpperLabel(level);
            var titleHig = upperLabel.show ? upperLabel.distance : 0f;
            var border = itemStyle.borderWidth;
            var borderColor = itemStyle.borderColor;
            var borderColor2 = itemStyle.borderColor0;
            var needOutBorderColor = !ChartHelper.IsClearColor(borderColor2);
            if (titleHig > 0)
            {
                var halfWid = parent.width / 2 + c_SubRectExtraWid;
                var halfHig = parent.height / 2 + c_SubRectExtraWid;
                parent = Rect.MinMaxRect(parent.center.x - halfWid, parent.center.y - halfHig,
                    parent.center.x + halfWid, parent.center.y + halfHig);
            }
            foreach (var serieData in data)
            {
                if (titleHig > 0 && !needOutBorderColor)
                {
                    var center = serieData.context.rect.center;
                    var halfWid = serieData.context.rect.width / 2;
                    var halfHig = serieData.context.rect.height / 2;

                    var ltLt = new Vector3(center.x - halfWid - border, center.y + halfHig + border);
                    var ltRt = new Vector3(center.x - halfWid, center.y + halfHig + border);
                    var ltRb = new Vector3(center.x - halfWid, center.y + halfHig);
                    var ltLb = new Vector3(center.x - halfWid - border, center.y + halfHig);

                    var rtLt = new Vector3(center.x + halfWid, center.y + halfHig + border);
                    var rtRb = new Vector3(center.x + halfWid + border, center.y + halfHig);
                    var rtLb = new Vector3(center.x + halfWid, center.y + halfHig);

                    var rbLt = new Vector3(center.x + halfWid, center.y - halfHig);
                    var rbRt = new Vector3(center.x + halfWid + border, center.y - halfHig);
                    var rbRb = new Vector3(center.x + halfWid + border, center.y - halfHig - border);
                    var rbLb = new Vector3(center.x + halfWid, center.y - halfHig - border);

                    var lbLt = new Vector3(center.x - halfWid - border, center.y - halfHig);
                    var lbRt = new Vector3(center.x - halfWid, center.y - halfHig);
                    var lbRb = new Vector3(center.x - halfWid, center.y - halfHig - border);
                    var lbLb = new Vector3(center.x - halfWid - border, center.y - halfHig - border);

                    DrawClampBorder(vh, parent, ltLt, ltRt, ltRb, ltLb, borderColor);
                    DrawClampBorder(vh, parent, ltRt, rtLt, rtLb, ltRb, borderColor);
                    DrawClampBorder(vh, parent, rtLb, rtRb, rbRt, rbLt, borderColor);
                    DrawClampBorder(vh, parent, rtRb, rbRt, rbLt, rtLb, borderColor);
                    DrawClampBorder(vh, parent, rbLt, rbRt, rbRb, rbLb, borderColor);
                    DrawClampBorder(vh, parent, lbRt, rbLt, rbLb, lbRb, borderColor);
                    DrawClampBorder(vh, parent, lbLt, lbRt, lbRb, lbLb, borderColor);
                }
                else
                {
                    UGL.DrawBorder(vh, serieData.context.rect, border, borderColor);
                }
                if (serieData.context.children.Count > 0)
                {
                    DrawBorder(vh, serieData.context.rect, serie, serieData.context.children, level + 1);
                }
            }
        }

        private void DrawClampBorder(VertexHelper vh, Rect parent, Vector3 lt, Vector3 rt, Vector3 rb, Vector3 lb, Color32 color)
        {
            if (parent.Contains(lt) && parent.Contains(rt) && parent.Contains(rb) && parent.Contains(lb))
                UGL.DrawQuadrilateral(vh, lt, rt, rb, lb, color);
        }

        private void Squarify(Treemap serie, Rect emptyArea, SerieData parent, List<SerieData> children, int level)
        {
            double total = 0d;
            double totalArea = emptyArea.width * emptyArea.height;
            foreach (var child in children)
                total += child.GetData(serie.visualDimension);
            foreach (var child in children)
            {
                child.context.level = level;
                child.context.area = total == 0 ? 0 : (child.GetData(serie.visualDimension) / total * totalArea);
            }

            Squarify(serie, emptyArea, children, new List<SerieData>(), Math.Min(emptyArea.width, emptyArea.height));

            foreach (var child in children)
            {
                child.context.parent = parent;
                if (child.context.children.Count > 0)
                {
                    var upperLabel = serie.GetLevelUpperLabel(level);
                    var titleHig = upperLabel.show ? upperLabel.distance : 0;
                    if (titleHig > 0)
                    {
                        Squarify(serie, GetCutTitleLabelRect(child.context.rect, titleHig), child, child.context.children, level + 1);
                    }
                    else
                    {
                        Squarify(serie, child.context.rect, child, child.context.children, level + 1);
                    }
                }
            }
        }

        private Rect GetCutTitleLabelRect(Rect sourRect, float titleHig)
        {
            if (titleHig <= 0) return sourRect;
            var diffWid = Math.Max(0, sourRect.width / 2 - titleHig);
            var diffHig = Math.Max(0, sourRect.height / 2 - titleHig);
            sourRect = Rect.MinMaxRect(sourRect.center.x - diffWid, sourRect.center.y - diffHig,
                sourRect.center.x + diffWid, sourRect.center.y + diffHig);
            return sourRect;
        }

        private void Squarify(Serie serie, Rect emptyArea, List<SerieData> children, List<SerieData> row, double w)
        {
            if (w <= 0) return;
            if (children.Count <= 0)
            {
                LayoutRow(serie, emptyArea, row);
                return;
            }

            SerieData head = children[0];
            var newrows = new List<SerieData>(row);
            newrows.Add(head);

            var wor1 = Worst(row, w);
            var wor2 = Worst(newrows, w);
            if (row.Count == 0 || wor1 > wor2)
            {
                var tempChildren = new List<SerieData>();
                for (int i = 1; i < children.Count; i++)
                    tempChildren.Add(children[i]);
                Squarify(serie, emptyArea, tempChildren, newrows, w);
            }
            else
            {
                emptyArea = LayoutRow(serie, emptyArea, row);
                Squarify(serie, emptyArea, children, new List<SerieData>(), Math.Min(emptyArea.width, emptyArea.height));
            }
        }

        private Rect LayoutRow(Serie serie, Rect emptyArea, List<SerieData> row)
        {
            Rect result;
            double areaUsed = 0;
            foreach (var r in row)
                areaUsed += r.context.area;

            if (emptyArea.width > emptyArea.height)
            {
                float w = (float)(areaUsed / emptyArea.height);
                result = new Rect(emptyArea.x + w, emptyArea.y, Math.Max(0, emptyArea.width - w), emptyArea.height);

                float y = emptyArea.y;
                foreach (var r in row)
                {
                    var h = (float)(r.context.area * emptyArea.height / areaUsed);
                    r.context.rect = new Rect(emptyArea.x, y, w, h);
                    y += h;
                }
            }
            else
            {
                float h = (float)(areaUsed / emptyArea.width);
                result = new Rect(emptyArea.x, emptyArea.y + h, emptyArea.width, Math.Max(0, emptyArea.height - h));

                float x = emptyArea.x;
                foreach (var r in row)
                {
                    var w = (float)(r.context.area * emptyArea.width / areaUsed);
                    r.context.rect = new Rect(x, emptyArea.y, w, h);
                    x += w;
                }
            }
            return result;
        }

        private double Worst(List<SerieData> row, double sideLength)
        {
            if (row.Count == 0) return 0;
            double maxArea = 0, minArea = double.MaxValue;
            double totalArea = 0;
            foreach (var n in row)
            {
                var area = n.context.area;
                if (area > maxArea) maxArea = area;
                if (area < minArea) minArea = area;
                totalArea += area;
            }
            double v1 = (sideLength * sideLength * maxArea) / (totalArea * totalArea);
            double v2 = (totalArea * totalArea) / (sideLength * sideLength * minArea);
            return Math.Max(v1, v2);
        }
    }
}