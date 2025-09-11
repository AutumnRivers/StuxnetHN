using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Stuxnet_HN.Extensions
{
    public static class XDocumentExtensions
    {
        public static bool TryGetAttribute(this XElement element, string attrName, out string value)
        {
            value = null;
            if(element.Attributes().Any(a => a.Name.LocalName == attrName))
            {
                value = element.Attribute(attrName).Value;
                return true;
            } else { return false; }
        }

        public static string[] GetAttributeValues(this XElement element, List<string> names)
        {
            var values = new string[names.Count];
            for (int idx = 0; idx < values.Length; idx++)
            {
                string name = names[idx];
                if (element.TryGetAttribute(name, out string value))
                {
                    values[idx] = value;
                }
                else { values[idx] = null; }
            }
            return values;
        }

        public static bool TryGetAttributes(this XElement element, List<string> names, out string[] values)
        {
            values = new string[names.Count];
            for(int idx = 0; idx < values.Length; idx++)
            {
                string name = names[idx];
                if(element.TryGetAttribute(name, out string value))
                {
                    values[idx] = value;
                } else { return false; }
            }
            return true;
        }

        public static bool TryGetAttributes(this XElement element, string[] names, out string[] values)
        {
            values = new string[names.Length];
            for (int idx = 0; idx < values.Length; idx++)
            {
                string name = names[idx];
                if (element.TryGetAttribute(name, out string value))
                {
                    values[idx] = value;
                }
                else { return false; }
            }
            return true;
        }
    }
}
