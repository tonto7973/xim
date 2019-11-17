using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Routing.Tests
{
    [TestFixture]
    public class ErrorTests
    {
        [TestCase("foo", "bar")]
        [TestCase("baz", "qux")]
        public void SetItem_SetsReason(string reasonName, string description)
        {
            var error = new Error
            {
                [reasonName] = description
            };

            error.ShouldSatisfyAllConditions(
                () => error[reasonName].ShouldBe(description),
                () => error.Reasons[reasonName].ShouldBe(description)
            );
        }

        [TestCase("header", "is empty")]
        [TestCase("id", "mMust be a number")]
        public void GetItem_GetsReason(string reasonName, string description)
        {
            var error = new Error();
            error.Reasons[reasonName] = description;

            error[reasonName].ShouldBe(description);
        }

        [Test]
        public void SerializesToXml()
        {
            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = null
            };
            var error = new Error(new Dictionary<string, string>
            {
                ["{Id}"] = "Not valid id",
                ["{Color}"] = "Bad color"
            })
            {
                Title = "Failed to handle request"
            };
            var xml = ToXml(error, xmlSettings);

            xml.ShouldBe("<Error><Title>Failed to handle request</Title><Reason Name=\"{Id}\">Not valid id</Reason><Reason Name=\"{Color}\">Bad color</Reason></Error>");
        }

        [TestCase("Some error", "The value is incorrect")]
        [TestCase("Important", "Could not verify the key")]
        public void DeserializesFromXml(string title, string reason)
        {
            const string key = "{key}";
            var xml = $"<Error><Reason Name=\"{key}\">{reason}</Reason><Reason Name=\"old\">man</Reason><Title>{title}</Title></Error>";
            var error = FromXml<Error>(xml, new XmlReaderSettings());
            error.Title.ShouldBe(title);
            error.Reasons.Count.ShouldBe(2);
            error.Reasons[key].ShouldBe(reason);
            error.Reasons["old"].ShouldBe("man");
        }

        [Test]
        public void DeserializesFromXml_IgnoresInvalidElement()
        {
            const string xml = "<Error><Why Name=\"buda\" /><Reason Name=\"old\" /><Title>X</Title></Error>";
            var error = FromXml<Error>(xml, new XmlReaderSettings());
            error.Title.ShouldBe("X");
            error.Reasons.Count.ShouldBe(1);
            error.Reasons["old"].ShouldBeNull();
        }

        [Test]
        public void DeserializesFromXml_ReadsEmptyReasonElementNameAttributeAsEmptyStringAndValueAsNull()
        {
            const string xml = "<Error><Reason /><Title>Abc</Title></Error>";
            var error = FromXml<Error>(xml, new XmlReaderSettings());
            error.Title.ShouldBe("Abc");
            error.Reasons.Count.ShouldBe(1);
            error.Reasons[""].ShouldBeNull();
        }

        [Test]
        public void DeserializesFromXml_ReadsEmptyReasonElementNameAttributeAsEmptyString()
        {
            const string xml = "<Error><Reason>No reason</Reason><Title>Abc</Title></Error>";
            var error = FromXml<Error>(xml, new XmlReaderSettings());
            error.Title.ShouldBe("Abc");
            error.Reasons.Count.ShouldBe(1);
            error.Reasons[""].ShouldBe("No reason");
        }

        [TestCase("<Error></Error>")]
        [TestCase("<Error><Title /></Error>")]
        [TestCase("<Error />")]
        public void DeserializesFromEmptyXml(string xml)
        {
            var error = FromXml<Error>(xml, new XmlReaderSettings());
            error.Title.ShouldBe(null);
            error.Reasons.ShouldNotBeNull();
        }

        [Test]
        public void SerializesToJsonWithTitleNamedError()
        {
            var error = new Error(new Dictionary<string, string>
            {
                ["{Color}"] = "Unknown color"
            })
            {
                Title = "Operation failed"
            };
            var json = JsonSerializer.Serialize(error);
            json.ShouldBe("{\"error\":\"Operation failed\",\"reasons\":{\"{Color}\":\"Unknown color\"}}");
        }

        [Test] // https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.ixmlserializable.getschema?view=netframework-4.7.2
        public void GetSchema_ReturnsNull()
        {
            IXmlSerializable error = new Error();

            error.GetSchema().ShouldBeNull();
        }

        private static string ToXml<T>(T value, XmlWriterSettings xmlSettings)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var sr = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(sr, xmlSettings))
            {
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                xmlSerializer.Serialize(xmlWriter, value, namespaces);
                return sr.ToString();
            }
        }

        private static T FromXml<T>(string value, XmlReaderSettings xmlSettings)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var sr = new StringReader(value))
            using (var xmlReader = XmlReader.Create(sr, xmlSettings))
            {
                return (T)xmlSerializer.Deserialize(xmlReader);
            }
        }
    }
}
