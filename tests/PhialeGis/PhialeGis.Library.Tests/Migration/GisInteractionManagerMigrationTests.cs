using System;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Actions.Ogc;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Migration
{
    [TestFixture]
    [Category("Unit")]
    [Category("MigrationGuard")]
    public sealed class GisInteractionManagerMigrationTests
    {
        [Test]
        public void RegisterControl_AcceptsEditorAndRejectsUnknownObject()
        {
            var manager = new GisInteractionManager(new FakeBackendFactory());
            var editor = new FakeEditorInteractive();

            Assert.DoesNotThrow(() => manager.RegisterControl(editor));
            Assert.DoesNotThrow(() => manager.UnregisterControl(editor));

            var ex = Assert.Throws<ArgumentException>(() => manager.RegisterControl(new object()));
            Assert.AreEqual("compositionObj", ex.ParamName);
        }

        [Test]
        public void StartInteractiveAction_UsesTargetFromEditor_WhenTargetIsNull()
        {
            var manager = new GisInteractionManager(new FakeBackendFactory());
            var editor = new FakeTargetAwareEditorInteractive(new object());

            Assert.DoesNotThrow(() => manager.StartInteractiveAction(new AddLineStringAction(), null, editor));
            Assert.IsTrue(manager.TryHandleInteractiveInput("0 0", editor));
        }

        [Test]
        public void PointerHandling_ReturnsFalseForUnknownTarget()
        {
            var manager = new GisInteractionManager(new FakeBackendFactory());

            var handled = manager.TryHandleInteractivePointerDown(new ActionPointerInput
            {
                TargetDraw = new object(),
                HasModelPosition = true
            });

            Assert.IsFalse(handled);
        }

        private sealed class FakeTargetAwareEditorInteractive : IEditorInteractive, IEditorTargetAware
        {
            public FakeTargetAwareEditorInteractive(object targetDraw)
            {
                TargetDraw = targetDraw;
            }

            public object TargetDraw { get; }

            public event EventHandler<UniversalTextChangedEventArgs> TextChangedUniversal { add { } remove { } }
            public event EventHandler<UniversalSelectionChangedEventArgs> SelectionChangedUniversal { add { } remove { } }
            public event EventHandler<UniversalCaretMovedEventArgs> CaretMovedUniversal { add { } remove { } }
            public event EventHandler<UniversalDirtyChangedEventArgs> DirtyChangedUniversal { add { } remove { } }
            public event EventHandler<UniversalCommandEventArgs> CommandUniversal { add { } remove { } }
            public event EventHandler<UniversalSaveRequestedEventArgs> SaveRequestedUniversal { add { } remove { } }
            public event EventHandler<UniversalLanguageChangedEventArgs> LanguageChangedUniversal { add { } remove { } }
            public event EventHandler<UniversalThemeChangedEventArgs> ThemeChangedUniversal { add { } remove { } }
            public event EventHandler<UniversalFindRequestedEventArgs> FindRequestedUniversal { add { } remove { } }
            public event EventHandler<UniversalReplaceRequestedEventArgs> ReplaceRequestedUniversal { add { } remove { } }
            public event EventHandler<UniversalLinkClickedEventArgs> LinkClickedUniversal { add { } remove { } }
            public event EventHandler<UniversalDiagnosticsUpdatedEventArgs> DiagnosticsUpdatedUniversal { add { } remove { } }
            public event EventHandler<UniversalHoverRequestedEventArgs> HoverRequestedUniversal { add { } remove { } }

            public void SetText(string text) { }
            public void SetLanguageId(string languageId) { }
            public void SetReadOnly(bool isReadOnly) { }
        }

        private sealed class FakeBackendFactory : IPhRenderBackendFactory
        {
            public IPhRenderDriver Create(object canvas, IViewport viewport)
                => new FakeRenderDriver();
        }

        private sealed class FakeRenderDriver : IPhRenderDriver
        {
        }
    }
}


