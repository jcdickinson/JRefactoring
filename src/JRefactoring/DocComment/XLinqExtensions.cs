using System.Linq;
using System.Xml.Linq;

namespace JRefactoring.DocComment
{
    public static class XLinqExtensions
    {
        public static XElement GetOrAddElement(this XContainer container, XName name)
        {
            var element = container.Element(name);
            if (element == null) container.Add(element = new XElement(name));
            return element;
        }

        public static XElement GetOrAddElement(this XContainer container, XName name, XName attributeName, string attributeValue)
        {
            var element = container.Elements(name).FirstOrDefault(e => (string)e.Attribute(attributeName) == attributeValue);
            if (element == null) container.Add(element = new XElement(name, new XAttribute(attributeName, attributeValue)));
            return element;
        }
    }
}
