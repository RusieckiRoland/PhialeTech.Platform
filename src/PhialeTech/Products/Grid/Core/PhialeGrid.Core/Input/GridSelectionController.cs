using System;
using UniversalInput.Contracts;

namespace PhialeGrid.Core.Input
{
    public sealed class GridSelectionController
    {
        private GridCellPosition? _anchorCell;
        private bool _isDragSelecting;

        public GridSelectionController()
        {
            State = new GridSelectionState();
        }

        public GridSelectionState State { get; }

        public void HandlePointerPressed(UniversalPointerRoutedEventArgs args, GridCellPosition hitCell, bool isCtrlPressed, bool isShiftPressed)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (isShiftPressed && _anchorCell.HasValue)
            {
                State.ReplaceWithRange(_anchorCell.Value, hitCell);
            }
            else if (isCtrlPressed)
            {
                State.Toggle(hitCell);
                _anchorCell = hitCell;
            }
            else
            {
                State.SetSingle(hitCell);
                _anchorCell = hitCell;
            }

            _isDragSelecting = IsLeftPressed(args);
            args.Handled = true;
        }

        public void HandlePointerMoved(UniversalPointerRoutedEventArgs args, GridCellPosition hitCell)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (!_isDragSelecting || !_anchorCell.HasValue)
            {
                return;
            }

            if (!IsLeftPressed(args))
            {
                _isDragSelecting = false;
                return;
            }

            State.ReplaceWithRange(_anchorCell.Value, hitCell);
            args.Handled = true;
        }

        public void HandlePointerReleased(UniversalPointerRoutedEventArgs args, GridCellPosition hitCell)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (_isDragSelecting && _anchorCell.HasValue)
            {
                State.ReplaceWithRange(_anchorCell.Value, hitCell);
            }

            _isDragSelecting = false;
            args.Handled = true;
        }

        private static bool IsLeftPressed(UniversalPointerRoutedEventArgs args)
        {
            return args.Pointer?.Properties?.IsLeftButtonPressed == true;
        }
    }
}
