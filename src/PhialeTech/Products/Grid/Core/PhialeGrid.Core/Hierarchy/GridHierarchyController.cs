using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Hierarchy
{
    public sealed class GridHierarchyController<T>
    {
        private readonly IGridHierarchyProvider<T> _provider;
        private readonly IGridHierarchyPagingProvider<T> _pagingProvider;
        private readonly GridHierarchyExpansionState _expansionState;
        private readonly int _pageSize;

        public GridHierarchyController(IGridHierarchyProvider<T> provider, GridHierarchyExpansionState expansionState = null, int pageSize = 50)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _pagingProvider = provider as IGridHierarchyPagingProvider<T>;
            _expansionState = expansionState ?? new GridHierarchyExpansionState();
            _pageSize = pageSize;
        }

        public async Task ExpandAsync(GridHierarchyNode<T> node, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!node.CanExpand)
            {
                return;
            }

            if (!node.IsChildrenLoaded)
            {
                await LoadChildrenInternalAsync(node, 0, cancellationToken).ConfigureAwait(false);
            }

            node.IsExpanded = true;
            _expansionState.SetExpanded(node.PathId, true);
        }

        public async Task LoadNextChildrenPageAsync(GridHierarchyNode<T> node, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (_pagingProvider == null || !node.HasMoreChildren)
            {
                return;
            }

            await LoadChildrenInternalAsync(node, node.LoadedChildrenCount, cancellationToken).ConfigureAwait(false);
        }

        public void Collapse(GridHierarchyNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.IsExpanded = false;
            _expansionState.SetExpanded(node.PathId, false);
        }

        public IReadOnlyList<GridHierarchyNode<T>> Flatten(IReadOnlyList<GridHierarchyNode<T>> roots)
        {
            var result = new List<GridHierarchyNode<T>>();
            if (roots == null)
            {
                return result;
            }

            foreach (var root in roots)
            {
                ApplyExpansion(root);
                Traverse(root, result);
            }

            return result;
        }

        private async Task LoadChildrenInternalAsync(GridHierarchyNode<T> node, int offset, CancellationToken cancellationToken)
        {
            if (_pagingProvider != null)
            {
                var page = await _pagingProvider.LoadChildrenPageAsync(node, offset, _pageSize, cancellationToken).ConfigureAwait(false);
                var children = node.Children.ToList();
                children.AddRange(page.Items);
                node.Children = children.ToArray();
                node.LoadedChildrenCount = children.Count;
                node.HasMoreChildren = page.HasMore;
                node.IsChildrenLoaded = true;
                return;
            }

            node.Children = await _provider.LoadChildrenAsync(node, cancellationToken).ConfigureAwait(false);
            node.LoadedChildrenCount = node.Children.Count;
            node.HasMoreChildren = false;
            node.IsChildrenLoaded = true;
        }

        private void ApplyExpansion(GridHierarchyNode<T> node)
        {
            node.IsExpanded = _expansionState.IsExpanded(node.PathId);
            foreach (var child in node.Children)
            {
                ApplyExpansion(child);
            }
        }

        private void Traverse(GridHierarchyNode<T> node, List<GridHierarchyNode<T>> result)
        {
            result.Add(node);
            if (!node.IsExpanded)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                Traverse(child, result);
            }
        }
    }
}
