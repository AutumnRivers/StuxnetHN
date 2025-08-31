using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using BepInEx;

using Pathfinder.Action;
using Pathfinder.Util;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Actions
{
    public class VaultKeyActions
    {
        public class AddVaultKey : DelayablePathfinderAction
        {
            [XMLStorage]
            public string KeyName;

            public override void Trigger(OS os)
            {
                if(KeyName.IsNullOrWhiteSpace())
                {
                    throw new ArgumentNullException("\"KeyName\" cannot be null or whitespace.");
                }

                if(!StuxnetCore.receivedKeys.ContainsKey(KeyName))
                {
                    StuxnetCore.receivedKeys.Add(KeyName, 1);
                } else
                {
                    if (StuxnetCore.receivedKeys[KeyName] >= 10) { return; }

                    StuxnetCore.receivedKeys[KeyName]++;
                }

                int keyCount = StuxnetCore.receivedKeys[KeyName];
                string keyCountString = keyCount.ToString();

                StuxnetCore.UpdateCustomReplacement($"{KeyName}_Keys", keyCountString);
            }
        }

        public class RemoveVaultKey : DelayablePathfinderAction
        {
            [XMLStorage]
            public string KeyName;

            public override void Trigger(OS os)
            {
                if (KeyName.IsNullOrWhiteSpace())
                {
                    throw new ArgumentNullException("\"KeyName\" cannot be null or whitespace.");
                }

                if (!StuxnetCore.receivedKeys.ContainsKey(KeyName))
                {
                    StuxnetCore.receivedKeys.Add(KeyName, 0);
                }
                else
                {
                    if(StuxnetCore.receivedKeys[KeyName] <= 0) { return; }

                    StuxnetCore.receivedKeys[KeyName]--;
                }

                int keyCount = StuxnetCore.receivedKeys[KeyName];
                string keyCountString = keyCount.ToString();

                StuxnetCore.UpdateCustomReplacement($"{KeyName}_Keys", keyCountString);
            }
        }
    }
}
