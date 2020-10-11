using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Xim.Simulators.Api.Routing
{
    /// <summary>
    /// Api routing error.
    /// </summary>
    public sealed class Error : IXmlSerializable
    {
        private const string XmlErrorElementName = "Error";
        private const string XmlReasonElementName = "Reason";
        private const string XmlReasonKeyAttribute = "Name";

        /// <summary>
        /// Gets and sets an error reason.
        /// </summary>
        /// <param name="key">The reason key.</param>
        /// <returns>The reason associated with the <paramref name="key"/>.</returns>
        [JsonIgnore]
        public string this[string key]
        {
            get => Reasons[key];
            set => Reasons[key] = value;
        }

        /// <summary>
        /// Gets or sets error title.
        /// </summary>
        [JsonPropertyName("error")]
        public string Title { get; set; }

        /// <summary>
        /// Gets the dictionary with the error reasons.
        /// </summary>
        [JsonPropertyName("reasons")]
        public Dictionary<string, string> Reasons { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Error"/> class.
        /// </summary>
        public Error()
        {
            Reasons = new Dictionary<string, string>();
        }

        internal Error(IDictionary<string, string> reasons)
        {
            Reasons = new Dictionary<string, string>(reasons);
        }

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Title = null;
            Reasons.Clear();

            if (reader.IsEmptyElement)
                return;

            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name.Equals(XmlErrorElementName, StringComparison.InvariantCulture))
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name.Equals(nameof(Title), StringComparison.InvariantCulture))
                    {
                        if (reader.IsEmptyElement)
                        {
                            Title = null;
                        }
                        else
                        {
                            Title = reader.ReadElementContentAsString();
                            continue;
                        }
                    }
                    else if (reader.Name.Equals(XmlReasonElementName, StringComparison.InvariantCulture))
                    {
                        var name = reader[XmlReasonKeyAttribute] ?? "";
                        if (reader.IsEmptyElement)
                        {
                            Reasons[name] = null;
                        }
                        else
                        {
                            Reasons[name] = reader.ReadElementContentAsString();
                            continue;
                        }
                    }
                }

                reader.Read();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(Title));
            writer.WriteString(Title);
            writer.WriteEndElement();

            foreach (KeyValuePair<string, string> item in Reasons)
            {
                writer.WriteStartElement(XmlReasonElementName);
                writer.WriteAttributeString(XmlReasonKeyAttribute, item.Key);
                writer.WriteString(item.Value);
                writer.WriteEndElement();
            }
        }
    }
}