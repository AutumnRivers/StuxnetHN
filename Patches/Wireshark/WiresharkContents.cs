using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Hacknet;
using Pathfinder.Util.XML;
using Stuxnet_HN.Executables;

namespace Stuxnet_HN.Patches
{
    public class WiresharkContents
    {
        public List<WiresharkEntry> entries = new();
        public string originID = "playerComp";

        private const string key = "82904-39431-39";

        public bool IsValid { get; private set; } = true;

        public string GetSaveString()
        {
            StringBuilder saveString = new();

            saveString.Append($"<WiresharkEntries origin=\"{originID}\">\r\n");
            foreach(var entry in entries)
            {
                saveString.Append($"<pcap id=\"{entry.id}\" from=\"{entry.ipFrom}\" " +
                    $"to=\"{entry.ipTo}\" method=\"{entry.method}\" protocol=\"{entry.protocol}\" " +
                    $"secure=\"{entry.secure}\">" +
                    entry.Content + "</pcap>\r\n");
            }
            saveString.Append("</WiresharkEntries>");

            return saveString.ToString();
        }

        public static WiresharkContents ReadWiresharkCaptureFileXML(ElementInfo info, string originID = "playerComp")
        {
            WiresharkContents contents = new() { originID = originID };

            if(info.Children.Count == 0)
            {
                StuxnetCore.Logger.LogWarning("Wireshark capture element doesn't have any children! " +
                    "This is unsupported.");
            }

            foreach(var child in info.Children)
            {
                if(child.Name != "pcap")
                {
                    throw new FormatException("Unrecognized child element in Wireshark element");
                }

                uint id = 0;
                string source = "127.0.0.1";
                string destination = "192.168.1.1";
                string method = "GET";
                string protocol = "TCP";
                bool isSecure = false;
                string origin = originID;

                throwIfNotFound("id", child);
                throwIfNotFound("secure", child);

                if (!uint.TryParse(child.Attributes["id"], out id))
                {
                    throw new FormatException("Wireshark capture attribute 'id' needs to be a non-negative numerical value");
                }

                if (!bool.TryParse(child.Attributes["secure"], out isSecure))
                {
                    throw new FormatException("Wireshark capture attribute 'secure' needs to be a boolean (true/false)");
                }

                if (child.Attributes.ContainsKey("source"))
                {
                    source = child.Attributes["source"];
                } else if(child.Attributes.ContainsKey("from"))
                {
                    source = child.Attributes["from"];
                }

                if (child.Attributes.ContainsKey("method"))
                {
                    method = child.Attributes["method"];
                }
                if (child.Attributes.ContainsKey("protocol"))
                {
                    protocol = child.Attributes["protocol"];
                }

                if (child.Attributes.ContainsKey("to"))
                {
                    destination = child.Attributes["to"];
                }
                else if (child.Attributes.ContainsKey("dest"))
                {
                    destination = child.Attributes["dest"];
                }

                WiresharkEntry entry = new(id, source, destination, child.Content, isSecure, method, protocol);
                contents.entries.Add(entry);
            }

            return contents;

            void throwIfNotFound(string attribute, ElementInfo elem)
            {
                if (!elem.Attributes.ContainsKey(attribute))
                {
                    throw new FormatException(string.Format("wiresharkCapture pcap element " +
                        "is missing required attribute '{0}'", attribute));
                }
            }
        }

        public static WiresharkContents Deserialize(XmlReader xml, string originID = "playerComp")
        {
            WiresharkContents contents = new WiresharkContents();
            while(xml.Name != "WiresharkEntries")
            {
                xml.Read();
                if(xml.EOF)
                {
                    throw new FormatException("Unexpected end of file looking for Wireshark tag.");
                }
            }

            do
            {
                if(xml.Name == "WiresharkEntries")
                {
                    if(xml.MoveToAttribute("origin"))
                    {
                        contents.originID = xml.ReadContentAsString();
                    } else
                    {
                        contents.originID = originID;
                    }
                }

                xml.Read();
                if (xml.Name == "WiresharkEntries" && !xml.IsStartElement())
                {
                    return contents;
                }

                if(xml.Name == "pcap" && xml.IsStartElement())
                {
                    contents.IsValid = true;

                    uint id = 0;
                    string source = "127.0.0.1";
                    string destination = "192.168.1.1";
                    string method = "GET";
                    string protocol = "TCP";
                    bool isSecure = false;

                    string content = "";

                    if (xml.MoveToAttribute("id"))
                    {
                        id = (uint)xml.ReadContentAsInt();
                    }

                    if (xml.MoveToAttribute("from"))
                    {
                        Console.WriteLine($"from: {xml.ReadContentAsString()}");
                        source = xml.ReadContentAsString();
                    } else if(xml.MoveToAttribute("source"))
                    {
                        source = xml.ReadContentAsString();
                    }

                    if (xml.MoveToAttribute("to"))
                    {
                        Console.WriteLine($"to: {xml.ReadContentAsString()}");
                        destination = xml.ReadContentAsString();
                    }

                    if (xml.MoveToAttribute("method"))
                    {
                        Console.WriteLine($"method: {xml.ReadContentAsString()}");
                        method = xml.ReadContentAsString();
                    }

                    if (xml.MoveToAttribute("protocol"))
                    {
                        Console.WriteLine($"protocol: {xml.ReadContentAsString()}");
                        protocol = xml.ReadContentAsString();
                    }

                    if (xml.MoveToAttribute("secure"))
                    {
                        Console.WriteLine($"secure: {xml.ReadContentAsString()}");
                        isSecure = bool.Parse(xml.ReadContentAsString().ToLower());
                    }

                    xml.MoveToContent();
                    content = xml.ReadElementContentAsString();

                    WiresharkEntry entry = new WiresharkEntry(id, source, destination, content, isSecure, method, protocol);

                    contents.entries.Add(entry);
                }
            } while (!xml.EOF);
            throw new FormatException("Unexpected end of file trying to deserialize wireshark contents!");
        }

        public string GetEncodedFileString()
        {
            string saveString = GetSaveString();
            string encoded = FileEncrypter.EncryptString(saveString, "WIRESHARK PCAP", "------", key);
            return "WIRESHARK_NETWORK_CAPTURE(PCAP) :: 1.37.2 -----------\n\n" + encoded;
        }

        public static WiresharkContents GetContentsFromEncodedFileString(string data)
        {
            string mainEncodedContent = data.Substring("WIRESHARK_NETWORK_CAPTURE(PCAP) :: 1.37.2 -----------\n\n".Length);
            string decodedContent = FileEncrypter.DecryptString(mainEncodedContent, key)[2];

            using Stream input = Utils.GenerateStreamFromString(decodedContent);
            XmlReader reader = XmlReader.Create(input);
            return Deserialize(reader);
        }
    }
}
