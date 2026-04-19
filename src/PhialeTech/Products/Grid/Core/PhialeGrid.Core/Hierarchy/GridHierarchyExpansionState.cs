using System.Collections.Generic;

namespace PhialeGrid.Core.Hierarchy
{
    public sealed class GridHierarchyExpansionState
    {
        private readonly HashSet<string> _expandedNodeIds = new HashSet<string>();

        public bool IsExpanded(string nodeId)
        {
            return _expandedNodeIds.Contains(nodeId);
        }

        public void SetExpanded(string nodeId, bool expanded)
        {
            if (expanded)
            {
                _expandedNodeIds.Add(nodeId);
            }
            else
            {
                _expandedNodeIds.Remove(nodeId);
            }
        }
    }
}
