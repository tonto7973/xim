using System.Text;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Internal
{
    [TestFixture]
    public class HttpContextExtensionsTests
    {
        [Test]
        public void SetApiSimulatorSettings_SetsSettingsToInstance()
        {
            var settings = new ApiSimulatorSettings();
            var context = new DefaultHttpContext();

            context.SetApiSimulatorSettings(settings);

            context.Items["ApiSimulatorSettings.Instance"].ShouldBeSameAs(settings);
        }

        [Test]
        public void SetApiSimulatorSettings_SetsSettingsToNull()
        {
            var context = new DefaultHttpContext();

            context.SetApiSimulatorSettings(null);

            context.Items["ApiSimulatorSettings.Instance"].ShouldBeNull();
        }

        [Test]
        public void SetApiSimulatorSettings_ReturnsContext()
        {
            var context = new DefaultHttpContext();

            var result = context.SetApiSimulatorSettings(new ApiSimulatorSettings());

            result.ShouldBeSameAs(context);
        }

        [Test]
        public void GetApiSimulatorSettings_ReturnsNull_WhenSettingsNotSetInItems()
        {
            var context = new DefaultHttpContext();

            var result = context.GetApiSimulatorSettings();

            result.ShouldBeNull();
        }

        [Test]
        public void GetApiSimulatorSettings_ReturnsTheSameInstance()
        {
            var settings = new ApiSimulatorSettings();
            var context = new DefaultHttpContext();
            context.SetApiSimulatorSettings(settings);

            var result = context.GetApiSimulatorSettings();

            result.ShouldBeSameAs(settings);
        }

        [Test]
        public void SetApiSimulatorBodyEncoding_SetsEncodingToInstance()
        {
            var encoding = Encoding.Unicode;
            var context = new DefaultHttpContext();

            context.SetApiSimulatorBodyEncoding(encoding);

            context.Items["ApiSimulatorBody.Encoding"].ShouldBeSameAs(encoding);
        }

        [Test]
        public void SetApiSimulatorBodyEncoding_SetsEncodingToNull()
        {
            var context = new DefaultHttpContext();

            context.SetApiSimulatorBodyEncoding(null);

            context.Items["ApiSimulatorBody.Encoding"].ShouldBeNull();
        }

        [Test]
        public void SetApiSimulatorBodyEncoding_ReturnsContext()
        {
            var context = new DefaultHttpContext();

            var result = context.SetApiSimulatorBodyEncoding(Encoding.ASCII);

            result.ShouldBeSameAs(context);
        }

        [Test]
        public void GetApiSimulatorBodyEncoding_ReturnsNull_WhenSettingsNotSetInItems()
        {
            var context = new DefaultHttpContext();

            var result = context.GetApiSimulatorBodyEncoding();

            result.ShouldBeNull();
        }

        [Test]
        public void GetApiSimulatorBodyEncoding_ReturnsTheSameInstance()
        {
            var encoding = Encoding.UTF8;
            var context = new DefaultHttpContext();
            context.SetApiSimulatorBodyEncoding(encoding);

            var result = context.GetApiSimulatorBodyEncoding();

            result.ShouldBeSameAs(encoding);
        }
    }
}
