// PhialeGis.Library.Core/Interactions/GisInteractionManager.cs
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Localization;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using UniversalInput.Contracts;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Core.Modes;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.DslEditor.Contracts;
using PhialeGis.Library.DslEditor.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGis.Library.Core.Interactions
{
    /// <summary>
    /// Manages rendering viewports (IRenderingComposition) and owns a DSL editor manager.
    /// One manager per GIS session.
    /// Additionally, it wires DSL editors to concrete GraphicsFacade instances
    /// when editors expose a bound target (via IEditorTargetAware).
    /// </summary>
    public sealed class GisInteractionManager : IGisInteractionManager, IRenderSyncConfig, IDslEditorProvider
    {
        public event EventHandler<ViewportInteractionStatusChangedEventArgs> ViewportInteractionStatusChanged;

        // Registered viewports (rendering compositions) and their facades.
        private readonly Dictionary<IRenderingComposition, GraphicsFacade> _facades
            = new Dictionary<IRenderingComposition, GraphicsFacade>();

        private IPhRenderBackendFactory _backendFactory;

        // Editors wired to their concrete facades (per drawbox).
        private readonly Dictionary<IEditorInteractive, GraphicsFacade> _editorToFacade
            = new Dictionary<IEditorInteractive, GraphicsFacade>();

        // Editors waiting for their target drawbox to be registered.
        // Keys are compared by reference (opaque target objects).
        private readonly Dictionary<object, List<IEditorInteractive>> _pendingEditorsByTarget
            = new Dictionary<object, List<IEditorInteractive>>(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<IEditorInteractive, EventHandler<UniversalLanguageChangedEventArgs>> _editorLanguageHandlers
            = new Dictionary<IEditorInteractive, EventHandler<UniversalLanguageChangedEventArgs>>();

        // DSL editors manager (owned collaborator).
        private IDslEditorManager _dsl;

        // Expose editor manager (read-only).
        public IDslEditorManager Editors { get { return _dsl; } }

        // Carries the current DSL command target across async calls (per logical call context).
        private readonly AsyncLocal<object> _currentDslTarget = new AsyncLocal<object>();

        /// <summary>
        /// The render target (e.g., IRenderingComposition or facade) associated with the
        /// currently executing DSL command. Set right before calling the engine.
        /// </summary>
        public object CurrentDslTarget { get { return _currentDslTarget.Value; } set  { _currentDslTarget.Value = value; } }

        // Global BeforeRender hook propagated to all facades.
        private Action<IWorld, IViewport> _beforeRenderHook;

        private readonly object _gate = new object();

        /// <summary>Indicates whether real DSL providers are configured.</summary>
        public bool IsDslConfigured { get; private set; }

        public IDslContextProvider DslContextProvider { get; set; } = new DslContextRegistry();

        private readonly ActionSessionRegistry _sessions = new ActionSessionRegistry();
        private readonly Dictionary<object, ActionContextMenuPayload> _pendingContextMenuByTarget
            = new Dictionary<object, ActionContextMenuPayload>(ReferenceEqualityComparer.Instance);
        private double[] _sharedPreview = Array.Empty<double>();
        private SnapResult _activeSnapResult;

        private IActionResultCommitter _actionCommitter;
        private ISnapService _snapService;

        private readonly Dictionary<object, CursorState> _cursorByTarget
            = new Dictionary<object, CursorState>(ReferenceEqualityComparer.Instance);

        private CursorSpec _idleCursor;

        // No-op providers (safe defaults).
        private static readonly Func<string, DslCommandEnvelope, Task<DslResultDto>> _noopExec =
            (code, env) => Task.FromResult(new DslResultDto { Success = false, Output = null, Error = null });

        private static readonly Func<string, Task<DslValidationResultDto>> _noopValidate =
            code => Task.FromResult(new DslValidationResultDto
            {
                IsValid = true,
                Diagnostics = Array.Empty<DslDiagnosticDto>()
            });

        private static readonly Func<string, int, Task<DslCompletionListDto>> _noopCompletions =
            (code, caret) => Task.FromResult(new DslCompletionListDto
            {
                Items = Array.Empty<DslCompletionItemDto>(),
                IsIncomplete = false
            });

        /// <summary>
        /// Initializes a new instance of the <see cref="GisInteractionManager"/> class.
        /// </summary>
        /// <param name="backendFactory">
        /// Rendering backend factory used to create platform-specific render drivers.
        /// Must not be <c>null</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="backendFactory"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// The constructor also initializes the DSL interaction manager and
        /// wires the internal finite state machine to the DSL layer.
        /// </remarks>
        public GisInteractionManager(IPhRenderBackendFactory backendFactory)
        {
           
            _dsl = new DslInteractionManager();
            _backendFactory = backendFactory ?? throw new ArgumentNullException(nameof(backendFactory));
            IsDslConfigured = true;
        }
              
           

        // ------------------------------------------------------------
        // Context resolution
        // ------------------------------------------------------------
        /// <summary>
        /// Resolves a rendering context for the given target object.
        /// Returns true if a context is available. If <paramref name="target"/> is null,
        /// the first registered viewport is returned (if any).
        /// </summary>
        public bool TryResolveContext(object target, out IViewport viewport, out IGraphicsFacade graphics)
        {
            lock (_gate)
            {
                // Try exact match by composition or facade reference.
                if (target != null)
                {
                    foreach (var kv in _facades)
                    {
                        if (ReferenceEquals(kv.Key, target) || ReferenceEquals(kv.Value, target))
                        {
                            viewport = kv.Value.Viewport;
                            graphics = kv.Value;
                            return true;
                        }
                    }
                }

                // Fallback: first registered.
                foreach (var kv in _facades)
                {
                    viewport = kv.Value.Viewport;
                    graphics = kv.Value;
                    return true;
                }
            }

            viewport = null;
            graphics = null;
            return false;
        }

        // ------------------------------------------------------------
        // Registration API
        // ------------------------------------------------------------
        public void RegisterControl(IRenderingComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));

            RegisterControl((object)composition);
        }

        public void RegisterControl(IEditorInteractive editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            RegisterControl((object)editor);
        }

        public void RegisterControl(object compositionObj)
        {
            if (compositionObj == null)
                throw new ArgumentNullException("compositionObj");

            // 1) Rendering viewport path
            var rc = compositionObj as IRenderingComposition;
            if (rc != null)
            {
                lock (_gate)
                {
                    if (_facades.ContainsKey(rc))
                        return;

                    var facade = new GraphicsFacade(rc, _backendFactory);
                    if (_beforeRenderHook != null)
                        facade.BeforeRender = _beforeRenderHook;
                    facade.PreviewProvider = TryGetSharedPreview;
                    facade.SnapProvider = TryGetActiveSnapMarker;
                    facade.CursorProvider = () => TryGetCursorForTarget(rc);

                    _facades[rc] = facade;

                    // Try to attach any editors that were waiting for this target.
                    AttachPendingEditorsIfAny_NoLock(facade);

                    if (_idleCursor != null)
                        SetCursorForTarget_NoLock(rc, _idleCursor);
                }
                RaiseViewportStatusChanged(rc);
                return;
            }

            // 2) DSL editor path
            var editor = compositionObj as IEditorInteractive;
            if (editor != null)
            {
                // Always register editor in DSL manager.
                _dsl.RegisterEditor(editor);
                HookEditorLanguageEvents(editor);
                EnsureEditorLanguageInContext(editor);

                // If the editor exposes its target drawbox, try wiring it now.
                var targetAware = editor as IEditorTargetAware;
                var target = targetAware != null ? targetAware.TargetDraw : null;

                if (target != null)
                {
                    lock (_gate)
                    {
                        IViewport vp; IGraphicsFacade gfx;
                        if (TryResolveContext_NoLock(target, out vp, out gfx))
                        {
                            var facade = gfx as GraphicsFacade;
                            if (facade != null)
                            {
                                facade.AttachEditor(editor);
                                _editorToFacade[editor] = facade;
                            }
                        }
                        else
                        {
                            // The target drawbox is not registered yet; remember the editor for later.
                            List<IEditorInteractive> list;
                            if (!_pendingEditorsByTarget.TryGetValue(target, out list))
                            {
                                list = new List<IEditorInteractive>();
                                _pendingEditorsByTarget[target] = list;
                            }
                            list.Add(editor);
                        }
                    }
                }
                return;
            }

            // 3) Unknown type
            throw new ArgumentException(
                "compositionObj must implement either IRenderingComposition (viewport) or IEditorInteractive (DSL editor).",
                "compositionObj");
        }

        public void UnregisterControl(IRenderingComposition composition)
        {
            if (composition == null) return;
            UnregisterControl((object)composition);
        }

        public void UnregisterControl(IEditorInteractive editor)
        {
            if (editor == null) return;
            UnregisterControl((object)editor);
        }

        public void UnregisterControl(object compositionObj)
        {
            if (compositionObj == null) return;

            var rc = compositionObj as IRenderingComposition;
            if (rc != null)
            {
                var removed = false;
                lock (_gate)
                {
                    GraphicsFacade facade;
                    if (_facades.TryGetValue(rc, out facade))
                    {
                        // Detach any editors bound to this facade (best-effort).
                        if (facade != null)
                        {
                            // Remove reverse links
                            var toRemove = new List<IEditorInteractive>();
                            foreach (var kv in _editorToFacade)
                            {
                                if (ReferenceEquals(kv.Value, facade))
                                    toRemove.Add(kv.Key);
                            }
                            for (int i = 0; i < toRemove.Count; i++)
                            {
                                facade.DetachEditor(toRemove[i]);
                                _editorToFacade.Remove(toRemove[i]);
                            }
                        }

                        _facades.Remove(rc);
                        try { facade.Dispose(); } catch { /* swallow */ }
                        removed = true;
                    }
                }
                if (removed)
                    RaiseViewportStatusChanged(rc);
                return;
            }

            var editor = compositionObj as IEditorInteractive;
            if (editor != null)
            {
                UnhookEditorLanguageEvents(editor);
                _dsl.UnregisterEditor(editor);

                lock (_gate)
                {
                    GraphicsFacade bound;
                    if (_editorToFacade.TryGetValue(editor, out bound))
                    {
                        try { bound.DetachEditor(editor); } catch { /* swallow */ }
                        _editorToFacade.Remove(editor);
                    }

                    // Also remove from any pending queues
                    foreach (var kv in _pendingEditorsByTarget)
                    {
                        kv.Value.Remove(editor);
                    }
                }
                return;
            }

            // Unknown type: ignore (idempotent)
        }

        // ------------------------------------------------------------
        // Viewport-wide operations
        // ------------------------------------------------------------
        public void SetBeforeRenderHook(Action<IWorld, IViewport> hook)
        {
            lock (_gate)
            {
                _beforeRenderHook = hook;
                foreach (var kv in _facades)
                    kv.Value.BeforeRender = hook;
            }
        }

        public void ApplyVisualWindow(double x, double y, double width, double height)
        {
            
            lock (_gate)
            {
                CoreDiag.LogApplyDispatch(_facades.Count, x, y, width, height);

                int i = 0;
                foreach (var kv in _facades)
                {
                    // kv.Key to IRenderingComposition (device)
                    CoreDiag.LogDevicePerFacade(i++, kv.Key);
                    kv.Value.ApplyVisualWindow(x, y, width, height);
                }
            }
        }

        public void InvalidateAll()
        {
            lock (_gate)
            {
                Debug.WriteLine($"[CORE] InvalidateAll → {_facades.Count} facade(s)");
                foreach (var kv in _facades)
                    kv.Value.Invalidate();
            }
        }

        // ------------------------------------------------------------
        // Helpers (private)
        // ------------------------------------------------------------

        /// <summary>
        /// Tries to resolve a context without locking this manager (caller must hold _gate).
        /// </summary>
        private bool TryResolveContext_NoLock(object target, out IViewport viewport, out IGraphicsFacade graphics)
        {
            // Exact match by reference (composition or facade)
            if (target != null)
            {
                foreach (var kv in _facades)
                {
                    if (ReferenceEquals(kv.Key, target) || ReferenceEquals(kv.Value, target))
                    {
                        viewport = kv.Value.Viewport;
                        graphics = kv.Value;
                        return true;
                    }
                }
            }

            // Fallback: first registered
            foreach (var kv in _facades)
            {
                viewport = kv.Value.Viewport;
                graphics = kv.Value;
                return true;
            }

            viewport = null;
            graphics = null;
            return false;
        }

        /// <summary>
        /// Attaches any editors that were queued for the given facade's target.
        /// Caller must hold _gate.
        /// </summary>
        private void AttachPendingEditorsIfAny_NoLock(GraphicsFacade newlyRegisteredFacade)
        {
            if (_pendingEditorsByTarget.Count == 0 || newlyRegisteredFacade == null)
                return;

            // We don't know which opaque target object equals this facade's "key" for editors.
            // Just probe all pending keys through TryResolveContext_NoLock.
            var keysToRemove = new List<object>();

            foreach (var kv in _pendingEditorsByTarget)
            {
                IViewport vp; IGraphicsFacade gfx;
                if (TryResolveContext_NoLock(kv.Key, out vp, out gfx) && ReferenceEquals(gfx, newlyRegisteredFacade))
                {
                    var editors = kv.Value;
                    for (int i = 0; i < editors.Count; i++)
                    {
                        newlyRegisteredFacade.AttachEditor(editors[i]);
                        _editorToFacade[editors[i]] = newlyRegisteredFacade;
                    }
                    keysToRemove.Add(kv.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                _pendingEditorsByTarget.Remove(keysToRemove[i]);
        }

        /// <summary>
        /// Reference comparer for opaque target keys (compares by object identity).
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return RuntimeHelpers.GetHashCode(obj); }
        }

        // ==========================
        // [NOWE – minimalnie] API do akcji interaktywnych
        // ==========================

        /// <summary>
        /// Starts an application-level interactive action.
        /// </summary>
        public void StartInteractiveAction(IInteractionAction action, object targetDraw, IEditorInteractive source)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (DslContextProvider == null) throw new InvalidOperationException("IDslContextProvider not set.");

            if (targetDraw == null && source is IEditorTargetAware aware)
                targetDraw = aware.TargetDraw;
            if (targetDraw == null) throw new InvalidOperationException("TargetDraw is required for interactive actions.");

            var active = _sessions.GetActive();
            if (active != null && active.Fsm.State == MainInteractionFsm.FsmState.Running)
            {
                if (active.Fsm.CurrentAction != null && active.Fsm.CurrentAction.CanBeSuspended)
                    active.Fsm.Suspend("takeover");
                else
                    active.Fsm.Cancel();
            }

            var fsm = new MainInteractionFsm();
            var session = new ActionSession(source, targetDraw, fsm);

            fsm.OnChange += (s, payload) => HandleSessionChange(session, payload);
            fsm.OnFinish += (s, payload) => HandleSessionFinish(session, payload);
            fsm.OnSuspend += (s, payload) => HandleSessionSuspend(session, payload);

            _sessions.Add(session);
            ClearContextMenuForTarget(targetDraw);
            var currentCtx = DslContextProvider.GetFor(source);
            var languageId = DslUiLocalization.NormalizeLanguageId(currentCtx?.LanguageId);
            fsm.StartAction(action, targetDraw, languageId);

            ApplyDslContextForSession(session);

            var cursorProvider = action as IInteractionCursorProvider;
            var cursor = cursorProvider?.Cursor ?? _idleCursor;
            SetCursorForTarget(targetDraw, cursor);
            RaiseViewportStatusChangedForAll();
        }

        public bool TryHandleInteractiveInput(string line, IEditorInteractive source)
        {
            var session = _sessions.GetByEditor(source);
            if (session == null) return false;
            if (!_sessions.IsActive(session)) return false;
            if (session.Fsm.State != MainInteractionFsm.FsmState.Running) return false;

            session.Fsm.HandleInput(line ?? string.Empty);
            TryRefreshActionGhostFromCursor(session);
            RaiseViewportStatusChangedForAll();
            return true;
        }

        public bool CancelInteractiveAction(IEditorInteractive source)
        {
            var session = _sessions.GetByEditor(source);
            if (session == null) return false;
            if (!_sessions.IsActive(session)) return false;
            if (session.Fsm.State == MainInteractionFsm.FsmState.Idle) return false;

            session.Fsm.Cancel();
            return true;
        }

        public bool TryHandleInteractivePointerDown(ActionPointerInput input)
        {
            return TryHandlePointer(input, PointerPhase.Down);
        }

        public bool TryHandleInteractivePointerMove(ActionPointerInput input)
        {
            return TryHandlePointer(input, PointerPhase.Move);
        }

        public bool TryHandleInteractivePointerUp(ActionPointerInput input)
        {
            return TryHandlePointer(input, PointerPhase.Up);
        }

        public bool TryHandleInteractiveMenuCommand(object targetDraw, string commandId)
        {
            if (targetDraw == null) return false;
            if (string.IsNullOrWhiteSpace(commandId)) return false;

            var session = _sessions.GetActive();
            if (session == null) return false;
            if (session.Fsm.State != MainInteractionFsm.FsmState.Running) return false;
            if (!ReferenceEquals(session.InputTargetDraw, targetDraw)) return false;

            var menuHandler = session.Fsm.CurrentAction as IActionMenuCommandHandler;
            if (menuHandler == null) return false;

            var handled = menuHandler.TryHandleMenuCommand(commandId);
            if (handled)
                RaiseViewportStatusChangedForAll();

            return handled;
        }

        public bool TryConsumePendingContextMenu(object targetDraw, out ActionContextMenuPayload payload)
        {
            payload = null;
            if (targetDraw == null) return false;

            lock (_gate)
            {
                if (!_pendingContextMenuByTarget.TryGetValue(targetDraw, out var found) || found == null)
                    return false;

                _pendingContextMenuByTarget.Remove(targetDraw);
                payload = found;
                return true;
            }
        }

        public bool TryTakeoverInteractiveSession(object targetDraw)
        {
            if (targetDraw == null)
                return false;

            var active = _sessions.GetActive();
            if (active == null)
                return false;

            if (active.Fsm.State != MainInteractionFsm.FsmState.Running &&
                active.Fsm.State != MainInteractionFsm.FsmState.Suspended)
                return false;

            if (ReferenceEquals(active.InputTargetDraw, targetDraw))
                return false;

            SetCursorForTarget(active.InputTargetDraw, _idleCursor);
            _sessions.TransferInputTarget(active, targetDraw);

            if (active.Fsm.State == MainInteractionFsm.FsmState.Running)
            {
                var cursorProvider = active.Fsm.CurrentAction as IInteractionCursorProvider;
                var cursor = cursorProvider?.Cursor ?? _idleCursor;
                SetCursorForTarget(targetDraw, cursor);
                TryRefreshActionGhostFromCursor(active);
            }

            ClearContextMenuForTarget(targetDraw);
            RaiseViewportStatusChangedForAll();
            return true;
        }

        public bool TryGetViewportInteractionStatus(object targetDraw, out ViewportInteractionStatus status)
        {
            status = new ViewportInteractionStatus();

            var active = _sessions.GetActive();
            if (active == null)
            {
                status.CoordinateText = BuildCoordinateText(targetDraw);
                return true;
            }

            status.HasActiveSession = true;
            status.IsInputViewport = ReferenceEquals(active.InputTargetDraw, targetDraw);
            status.ActionName = active.Fsm.CurrentActionName ?? string.Empty;
            status.PromptText = active.LastPromptText ?? string.Empty;
            status.CoordinateText = BuildCoordinateText(targetDraw);
            status.SnapText = BuildSnapText(status.IsInputViewport);

            if (status.IsInputViewport)
            {
                var menu = TryGetActionMenuItems(active);
                status.Commands = menu ?? Array.Empty<ActionContextMenuItem>();
            }

            return true;
        }

        public void SetActionResultCommitter(IActionResultCommitter committer)
        {
            _actionCommitter = committer;
        }

        public void SetSnapService(ISnapService snapService)
        {
            _snapService = snapService;
            RaiseViewportStatusChangedForAll();
        }

        // Legacy overloads (temporary compatibility)
        public bool TryHandleInteractiveInput(string line)
            => TryHandleInteractiveInput(line, _dsl?.ActiveEditor);

        public bool CancelInteractiveAction()
            => CancelInteractiveAction(_dsl?.ActiveEditor);

        private bool TryHandlePointer(ActionPointerInput input, PointerPhase phase)
        {
            if (input == null || input.TargetDraw == null) return false;

            var session = _sessions.GetActive();
            if (session == null) return false;
            if (session.Fsm.State != MainInteractionFsm.FsmState.Running) return false;
            if (!ReferenceEquals(session.InputTargetDraw, input.TargetDraw)) return false;

            EnsureModelPosition(input);
            ApplySnap(input, phase);

            var handler = session.Fsm.CurrentAction as IActionPointerHandler;
            if (handler == null) return false;

            switch (phase)
            {
                case PointerPhase.Down:
                    if (handler.TryHandlePointerDown(input))
                    {
                        RaiseViewportStatusChangedForAll();
                        return true;
                    }
                    return false;
                case PointerPhase.Move:
                    if (handler.TryHandlePointerMove(input))
                    {
                        RaiseViewportStatusChangedForAll();
                        return true;
                    }
                    return false;
                case PointerPhase.Up:
                    if (handler.TryHandlePointerUp(input))
                    {
                        RaiseViewportStatusChangedForAll();
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private void HandleSessionChange(ActionSession session, object payloadObj)
        {
            if (session == null) return;

            var ap = payloadObj as ActionPromptPayload;
            if (ap?.Prompt != null)
            {
                session.LastPromptText = ap.Prompt.ModeText ?? string.Empty;
                try { _dsl.PushPromptForEditor(session.Editor, ap.Prompt); }
                catch { /* best-effort */ }
            }

            var cp = payloadObj as ActionChangePayload;
            if (cp != null)
            {
                UpdateSharedPreview(cp.Preview);
            }

            var mp = payloadObj as ActionContextMenuPayload;
            if (mp != null)
            {
                var target = mp.TargetDraw ?? session.InputTargetDraw;
                mp.TargetDraw = target;
                lock (_gate)
                {
                    _pendingContextMenuByTarget[target] = mp;
                }
            }

            RaiseViewportStatusChangedForAll();
        }

        private void HandleSessionFinish(ActionSession session, object payloadObj)
        {
            if (session == null) return;

            ClearSharedPreview();
            ClearActiveSnap();
            ClearAllContextMenus();
            ClearPromptForEditor(session.Editor);
            ResetEditorDslContext(session.Editor);
            SetCursorForTarget(session.InputTargetDraw, _idleCursor);

            var finish = payloadObj as ActionFinishPayload;
            if (finish != null && finish.Success && finish.Result != null)
            {
                try { _actionCommitter?.Commit(finish.Result); }
                catch { /* best-effort */ }
                InvalidateAll();
            }

            _sessions.Remove(session);
            ResumeSuspendedSessionIfAny();
            RaiseViewportStatusChangedForAll();
        }

        private void HandleSessionSuspend(ActionSession session, object payloadObj)
        {
            if (session == null) return;
            ClearSharedPreview();
            ClearActiveSnap();
            ClearAllContextMenus();
            ClearPromptForEditor(session.Editor);
            ResetEditorDslContext(session.Editor);
            SetCursorForTarget(session.InputTargetDraw, _idleCursor);
            RaiseViewportStatusChangedForAll();
        }

        private void ResumeSuspendedSessionIfAny()
        {
            var active = _sessions.GetActive();
            if (active == null) return;
            if (active.Fsm.State != MainInteractionFsm.FsmState.Suspended) return;

            ApplyDslContextForSession(active);
            active.Fsm.Resume();

            var cursorProvider = active.Fsm.CurrentAction as IInteractionCursorProvider;
            var cursor = cursorProvider?.Cursor ?? _idleCursor;
            SetCursorForTarget(active.InputTargetDraw, cursor);
            TryRefreshActionGhostFromCursor(active);
        }

        private void ApplyDslContextForSession(ActionSession session)
        {
            if (session == null) return;
            if (session.Editor == null) return;
            var mode = session.Fsm.RequiredMode;
            var current = DslContextProvider.GetFor(session.Editor);
            var languageId = DslUiLocalization.NormalizeLanguageId(current?.LanguageId);

            DslContextProvider.SetFor(session.Editor, new DslContext
            {
                Mode = mode,
                ActionId = session.Fsm.CurrentActionId,
                ActionName = session.Fsm.CurrentActionName,
                TargetDraw = session.InputTargetDraw,
                LanguageId = languageId
            });
        }

        private void HookEditorLanguageEvents(IEditorInteractive editor)
        {
            if (editor == null) return;

            lock (_gate)
            {
                if (_editorLanguageHandlers.ContainsKey(editor))
                    return;

                EventHandler<UniversalLanguageChangedEventArgs> handler = (s, e) =>
                {
                    var languageId = DslUiLocalization.NormalizeLanguageId(e?.LanguageId);
                    UpdateEditorLanguageInContext(editor, languageId);
                };

                _editorLanguageHandlers[editor] = handler;
                editor.LanguageChangedUniversal += handler;
            }
        }

        private void UnhookEditorLanguageEvents(IEditorInteractive editor)
        {
            if (editor == null) return;

            lock (_gate)
            {
                EventHandler<UniversalLanguageChangedEventArgs> handler;
                if (!_editorLanguageHandlers.TryGetValue(editor, out handler))
                    return;

                editor.LanguageChangedUniversal -= handler;
                _editorLanguageHandlers.Remove(editor);
            }
        }

        private void EnsureEditorLanguageInContext(IEditorInteractive editor)
        {
            if (editor == null) return;

            var current = DslContextProvider.GetFor(editor);
            if (!string.IsNullOrWhiteSpace(current?.LanguageId))
                return;

            var languageId = DslUiLocalization.NormalizeLanguageId(null);
            UpdateEditorLanguageInContext(editor, languageId);
        }

        private void UpdateEditorLanguageInContext(IEditorInteractive editor, string languageId)
        {
            if (editor == null || DslContextProvider == null) return;

            var lang = DslUiLocalization.NormalizeLanguageId(languageId);
            var current = DslContextProvider.GetFor(editor) ?? new DslContext();

            DslContextProvider.SetFor(editor, new DslContext
            {
                Mode = current.Mode,
                ActionId = current.ActionId,
                ActionName = current.ActionName,
                TargetDraw = current.TargetDraw,
                LanguageId = lang
            });
        }

        private void ResetEditorDslContext(IEditorInteractive editor)
        {
            if (editor == null || DslContextProvider == null) return;

            var current = DslContextProvider.GetFor(editor);
            var languageId = DslUiLocalization.NormalizeLanguageId(current?.LanguageId);

            DslContextProvider.SetFor(editor, new DslContext
            {
                Mode = DslMode.Normal,
                LanguageId = languageId
            });
        }

        private void ClearPromptForEditor(IEditorInteractive editor)
        {
            if (editor == null)
                return;

            try
            {
                _dsl.PushPromptForEditor(editor, new DslPromptDto
                {
                    ModeText = "Idle",
                    ChipHtml = string.Empty,
                    Kind = "idle"
                });
            }
            catch { /* best-effort */ }
        }

        private void UpdateSharedPreview(double[] preview)
        {
            lock (_gate)
            {
                if (preview == null || preview.Length < 4)
                    _sharedPreview = Array.Empty<double>();
                else
                    _sharedPreview = preview;
            }

            InvalidateAll();
        }

        private void ClearSharedPreview()
        {
            lock (_gate)
            {
                _sharedPreview = Array.Empty<double>();
            }
            InvalidateAll();
        }

        private void ClearContextMenuForTarget(object targetDraw)
        {
            if (targetDraw == null) return;
            lock (_gate)
            {
                _pendingContextMenuByTarget.Remove(targetDraw);
            }
        }

        private void ClearAllContextMenus()
        {
            lock (_gate)
            {
                _pendingContextMenuByTarget.Clear();
            }
        }

        private double[] TryGetSharedPreview()
        {
            lock (_gate)
            {
                return _sharedPreview;
            }
        }

        public void UpdateCursorPosition(object targetDraw, double screenX, double screenY)
        {
            if (targetDraw == null) return;
            lock (_gate)
            {
                if (!_cursorByTarget.TryGetValue(targetDraw, out var state))
                    state = new CursorState();

                state.ScreenX = screenX;
                state.ScreenY = screenY;
                state.HasPosition = true;
                if (TryResolveContext_NoLock(targetDraw, out var viewport, out var graphics))
                {
                    viewport.GetScreenToModelAffine(out var m11, out var m12, out var m21, out var m22, out var tx, out var ty);
                    state.ModelX = m11 * screenX + m12 * screenY + tx;
                    state.ModelY = m21 * screenX + m22 * screenY + ty;
                    state.HasModelPosition = true;
                }
                _cursorByTarget[targetDraw] = state;
            }
            InvalidateTarget(targetDraw);
            RaiseViewportStatusChanged(targetDraw);
        }

        public void SetIdleCursor(CursorSpec cursor)
        {
            lock (_gate)
            {
                _idleCursor = cursor;
            }
        }

        private void EnsureModelPosition(ActionPointerInput input)
        {
            if (input.HasModelPosition) return;

            IViewport viewport;
            IGraphicsFacade graphics;
            if (!TryResolveContext(input.TargetDraw, out viewport, out graphics)) return;

            viewport.GetScreenToModelAffine(out var m11, out var m12, out var m21, out var m22, out var tx, out var ty);

            var sx = input.ScreenPosition.X;
            var sy = input.ScreenPosition.Y;
            var mx = m11 * sx + m12 * sy + tx;
            var my = m21 * sx + m22 * sy + ty;

            input.ModelPosition = new UniversalInput.Contracts.UniversalPoint
            {
                X = mx,
                Y = my
            };
            input.HasModelPosition = true;
        }

        private void TryRefreshActionGhostFromCursor(ActionSession session)
        {
            if (session == null) return;
            if (session.Fsm.State != MainInteractionFsm.FsmState.Running) return;

            var handler = session.Fsm.CurrentAction as IActionPointerHandler;
            if (handler == null) return;

            CursorState state;
            lock (_gate)
            {
                if (!_cursorByTarget.TryGetValue(session.InputTargetDraw, out state))
                    return;

                if (!state.HasPosition)
                    return;
            }

            var input = new ActionPointerInput
            {
                ScreenPosition = new UniversalInput.Contracts.UniversalPoint
                {
                    X = state.ScreenX,
                    Y = state.ScreenY
                },
                PointerDeviceType = DeviceType.Mouse,
                PointerId = 0,
                TargetDraw = session.InputTargetDraw,
                HasModelPosition = state.HasModelPosition,
                ModelPosition = new UniversalInput.Contracts.UniversalPoint
                {
                    X = state.ModelX,
                    Y = state.ModelY
                },
                TimestampUtc = DateTime.UtcNow
            };

            EnsureModelPosition(input);
            ApplySnap(input, PointerPhase.Move);
            handler.TryHandlePointerMove(input);
        }

        private void ApplySnap(ActionPointerInput input, PointerPhase phase)
        {
            if (input == null || !input.HasModelPosition)
                return;

            if (phase != PointerPhase.Down && phase != PointerPhase.Move)
                return;

            if (_snapService == null)
            {
                SetActiveSnapResult(null);
                return;
            }

            if (!TryResolveContext(input.TargetDraw, out var viewport, out var graphics))
            {
                SetActiveSnapResult(null);
                return;
            }

            if (_snapService.TrySnap(new SnapRequest
            {
                TargetDraw = input.TargetDraw,
                Viewport = viewport,
                ModelX = input.ModelPosition.X,
                ModelY = input.ModelPosition.Y,
                ScreenX = input.ScreenPosition.X,
                ScreenY = input.ScreenPosition.Y
            }, out var result) && result != null && result.HasSnap)
            {
                input.ModelPosition = new UniversalPoint
                {
                    X = result.X,
                    Y = result.Y
                };
                input.HasModelPosition = true;
                input.SnapResult = result;
                SetActiveSnapResult(result);
                return;
            }

            input.SnapResult = null;
            SetActiveSnapResult(null);
        }

        private void SetActiveSnapResult(SnapResult result)
        {
            lock (_gate)
            {
                _activeSnapResult = result;
            }

            InvalidateAll();
        }

        private void ClearActiveSnap()
        {
            SetActiveSnapResult(null);
        }

        private GraphicsFacade.SnapMarkerRenderState TryGetActiveSnapMarker()
        {
            lock (_gate)
            {
                if (_activeSnapResult == null || !_activeSnapResult.HasSnap)
                    return default;

                return new GraphicsFacade.SnapMarkerRenderState
                {
                    HasSnap = true,
                    ModelX = _activeSnapResult.X,
                    ModelY = _activeSnapResult.Y,
                    StrokeArgb = 0xFFFFA500,
                    SizePx = 8f,
                    ThicknessPx = 1.5f
                };
            }
        }

        private ActionContextMenuItem[] TryGetActionMenuItems(ActionSession session)
        {
            if (session?.Fsm?.CurrentAction is IActionMenuDescriptorProvider descriptor)
                return descriptor.GetMenuItems() ?? Array.Empty<ActionContextMenuItem>();

            return Array.Empty<ActionContextMenuItem>();
        }

        private string BuildCoordinateText(object targetDraw)
        {
            if (targetDraw == null)
                return string.Empty;

            lock (_gate)
            {
                if (!_cursorByTarget.TryGetValue(targetDraw, out var state) || !state.HasModelPosition)
                    return string.Empty;

                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "X: {0:0.###}  Y: {1:0.###}",
                    state.ModelX,
                    state.ModelY);
            }
        }

        private string BuildSnapText(bool isInputViewport)
        {
            if (!isInputViewport || _snapService == null)
                return string.Empty;

            lock (_gate)
            {
                if (_activeSnapResult == null || !_activeSnapResult.HasSnap)
                    return "SNAP: Off";

                return "SNAP: " + _activeSnapResult.Kind;
            }
        }

        private void RaiseViewportStatusChanged(object targetDraw)
        {
            var handler = ViewportInteractionStatusChanged;
            if (handler == null || targetDraw == null)
                return;

            try
            {
                handler(this, new ViewportInteractionStatusChangedEventArgs
                {
                    TargetDraw = targetDraw
                });
            }
            catch
            {
                // Best-effort.
            }
        }

        private void RaiseViewportStatusChangedForAll()
        {
            List<IRenderingComposition> targets;
            lock (_gate)
            {
                targets = new List<IRenderingComposition>(_facades.Keys);
            }

            for (var i = 0; i < targets.Count; i++)
                RaiseViewportStatusChanged(targets[i]);
        }

        private void InvalidateTarget(object targetDraw)
        {
            if (targetDraw == null) return;
            if (TryResolveContext(targetDraw, out var viewport, out var graphics))
                graphics?.Invalidate();
        }

        private enum PointerPhase { Down, Move, Up }

        private static class CoreDiag
        {
            public static void LogApplyDispatch(
                int targetCount, double x1, double y1, double x2, double y2)
            {
                var sw = x2 - x1;
                var sh = y2 - y1;
                Debug.WriteLine($"[CORE] ApplyVisualWindow → {targetCount} facade(s)  " +
                                $"world=({x1}, {y1}) → ({x2}, {y2}) size=({sw}, {sh})");
            }

            public static void LogDevicePerFacade(int idx, IRenderingComposition rc)
            {
                try
                {
                    Debug.WriteLine($"[CORE]  [{idx}] DEV={rc.CurrentWidth:0.##}x{rc.CurrentHeight:0.##} DIP  " +
                                    $"DPI=({rc.GetDpiX():0.##},{rc.GetDpiY():0.##})");
                }
                catch { /* best-effort */ }
            }
        }

        private void SetCursorForTarget(object targetDraw, CursorSpec cursor)
        {
            if (targetDraw == null) return;
            lock (_gate)
            {
                SetCursorForTarget_NoLock(targetDraw, cursor);
            }
            InvalidateTarget(targetDraw);
        }

        private void SetCursorForTarget_NoLock(object targetDraw, CursorSpec cursor)
        {
            if (cursor == null)
            {
                _cursorByTarget.Remove(targetDraw);
                return;
            }

            if (!_cursorByTarget.TryGetValue(targetDraw, out var state))
                state = new CursorState();

            state.Spec = cursor;
            _cursorByTarget[targetDraw] = state;
        }

        private GraphicsFacade.CursorRenderState TryGetCursorForTarget(object targetDraw)
        {
            if (targetDraw == null) return default;
            lock (_gate)
            {
                if (_cursorByTarget.TryGetValue(targetDraw, out var state))
                {
                    return new GraphicsFacade.CursorRenderState
                    {
                        Spec = state.Spec,
                        ScreenX = state.ScreenX,
                        ScreenY = state.ScreenY,
                        HasPosition = state.HasPosition
                    };
                }
            }

            return default;
        }

        private struct CursorState
        {
            public CursorSpec Spec;
            public double ScreenX;
            public double ScreenY;
            public bool HasPosition;
            public double ModelX;
            public double ModelY;
            public bool HasModelPosition;
        }



    }
}

