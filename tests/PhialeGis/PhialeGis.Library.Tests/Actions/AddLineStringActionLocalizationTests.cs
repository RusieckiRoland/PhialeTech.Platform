using NUnit.Framework;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Actions.Ogc;

namespace PhialeGis.Library.Tests.Actions
{
    [TestFixture]
    [Category("Unit")]
    public sealed class AddLineStringActionLocalizationTests
    {
        [Test]
        public void Start_WithPolishLanguage_UsesPolishPromptAndChip()
        {
            var action = new AddLineStringAction();
            ActionPromptPayload firstPrompt = null;

            action.Changed += (s, payload) =>
            {
                var prompt = payload as ActionPromptPayload;
                if (prompt != null && firstPrompt == null)
                    firstPrompt = prompt;
            };

            action.Start(new ActionContext
            {
                ActionId = System.Guid.NewGuid(),
                TargetDraw = new object(),
                LanguageId = "pl-PL"
            });

            Assert.NotNull(firstPrompt);
            Assert.AreEqual("Wskaz pierwszy punkt", firstPrompt.Prompt.ModeText);
            StringAssert.Contains("UNDO/U/COFNIJ", firstPrompt.Prompt.ChipHtml);
        }

        [Test]
        public void Start_WithUnknownLanguage_FallsBackToEnglish()
        {
            var action = new AddLineStringAction();
            ActionPromptPayload firstPrompt = null;

            action.Changed += (s, payload) =>
            {
                var prompt = payload as ActionPromptPayload;
                if (prompt != null && firstPrompt == null)
                    firstPrompt = prompt;
            };

            action.Start(new ActionContext
            {
                ActionId = System.Guid.NewGuid(),
                TargetDraw = new object(),
                LanguageId = "de-DE"
            });

            Assert.NotNull(firstPrompt);
            Assert.AreEqual("Specify first point", firstPrompt.Prompt.ModeText);
            StringAssert.Contains("Point input:", firstPrompt.Prompt.ChipHtml);
        }
    }
}

