﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BepInEx;
using Hacknet;

using HarmonyLib;

using Stuxnet_HN.Executables;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class WiresharkComputerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerLoader), "loadComputer")]
        static void Postfix_WiresharkFiles(ref string filename, ref object __result)
        {
            Computer c = (Computer)__result;
            Stream fileStream = File.OpenRead(filename);
            XmlReader xml = XmlReader.Create(fileStream);

            while(xml.Name != "wiresharkCapture")
            {
                xml.Read();
                if(xml.EOF)
                {
                    return;
                }
            }

            if(xml.Name == "wiresharkCapture")
            {
                xml.MoveToAttribute("path");
                string folderPath = xml.ReadContentAsString();

                xml.MoveToAttribute("name");
                string wFilename = xml.ReadContentAsString();

                WiresharkContents contents = WiresharkContents.ReadWiresharkCaptureFileXML(xml);
                contents.originID = c.idName;

                string filedata = contents.GetEncodedFileString();
                Folder targetFolder = c.getFolderFromPath(folderPath, true);

                Console.WriteLine($"{folderPath} : {wFilename}");

                if(targetFolder.searchForFile(wFilename) != null)
                {
                    targetFolder.searchForFile(wFilename).data = filedata;
                } else
                {
                    targetFolder.files.Add(new FileEntry(filedata, wFilename));
                }
            }

            xml.Close();
            fileStream.Close();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerLoader), "loadComputer")]
        static void Postfix_WiresharkedPC(ref string filename, ref object __result)
        {
            Computer c = (Computer)__result;
            Stream fileStream = File.OpenRead(filename);
            XmlReader xml = XmlReader.Create(fileStream);

            while (xml.Name != "WiresharkEntries")
            {
                xml.Read();
                if (xml.EOF)
                {
                    return;
                }
            }

            if(xml.Name == "WiresharkEntries")
            {
                WiresharkContents contents = WiresharkContents.Deserialize(xml, c.idName);

                if(!contents.IsValid) { return; }

                StuxnetCore.wiresharkComps.Add(c.idName, contents);
            }
        }
    }

    public class WiresharkContents
    {
        public List<WiresharkEntry> entries = new List<WiresharkEntry>();
        public string originID = "playerComp";

        private const string key = "82904-39431-39";

        public bool IsValid { get; private set; }

        public string GetSaveString()
        {
            StringBuilder saveString = new StringBuilder();

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

        public static WiresharkContents ReadWiresharkCaptureFileXML(XmlReader xml, string originID = "playerComp")
        {
            WiresharkContents contents = new WiresharkContents
            {
                originID = originID
            };

            Console.WriteLine(1);

            do
            {
                if (xml.Name == "WiresharkEntries")
                {
                    if (xml.MoveToAttribute("origin"))
                    {
                        contents.originID = xml.ReadContentAsString();
                    }
                    else
                    {
                        contents.originID = originID;
                    }
                }

                xml.Read();
                if(xml.Name == "wiresharkCapture" && !xml.IsStartElement())
                {
                    return contents;
                }

                if (xml.Name == "pcap" && xml.IsStartElement())
                {
                    contents.IsValid = true;

                    Console.WriteLine(2);

                    uint id = 0;
                    string source = "127.0.0.1";
                    string destination = "192.168.1.1";
                    string method = "GET";
                    string protocol = "TCP";
                    bool isSecure = false;

                    if (xml.MoveToAttribute("id"))
                    {
                        id = (uint)xml.ReadContentAsInt();
                    }

                    if (xml.MoveToAttribute("from"))
                    {
                        Console.WriteLine($"from: {xml.ReadContentAsString()}");
                        source = xml.ReadContentAsString();
                    }

                    if(xml.MoveToAttribute("to"))
                    {
                        Console.WriteLine($"to: {xml.ReadContentAsString()}");
                        destination = xml.ReadContentAsString();
                    }

                    if(xml.MoveToAttribute("method"))
                    {
                        Console.WriteLine($"method: {xml.ReadContentAsString()}");
                        method = xml.ReadContentAsString();
                    }

                    if(xml.MoveToAttribute("protocol"))
                    {
                        Console.WriteLine($"protocol: {xml.ReadContentAsString()}");
                        protocol = xml.ReadContentAsString();
                    }

                    if(xml.MoveToAttribute("secure"))
                    {
                        Console.WriteLine($"secure: {xml.ReadContentAsString()}");
                        isSecure = bool.Parse(xml.ReadContentAsString().ToLower());
                    }

                    xml.MoveToContent();
                    string content = xml.ReadElementContentAsString();

                    WiresharkEntry entry = new WiresharkEntry(id, source, destination, content, isSecure, method, protocol);

                    contents.entries.Add(entry);
                }
            } while (!xml.EOF);
            throw new FormatException("Unexpected end of file trying to deserialize wireshark contents!");
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
