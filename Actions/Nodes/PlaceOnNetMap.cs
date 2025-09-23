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

        public override void Trigger(OS os)
        {
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
