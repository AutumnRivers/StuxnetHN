using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Stuxnet_HN.Extensions
{
    public static class XmlReaderExtensions
    {
        public static string ReadRequiredAttribute(this XmlReader xml, string attributeName)
        {
            if (xml.MoveToAttribute(attributeName))
            {
                return xml.ReadContentAsString();
            }
            else
            {
                throw new FormatException($"{xml.Name} is missing '{attributeName}' attribute!");
            }
        }
    }
}
