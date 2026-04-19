using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Interactions.Activities;

namespace PhialeGis.Library.Tests.Migration
{
    [TestFixture]
    [Category("Unit")]
    [Category("MigrationGuard")]
    public sealed class Stage3CoreInputMigrationTests
    {
        [Test]
        public void CoreInputDtos_UseCoreDeviceType()
        {
            Assert.AreEqual(typeof(CoreDeviceType), typeof(CoreInputEvent).GetProperty(nameof(CoreInputEvent.DeviceType))?.PropertyType);
            Assert.AreEqual(typeof(CoreDeviceType), typeof(CorePointerInput).GetProperty(nameof(CorePointerInput.DeviceType))?.PropertyType);
            Assert.AreEqual(typeof(CoreDeviceType), typeof(CoreManipulationInput).GetProperty(nameof(CoreManipulationInput.DeviceType))?.PropertyType);
        }

        [Test]
        public void BaseActivity_EventObservable_UsesCoreInputEvent()
        {
            var eventObservableType = typeof(BaseActivity).GetProperty(nameof(BaseActivity.EventObservable))?.PropertyType;

            Assert.NotNull(eventObservableType);
            Assert.IsTrue(eventObservableType.IsGenericType);
            Assert.AreEqual(typeof(IObservable<CoreInputEvent>), eventObservableType);
        }

        [Test]
        public void InteractionMonitor_Transitions_UseCoreDeviceType()
        {
            var field = typeof(InteractionMonitor).GetField("stateTransitions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            var keyTuple = field.FieldType.GetGenericArguments().First();
            var tupleArgs = keyTuple.GetGenericArguments();

            Assert.AreEqual(typeof(CoreInputKind), tupleArgs[1]);
            Assert.AreEqual(typeof(CoreDeviceType), tupleArgs[2]);
        }

        [Test]
        public void ActivityCoreCallbacks_DoNotExposeUniversalArgs()
        {
            AssertActivityUsesCoreInputs(typeof(PanActivity));
            AssertActivityUsesCoreInputs(typeof(MultiTouchActivity));
        }

        [Test]
        public void ViewportTranslation_UsesCorePoint()
        {
            var method = typeof(ViewportManager).GetMethod("TranslateUPoint", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(CorePoint), parameters[0].ParameterType);
        }

        private static void AssertActivityUsesCoreInputs(Type activityType)
        {
            var onCoreMethods = activityType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.Name.StartsWith("OnCore", StringComparison.Ordinal))
                .ToArray();

            Assert.IsNotEmpty(onCoreMethods, $"{activityType.Name} should define core callbacks.");

            foreach (var method in onCoreMethods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    Assert.AreNotEqual(
                        "UniversalInput.Contracts",
                        parameter.ParameterType.Namespace,
                        $"{activityType.Name}.{method.Name} should not expose UniversalInput.Contracts arguments.");
                }
            }
        }
    }
}
