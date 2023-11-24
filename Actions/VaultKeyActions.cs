using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

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
                if(!StuxnetCore.receivedKeys.ContainsKey(KeyName))
                {
                    StuxnetCore.receivedKeys.Add(KeyName, 1);
                } else
                {
                    if (StuxnetCore.receivedKeys[KeyName] >= 10) { return; }

                    StuxnetCore.receivedKeys[KeyName]++;
                }
            }
        }

        public class RemoveVaultKey : DelayablePathfinderAction
        {
            [XMLStorage]
            public string KeyName;

            public override void Trigger(OS os)
            {
                if (!StuxnetCore.receivedKeys.ContainsKey(KeyName))
                {
                    StuxnetCore.receivedKeys.Add(KeyName, 0);
                }
                else
                {
                    if(StuxnetCore.receivedKeys[KeyName] <= 0) { return; }

                    StuxnetCore.receivedKeys[KeyName]--;
                }
            }
        }
    }
}
