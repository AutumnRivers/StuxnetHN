using Pathfinder.Meta.Load;
using Pathfinder.Event.Saving;
using Stuxnet_HN;
using System.Xml.Linq;
using StuxnetHN.Audio.Replacements;
using Pathfinder.Replacements;
using Pathfinder.Util.XML;

namespace StuxnetHN.Audio
{
    public static class AudioSaveLoader
    {
        public const string SMM_SAVE_ELEMENT_NAME = "StuxnetMusicManagerProperties";

        [Event()]
        public static void SaveSMMProperties(SaveEvent saveEvent)
        {
            if (!StuxnetCore.Configuration.Audio.ReplaceMusicManager) return;

            var save = saveEvent.Save;
            XElement stuxnetAudioElement = new(SMM_SAVE_ELEMENT_NAME);

            XAttribute beginLoopAttr = new("BeginLoop", StuxnetMusicManager.LoopBegin);
            XAttribute endLoopAttr = new("EndLoop", StuxnetMusicManager.LoopEnd);

            stuxnetAudioElement.Add(beginLoopAttr, endLoopAttr);

            save.FirstNode.AddAfterSelf(stuxnetAudioElement);
        }

        [SaveExecutor("HacknetSave." + SMM_SAVE_ELEMENT_NAME)]
        public class SMMSaveLoader : SaveLoader.SaveExecutor
        {
            public override void Execute(EventExecutor exec, ElementInfo info)
            {
                if (int.TryParse(info.Attributes["BeginLoop"], out int begin))
                {
                    StuxnetMusicManager.LoopBegin = begin;
                }

                if (int.TryParse(info.Attributes["EndLoop"], out int end))
                {
                    StuxnetMusicManager.LoopEnd = end;
                }
            }
        }
    }
}
