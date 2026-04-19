using System;
using UniversalInput.Contracts;

namespace PhialeGrid.Core.Input
{
    public sealed class GridHeaderDragController
    {
        private string _columnId = string.Empty;
        private UniversalPoint? _startPosition;

        public void BeginHeaderPointerPressed(UniversalPointerRoutedEventArgs args, string columnId)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            _columnId = columnId;
            _startPosition = args.Pointer == null ? default(UniversalPoint?) : args.Pointer.Position;
        }

        public GridHeaderDragAction HandleHeaderPointerMoved(UniversalPointerRoutedEventArgs args, double minimumVerticalDragDistance)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (minimumVerticalDragDistance <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumVerticalDragDistance));
            }

            if (_startPosition == null || string.IsNullOrWhiteSpace(_columnId))
            {
                return GridHeaderDragAction.None;
            }

            if (args.Pointer?.Properties?.IsLeftButtonPressed != true)
            {
                Reset();
                return GridHeaderDragAction.None;
            }

            var startPosition = _startPosition.Value;
            var currentPosition = args.Pointer.Position;
            var horizontalDistance = Math.Abs(currentPosition.X - startPosition.X);
            var upwardDistance = startPosition.Y - currentPosition.Y;
            if (upwardDistance < minimumVerticalDragDistance || upwardDistance <= horizontalDistance)
            {
                return GridHeaderDragAction.None;
            }

            var action = new GridHeaderDragAction(GridHeaderDragActionKind.BeginGroupingDrag, _columnId);
            Reset();
            args.Handled = true;
            return action;
        }

        public void HandleHeaderPointerReleased(UniversalPointerRoutedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Reset();
        }

        public void Cancel()
        {
            Reset();
        }

        private void Reset()
        {
            _columnId = string.Empty;
            _startPosition = null;
        }
    }
}
