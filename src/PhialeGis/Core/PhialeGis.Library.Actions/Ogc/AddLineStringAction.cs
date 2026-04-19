using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Localization;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Actions.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PhialeGis.Library.Actions.Ogc
{
    /// <summary>
    /// Interactive action: "Add LineString" (MVP).
    /// Supports absolute points "x y", relative "@dx dy", polar "<angle dist>",
    /// UNDO/U/COFNIJ/CONFIJ, empty ENTER to finish, and pointer input.
    /// </summary>
    public sealed class AddLineStringAction : InteractiveActionBase, IActionPointerHandler, IActionMenuCommandHandler, IActionMenuDescriptorProvider, IInteractionCursorProvider
    {
        private const string MenuCommandEnter = "enter";
        private const string MenuCommandCancel = "cancel";
        private const string MenuCommandUndo = "undo";

        public override string Name => "AddLineString";
        public override DslMode RequiredMode => DslMode.Points;
        public override bool CanBeSuspended => true;

        private static readonly CursorSpec CursorSpec = new CursorSpec
        {
            StrokeArgb = 0xFF1E2A3A,
            Thickness = 1.5,
            CrosshairLength = 22.0,
            Gap = 6.0,
            ApertureSize = 6.0
        };

        public CursorSpec Cursor => CursorSpec;

        private readonly List<CadPoint> _points = new List<CadPoint>();
        private ActionContext _ctx;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private CadPoint? _ghost;
        private string _chipHtml = string.Empty;

        public override void Start(ActionContext ctx)
        {
            _ctx = ctx;
            _points.Clear();
            _ghost = null;
            _chipHtml = BuildChipHtml();

            // Initial prompt
            SetPrompt(T("text.points.prompt.first", "Specify first point"), _chipHtml, "draw");
            EmitPreview();
        }

        public override void HandleInput(string line)
        {
            var t = (line ?? string.Empty).Trim();

            if (t.Length == 0)
            {
                if (_points.Count >= 2)
                {
                    EmitFinished(new ActionFinishPayload
                    {
                        Success = true,
                        Message = $"✓ LINESTRING inserted (n={_points.Count})",
                        CanonicalCommand = BuildCanonicalCommand(),
                        Result = BuildResult()
                    });
                }
                else
                {
                    SetPrompt(GetPromptForPointCount(), _chipHtml, "draw");
                }
                return;
            }

            var hasLast = _points.Count > 0;
            var last = hasLast ? _points[_points.Count - 1] : default(CadPoint);

            if (CadPointInputParser.IsUndo(t))
            {
                UndoLastPoint();
                return;
            }

            if (CadPointInputParser.TryParse(t, hasLast, last.X, last.Y, out var parsed))
            {
                _points.Add(new CadPoint(parsed.X, parsed.Y));
                _ghost = null;
                SetPrompt(GetPromptForPointCount(), _chipHtml, "draw");
                EmitPreview();
                return;
            }

            // Unsupported token (MVP)
            SetPrompt(GetPromptForPointCount(), _chipHtml, "error");
        }

        public override void Resume()
        {
            SetPrompt(GetPromptForPointCount(), _chipHtml, "draw");
            EmitPreview();
        }

        public bool TryHandlePointerDown(ActionPointerInput input)
        {
            if (input == null) return false;
            if (!input.HasModelPosition) return false;
            if (input.Button == PointerButton.Secondary)
            {
                EmitContextMenu(input);
                return true;
            }

            _points.Add(new CadPoint(input.ModelPosition.X, input.ModelPosition.Y));
            _ghost = null;
            SetPrompt(GetPromptForPointCount(), _chipHtml, "draw");
            EmitPreview();
            return true;
        }

        public bool TryHandlePointerMove(ActionPointerInput input)
        {
            if (input == null) return false;
            if (!input.HasModelPosition) return false;
            if (_points.Count == 0) return false;

            _ghost = new CadPoint(input.ModelPosition.X, input.ModelPosition.Y);
            EmitPreview();
            return true;
        }

        public bool TryHandlePointerUp(ActionPointerInput input)
        {
            if (input == null) return false;
            return _points.Count > 0;
        }

        public bool TryHandleMenuCommand(string commandId)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return false;

            switch (commandId.Trim().ToLowerInvariant())
            {
                case MenuCommandEnter:
                    HandleInput(string.Empty);
                    return true;
                case MenuCommandUndo:
                    UndoLastPoint();
                    return true;
                case MenuCommandCancel:
                    Cancel();
                    return true;
                default:
                    return false;
            }
        }

        // ===== Helpers =====

        private string GetPromptForPointCount()
        {
            if (_points.Count <= 0)
                return T("text.points.prompt.first", "Specify first point");

            return T("text.points.prompt.next", "Specify next point or press Enter to finish");
        }

        private string T(string key, string fallback)
        {
            var languageId = _ctx?.LanguageId;
            return DslUiLocalization.GetText(key, languageId, fallback);
        }

        private string BuildChipHtml()
        {
            var title = T("text.points.chip.title", "Point input:");
            var abs = T("text.points.chip.abs", "(X,Y)");
            var rel = T("text.points.chip.rel", "@dx,dy");
            var polar = T("text.points.chip.polar", "&lt;angle dist&gt;");
            var undo = T("text.points.chip.undo", "UNDO/U");

            return "<span style='display:inline-block;padding:2px 10px;border-radius:9999px;" +
                   "background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.10);" +
                   "box-shadow:0 0 0 1px rgba(0,0,0,.25) inset'>" +
                   "<span style='font-weight:600;color:#E6EEF8;'>" + title + "</span> " +
                   "<span style='color:#7fb3ff;margin-left:8px'>" + abs + "</span>" +
                   "&nbsp;&nbsp;<span style='opacity:.35'>|</span>&nbsp;&nbsp;" +
                   "<span style='color:#ffb86c'>" + rel + "</span>" +
                   "&nbsp;&nbsp;<span style='opacity:.35'>|</span>&nbsp;&nbsp;" +
                   "<span style='color:#ff79c6'>" + polar + "</span>" +
                   "&nbsp;&nbsp;<span style='opacity:.35'>|</span>&nbsp;&nbsp;" +
                   "<span style='color:#50fa7b'>" + undo + "</span>" +
                   "</span>";
        }

        private void EmitContextMenu(ActionPointerInput input)
        {
            EmitChanged(new ActionContextMenuPayload
            {
                TargetDraw = _ctx?.TargetDraw,
                ScreenPosition = input != null ? input.ScreenPosition : default(UniversalInput.Contracts.UniversalPoint),
                Items = GetMenuItems()
            });
        }

        public ActionContextMenuItem[] GetMenuItems()
        {
            return new[]
            {
                new ActionContextMenuItem
                {
                    CommandId = MenuCommandEnter,
                    Label = T("text.points.menu.enter", "Enter"),
                    Enabled = _points.Count >= 2
                },
                new ActionContextMenuItem
                {
                    CommandId = MenuCommandCancel,
                    Label = T("text.points.menu.cancel", "Cancel"),
                    Enabled = true
                },
                new ActionContextMenuItem
                {
                    IsSeparator = true
                },
                new ActionContextMenuItem
                {
                    CommandId = MenuCommandUndo,
                    Label = T("text.points.menu.undo", "Undo"),
                    Enabled = _points.Count > 0
                }
            };
        }

        private void UndoLastPoint()
        {
            if (_points.Count > 0) _points.RemoveAt(_points.Count - 1);
            _ghost = null;
            SetPrompt(T("text.points.prompt.undoDone", "Last point undone"), _chipHtml, "info");
            EmitPreview();
        }

        private void EmitPreview()
        {
            var pts = BuildPreviewArray();
            EmitChanged(new ActionChangePayload
            {
                TargetDraw = _ctx?.TargetDraw,
                Preview = pts
            });
        }

        private string BuildCanonicalCommand()
        {
            var sb = new StringBuilder();
            sb.Append("ADD LINESTRING (");
            for (int i = 0; i < _points.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendFormat(Inv, "{0} {1}", _points[i].X, _points[i].Y);
            }
            sb.Append(")");
            return sb.ToString();
        }

        private LineStringActionResult BuildResult()
        {
            var arr = new double[_points.Count * 2];
            for (int i = 0; i < _points.Count; i++)
            {
                arr[i * 2] = _points[i].X;
                arr[i * 2 + 1] = _points[i].Y;
            }

            return new LineStringActionResult
            {
                Points = arr,
                TargetDraw = _ctx?.TargetDraw
            };
        }

        private double[] BuildPreviewArray()
        {
            var count = _points.Count + (_ghost.HasValue ? 1 : 0);
            if (count < 2) return new double[0];

            var arr = new double[count * 2];
            for (int i = 0; i < _points.Count; i++)
            {
                arr[i * 2] = _points[i].X;
                arr[i * 2 + 1] = _points[i].Y;
            }

            if (_ghost.HasValue)
            {
                var idx = _points.Count;
                arr[idx * 2] = _ghost.Value.X;
                arr[idx * 2 + 1] = _ghost.Value.Y;
            }

            return arr;
        }

        private struct CadPoint
        {
            public CadPoint(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }
    }
}

