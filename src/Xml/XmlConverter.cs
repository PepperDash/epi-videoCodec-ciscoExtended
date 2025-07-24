using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Xml
{
	public static class XmlConverter
	{
		public static string SerializeObject<T>(T value)
		{
			var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
			var serializer = new XmlSerializer(value.GetType());
			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = true, // Do not include XML declaration
				Indent = true, // Indent XML for readability
			};

			using (var stream = new StringWriter())
			using (var writer = XmlWriter.Create(stream, settings))
			{
				serializer.Serialize(writer, value, emptyNamespaces);
				return stream.ToString();
			}
		}
	}

}
