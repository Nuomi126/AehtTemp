
using XCharts.Runtime.Gauges;

namespace XCharts.Editor.Gauges
{
    [SerieEditor(typeof(Gauge))]
    public class GaugeEditor : SerieEditor<Gauge>
    {
        public override void OnCustomInspectorGUI()
        {
            PropertyTwoFiled("m_Center");
            PropertyTwoFiled("m_Radius");
            PropertyField("m_Min");
            PropertyField("m_Max");
            PropertyField("m_StartAngle");
            PropertyField("m_EndAngle");
            PropertyFieldLimitMax("m_SplitNumber", 36);
            PropertyField("m_RoundCap");
            PropertyField("m_Loop");
            PropertyField("m_RangeDataMode");

            PropertyField("m_GaugeAxis");
            PropertyField("m_GaugePointer");
            PropertyField("m_GaugeAnchor");
            PropertyField("m_ItemStyle");
            PropertyField("m_Animation");
        }
    }
}