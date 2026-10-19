using UnityEngine;

namespace XCharts.Runtime.Graphs
{
    /// <summary>
    /// The force layout of the graph.
    /// ||图表的力引导布局。
    /// </summary>
    [System.Serializable]
    public class Force : ChildComponent
    {
        [SerializeField] protected bool m_Show = true;
        [SerializeField] private float m_Repulsion = 1; //50
        [SerializeField] private float m_Gravity = 3f; //0.1f;
        [SerializeField] private float m_StepFactor = 1f;
        [SerializeField] private int m_StepCount = 1;

        [SerializeField][Range(0, 1f)] private float m_Friction = 0.6f;
        [SerializeField] private float m_EdgeLength = 80;
        [SerializeField] private int m_CenterDataIndex = -1;
        [SerializeField] private int m_Iterations = 200;
        
        public int currentIterations { get; internal set; }



        /// <summary>
        /// Whether to show the force layout.
        /// ||是否显示力引导布局。
        /// <summary>  
        public bool show
        {
            get { return m_Show; }
            set { m_Show = value; }
        }
        /// <summary>
        /// the repulsion between nodes.
        /// ||节点之间的斥力。
        /// </summary>
        public float repulsion
        {
            get { return m_Repulsion; }
            set { m_Repulsion = value; }
        }
        /// <summary>
        /// the gravity factor of the node to the center. The larger the value, the closer the node is to the center.
        /// ||节点受到的向中心的引力因子。该值越大节点越往中心点靠拢。
        /// </summary>
        public float gravity
        {
            get { return m_Gravity; }
            set { m_Gravity = value; }
        }
        /// <summary>
        /// the friction factor of the node movement, the larger the value, the slower the node movement.
        /// ||节点移动的摩擦因子，该值越大节点移动越慢。
        /// </summary>
        public float friction
        {
            get { return m_Friction; }
            set { m_Friction = value; }
        }
        /// <summary>
        /// the distance between the two nodes of the edge, this distance will also be affected by repulsion.
        /// ||边的两个节点之间的距离，这个距离也会受 repulsion 影响。
        /// </summary>
        public float edgeLength
        {
            get { return m_EdgeLength; }
            set { m_EdgeLength = value; }
        }
        /// <summary>
        /// The number of iterations of the force layout.
        /// ||力引导布局的迭代次数。
        /// </summary>
        public int iterations
        {
            get { return m_Iterations; }
            set { m_Iterations = value; }
        }
        /// <summary>
        /// the index of the center node data.
        /// ||中心节点数据的索引。
        /// </summary>
        public int centerDataIndex
        {
            get { return m_CenterDataIndex; }
            set { m_CenterDataIndex = value; }
        }
        /// <summary>
        /// The step factor of the force layout.
        /// ||力引导布局的步进因子。
        /// </summary>
        public float stepFactor
        {
            get { return m_StepFactor; }
            set { m_StepFactor = value; }
        }
        public int stepCount
        {
            get { return m_StepCount; }
            set { m_StepCount = value; }
        }
        
    }
}