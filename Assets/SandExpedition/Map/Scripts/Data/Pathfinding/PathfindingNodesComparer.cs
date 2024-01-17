using System.Collections.Generic;

namespace SCM.Map.Pathfinding
{
    public class PathfindingNodesComparer : IComparer<int>
    {
        DPathfindingNodeFast[] m;

        public PathfindingNodesComparer(
            DPathfindingNodeFast[] nodes)
        {
            m = nodes;
        }

        public int Compare(
            int a, int b)
        {
            if (m[a].priority > m[b].priority)
            {
                return 1;
            }
            else if (m[a].priority < m[b].priority)
            {
                return -1;
            }
            return 0;
        }

        public void SetMatrix(
            DPathfindingNodeFast[] nodes)
        {
            m = nodes;
        }
    }
}