using System.Collections.Generic;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupExpansionState
    {
        private readonly HashSet<string> _collapsedGroups = new HashSet<string>();

        public bool IsExpanded(string groupId)
        {
            return !_collapsedGroups.Contains(groupId);
        }

        public void SetExpanded(string groupId, bool isExpanded)
        {
            if (isExpanded)
            {
                _collapsedGroups.Remove(groupId);
            }
            else
            {
                _collapsedGroups.Add(groupId);
            }
        }
    }
}
