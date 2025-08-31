using System;
using BepInEx;
using Hacknet;

using Microsoft.Xna.Framework;

using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions.Nodes
{
    public class PlaceOnNetMap : DelayablePathfinderAction
    {
        [XMLStorage]
        public string TargetCompID;

        [XMLStorage]
        public string Offset;

        [XMLStorage]
        public string StartingPosition = string.Empty;

        public override void Trigger(OS os)
        {
            if(!StartingPosition.IsNullOrWhiteSpace())
            {
                StuxnetCore.Logger.LogWarning("The StartingPosition attribute for PlaceNodeOnNetMap is " +
                    "deprecated, and will be removed in Stuxnet 2.1.0, which will break your extension.\n" +
                    "Please remove the attribute ASAP!");
            }

            string[] offsetSplit = Offset.Split(',');
            Vector2 offsetVector = new(
                float.Parse(offsetSplit[0]),
                float.Parse(offsetSplit[1])
                );

            Computer targetComp = ComputerLookup.FindById(TargetCompID) ?? throw new
                NullReferenceException($"Target Comp with ID {TargetCompID} not found!");

            targetComp.location = new Vector2(
                offsetVector.X,
                offsetVector.Y
                );
        }
    }
}
