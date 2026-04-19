using System;
using System.Collections.Generic;
using PhialeGrid.Core.Input;
using PhialeGrid.Core.Query;

namespace PhialeGrid.Core
{
    public sealed class GridEvents
    {
        public GridEventExceptionPolicy ExceptionPolicy { get; set; } = GridEventExceptionPolicy.Throw;

        public event EventHandler<GridHandlerExceptionEventArgs> HandlerException;

        public event EventHandler<GridOperationEventArgs<GridCellPosition>> CellPreparing;

        public event EventHandler<GridOperationEventArgs<GridCellPosition>> CellPrepared;

        public event EventHandler<GridCancelableEventArgs<string>> EditorOpening;

        public event EventHandler<GridOperationEventArgs<string>> EditorOpened;

        public event EventHandler<GridCancelableEventArgs<IReadOnlyList<GridSortDescriptor>>> Sorting;

        public event EventHandler<GridOperationEventArgs<IReadOnlyList<GridSortDescriptor>>> Sorted;

        public event EventHandler<GridCancelableEventArgs<GridFilterGroup>> Filtering;

        public event EventHandler<GridOperationEventArgs<GridFilterGroup>> Filtered;

        public event EventHandler<GridCancelableEventArgs<IReadOnlyList<GridGroupDescriptor>>> Grouping;

        public event EventHandler<GridOperationEventArgs<IReadOnlyList<GridGroupDescriptor>>> Grouped;

        public event EventHandler<GridCancelableEventArgs<GridSelectionState>> SelectionChanging;

        public event EventHandler<GridOperationEventArgs<GridSelectionState>> SelectionChanged;

        public void RaiseCellPreparing(GridCellPosition position)
        {
            Raise(CellPreparing, new GridOperationEventArgs<GridCellPosition>(position), "CellPreparing");
        }

        public void RaiseCellPrepared(GridCellPosition position)
        {
            Raise(CellPrepared, new GridOperationEventArgs<GridCellPosition>(position), "CellPrepared");
        }

        public bool RaiseEditorOpening(string columnId)
        {
            return RaiseCancelable(EditorOpening, new GridCancelableEventArgs<string>(columnId), "EditorOpening");
        }

        public void RaiseEditorOpened(string columnId)
        {
            Raise(EditorOpened, new GridOperationEventArgs<string>(columnId), "EditorOpened");
        }

        public bool RaiseSorting(IReadOnlyList<GridSortDescriptor> sortDescriptors)
        {
            return RaiseCancelable(Sorting, new GridCancelableEventArgs<IReadOnlyList<GridSortDescriptor>>(sortDescriptors), "Sorting");
        }

        public void RaiseSorted(IReadOnlyList<GridSortDescriptor> sortDescriptors)
        {
            Raise(Sorted, new GridOperationEventArgs<IReadOnlyList<GridSortDescriptor>>(sortDescriptors), "Sorted");
        }

        public bool RaiseFiltering(GridFilterGroup filterGroup)
        {
            return RaiseCancelable(Filtering, new GridCancelableEventArgs<GridFilterGroup>(filterGroup), "Filtering");
        }

        public void RaiseFiltered(GridFilterGroup filterGroup)
        {
            Raise(Filtered, new GridOperationEventArgs<GridFilterGroup>(filterGroup), "Filtered");
        }

        public bool RaiseGrouping(IReadOnlyList<GridGroupDescriptor> groupDescriptors)
        {
            return RaiseCancelable(Grouping, new GridCancelableEventArgs<IReadOnlyList<GridGroupDescriptor>>(groupDescriptors), "Grouping");
        }

        public void RaiseGrouped(IReadOnlyList<GridGroupDescriptor> groupDescriptors)
        {
            Raise(Grouped, new GridOperationEventArgs<IReadOnlyList<GridGroupDescriptor>>(groupDescriptors), "Grouped");
        }

        public bool RaiseSelectionChanging(GridSelectionState state)
        {
            return RaiseCancelable(SelectionChanging, new GridCancelableEventArgs<GridSelectionState>(state), "SelectionChanging");
        }

        public void RaiseSelectionChanged(GridSelectionState state)
        {
            Raise(SelectionChanged, new GridOperationEventArgs<GridSelectionState>(state), "SelectionChanged");
        }

        private bool RaiseCancelable<TPayload>(EventHandler<GridCancelableEventArgs<TPayload>> handler, GridCancelableEventArgs<TPayload> args, string eventName)
        {
            if (handler == null)
            {
                return !args.Cancel;
            }

            foreach (EventHandler<GridCancelableEventArgs<TPayload>> single in handler.GetInvocationList())
            {
                try
                {
                    single(this, args);
                }
                catch (Exception ex)
                {
                    HandleException(eventName, ex);
                }

                if (args.Cancel)
                {
                    break;
                }
            }

            return !args.Cancel;
        }

        private void Raise<TPayload>(EventHandler<GridOperationEventArgs<TPayload>> handler, GridOperationEventArgs<TPayload> args, string eventName)
        {
            if (handler == null)
            {
                return;
            }

            foreach (EventHandler<GridOperationEventArgs<TPayload>> single in handler.GetInvocationList())
            {
                try
                {
                    single(this, args);
                }
                catch (Exception ex)
                {
                    HandleException(eventName, ex);
                }
            }
        }

        private void HandleException(string eventName, Exception exception)
        {
            var handler = HandlerException;
            if (handler != null)
            {
                handler(this, new GridHandlerExceptionEventArgs(eventName, exception));
            }

            if (ExceptionPolicy == GridEventExceptionPolicy.Throw)
            {
                throw exception;
            }
        }
    }
}
