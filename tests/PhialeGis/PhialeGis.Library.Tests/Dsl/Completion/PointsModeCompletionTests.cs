using System;
using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Modes;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Adapter;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Dsl.Completion
{
    [TestFixture]
    [Category("Integration")]
    public sealed class PointsModeCompletionTests
    {
        [Test]
        public void EmptyInput_Offers_PointMode_Tokens()
        {
            var editor = new FakeEditorInteractive();
            var ctxProvider = new DslContextRegistry();
            ctxProvider.SetFor(editor, new DslContext { Mode = DslMode.Points, LanguageId = "pl-PL" });

            var adapter = CreateAdapter();
            var list = adapter.GetCompletions(string.Empty, 0, editor, ctxProvider);
            var labels = list.Items.Select(x => x.Label).ToArray();
            var inserts = list.Items.Select(x => x.InsertText).ToArray();

            CollectionAssert.Contains(labels, "(X,Y) punkt absolutny");
            CollectionAssert.Contains(labels, "@dx,dy punkt wzgledny");
            CollectionAssert.Contains(labels, "<angle dist> punkt biegunowy");
            CollectionAssert.Contains(labels, "UNDO/U cofnij");

            CollectionAssert.Contains(inserts, "0 0");
            CollectionAssert.Contains(inserts, "@1 0");
            CollectionAssert.Contains(inserts, "<45 10>");
            CollectionAssert.Contains(inserts, "UNDO");
        }

        [Test]
        public void UnknownLanguage_FallsBack_To_English()
        {
            var editor = new FakeEditorInteractive();
            var ctxProvider = new DslContextRegistry();
            ctxProvider.SetFor(editor, new DslContext { Mode = DslMode.Points, LanguageId = "de-DE" });

            var adapter = CreateAdapter();
            var list = adapter.GetCompletions(string.Empty, 0, editor, ctxProvider);
            var labels = list.Items.Select(x => x.Label).ToArray();

            CollectionAssert.Contains(labels, "(X,Y) absolute point");
            CollectionAssert.Contains(labels, "@dx,dy relative point");
            CollectionAssert.Contains(labels, "<angle dist> polar point");
            CollectionAssert.Contains(labels, "UNDO/U undo");
        }

        [Test]
        public void PrefixU_Still_Offers_Undo_In_PointsMode()
        {
            var editor = new FakeEditorInteractive();
            var ctxProvider = new DslContextRegistry();
            ctxProvider.SetFor(editor, new DslContext { Mode = DslMode.Points, LanguageId = "pl" });

            var adapter = CreateAdapter();
            var list = adapter.GetCompletions("u", 1, editor, ctxProvider);
            var labels = list.Items.Select(x => x.Label).ToArray();

            CollectionAssert.Contains(labels, "UNDO/U cofnij");
        }

        private static PhGisDslEngineAdapter CreateAdapter()
        {
            return new PhGisDslEngineAdapter(
                new PhGis(),
                _ => Tuple.Create<IViewport, IGraphicsFacade>(null, null));
        }
    }
}

