using System;
using System.Threading;
using System.Threading.Tasks;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using UniversalInput.Contracts;

namespace PhialeTech.ComponentHost.Presentation
{
    public sealed class HostedSurfaceManager : IHostedSurfaceManager
    {
        private readonly object _syncRoot = new object();
        private readonly IHostedShellCoordinator _shellCoordinator;

        private HostedSurfaceSessionState _currentSession;
        private TaskCompletionSource<IHostedSurfaceResult> _currentCompletionSource;
        private CancellationTokenRegistration _currentCancellationRegistration;

        public HostedSurfaceManager(IHostedShellCoordinator shellCoordinator = null)
        {
            _shellCoordinator = shellCoordinator ?? new NullHostedShellCoordinator();
        }

        public IHostedSurfaceSessionState CurrentSession
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentSession;
                }
            }
        }

        public event EventHandler CurrentSessionChanged;

        public Task<IHostedSurfaceResult> ShowAsync(IHostedSurfaceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request is HostedSurfaceRequest mutableRequest)
            {
                mutableRequest.Validate();
            }

            HostedSurfaceSessionState session;
            TaskCompletionSource<IHostedSurfaceResult> completionSource;

            lock (_syncRoot)
            {
                if (_currentSession != null)
                {
                    throw new InvalidOperationException("Only one hosted modal session can be active at a time.");
                }

                _shellCoordinator.PrepareForPresentation(request);

                session = new HostedSurfaceSessionState
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    Request = request,
                    OpenedAtUtc = DateTimeOffset.UtcNow,
                };

                completionSource = new TaskCompletionSource<IHostedSurfaceResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _currentSession = session;
                _currentCompletionSource = completionSource;

                if (cancellationToken.CanBeCanceled)
                {
                    _currentCancellationRegistration = cancellationToken.Register(() => TryDismissCurrent(HostedSurfaceCommandIds.Dismiss));
                }
            }

            RaiseCurrentSessionChanged();
            return completionSource.Task;
        }

        public bool TryConfirmCurrent(string commandId = null, string payload = null)
        {
            return TryCompleteCurrent(HostedSurfaceResultOutcome.Confirmed, commandId ?? HostedSurfaceCommandIds.Confirm, payload);
        }

        public bool TryCancelCurrent(string commandId = null, string payload = null)
        {
            return TryCompleteCurrent(HostedSurfaceResultOutcome.Cancelled, commandId ?? HostedSurfaceCommandIds.Cancel, payload);
        }

        public bool TryDismissCurrent(string commandId = null, string payload = null)
        {
            return TryCompleteCurrent(HostedSurfaceResultOutcome.Dismissed, commandId ?? HostedSurfaceCommandIds.Dismiss, payload);
        }

        public void HandleCommand(UniversalCommandEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (string.Equals(e.CommandId, HostedSurfaceCommandIds.Confirm, StringComparison.OrdinalIgnoreCase))
            {
                TryConfirmCurrent(e.CommandId, BuildPayload(e));
                return;
            }

            if (string.Equals(e.CommandId, HostedSurfaceCommandIds.Cancel, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.CommandId, HostedSurfaceCommandIds.Close, StringComparison.OrdinalIgnoreCase))
            {
                TryCancelCurrent(e.CommandId, BuildPayload(e));
                return;
            }

            if (string.Equals(e.CommandId, HostedSurfaceCommandIds.Dismiss, StringComparison.OrdinalIgnoreCase))
            {
                if (CanDismissCurrent())
                {
                    TryDismissCurrent(e.CommandId, BuildPayload(e));
                }

                return;
            }

            if (string.Equals(e.CommandId, HostedSurfaceCommandIds.Backdrop, StringComparison.OrdinalIgnoreCase))
            {
                if (CanDismissCurrent())
                {
                    TryDismissCurrent(e.CommandId, BuildPayload(e));
                }
            }
        }

        public void HandleKey(UniversalKeyEventArgs e)
        {
            if (e == null || !e.IsKeyDown)
            {
                return;
            }

            if (string.Equals(e.Key, "Escape", StringComparison.OrdinalIgnoreCase) && CanDismissCurrent())
            {
                e.Handled = TryDismissCurrent(HostedSurfaceCommandIds.Dismiss);
            }
        }

        public void HandlePointer(UniversalPointerRoutedEventArgs e)
        {
        }

        public void HandleFocus(UniversalFocusChangedEventArgs e)
        {
        }

        private bool CanDismissCurrent()
        {
            lock (_syncRoot)
            {
                return _currentSession != null && _currentSession.Request != null && _currentSession.Request.CanDismiss;
            }
        }

        private bool TryCompleteCurrent(HostedSurfaceResultOutcome outcome, string commandId, string payload)
        {
            HostedSurfaceSessionState session;
            TaskCompletionSource<IHostedSurfaceResult> completionSource;
            HostedSurfaceResult result;

            lock (_syncRoot)
            {
                if (_currentSession == null || _currentCompletionSource == null)
                {
                    return false;
                }

                session = _currentSession;
                completionSource = _currentCompletionSource;
                result = new HostedSurfaceResult
                {
                    SessionId = session.SessionId,
                    Outcome = outcome,
                    CommandId = commandId ?? string.Empty,
                    Payload = payload ?? string.Empty,
                };

                _currentSession = null;
                _currentCompletionSource = null;

                _currentCancellationRegistration.Dispose();
                _currentCancellationRegistration = default;
            }

            _shellCoordinator.CompletePresentation(session.Request, result);
            completionSource.TrySetResult(result);
            RaiseCurrentSessionChanged();
            return true;
        }

        private void RaiseCurrentSessionChanged()
        {
            CurrentSessionChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string BuildPayload(UniversalCommandEventArgs e)
        {
            if (e == null || e.Arguments == null || e.Arguments.Count == 0)
            {
                return string.Empty;
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (var pair in e.Arguments)
            {
                parts.Add(pair.Key + "=" + pair.Value);
            }

            return string.Join(";", parts);
        }
    }
}
