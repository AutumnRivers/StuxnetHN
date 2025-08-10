using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pathfinder.Meta.Load;
using Pathfinder.Replacements;
using Pathfinder.Util.XML;

using Hacknet;

namespace Stuxnet_HN.Patches
{
    [ComputerExecutor("Computer.wiresharkCapture", ParseOption.ParseInterior)]
    public class WiresharkFileLoader : ContentLoader.ComputerExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            if (!info.Attributes.ContainsKey("path") || !info.Attributes.ContainsKey("name"))
            {
                throw new FormatException("Missing required attribute on wiresharkCapture element");
            }

            string folderPath = info.Attributes["path"];
            string filename = info.Attributes["name"];

            WiresharkContents contents = WiresharkContents.ReadWiresharkCaptureFileXML(info, Comp.idName);
            string filedata = contents.GetEncodedFileString();
            Folder targetFolder = Comp.getFolderFromPath(folderPath, true);

            if (targetFolder.searchForFile(filename) != null)
            {
                targetFolder.searchForFile(filename).data = filedata;
            }
            else
            {
                targetFolder.files.Add(new FileEntry(filedata, filename));
            }
        }
    }

    [ComputerExecutor("Computer.WiresharkEntries", ParseOption.ParseInterior)]
    public class WiresharkEntriesLoader : ContentLoader.ComputerExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            WiresharkContents contents = WiresharkContents.ReadWiresharkCaptureFileXML(info, Comp.idName);

            if(StuxnetCore.wiresharkComps.ContainsKey(Comp.idName))
            {
                StuxnetCore.Logger.LogWarning(string.Format("Computer with ID of '{0}' already exists in " +
                    "wiresharkComps! Overwriting...", Comp.idName));
                StuxnetCore.wiresharkComps[Comp.idName] = contents;
            } else
            {
                StuxnetCore.wiresharkComps.Add(Comp.idName, contents);
            }
        }
    }
}
