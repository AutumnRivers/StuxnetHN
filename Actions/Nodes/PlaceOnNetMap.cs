using System;

using Hacknet;

using Microsoft.Xna.Framework;

using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions.Nodes
{
    public class PlaceOnNetMap : DelayablePathfinderAction
    {
        [XMLStorage]
        public string TargetCompID;

        [XMLStorage]
        public string StartingPosition = "truecenter";

        [XMLStorage]
        public string Offset = "0,0";

        public override void Trigger(OS os)
        {
            string[] offsetSplit = Offset.Split(',');
            Vector2 offsetVector = new Vector2(
                float.Parse(offsetSplit[0]),
                float.Parse(offsetSplit[1])
                );

            Vector2 startingPos;

            switch(StartingPosition)
            {
                case "truecenter": // Center
                    startingPos = new Vector2(0.5f, 0.5f);
                    break;
                case "topleft": // Top Left
                    startingPos = new Vector2(0, 0);
                    break;
                case "centerleft": // Center Left
                    startingPos = new Vector2(0, 0.5f);
                    break;
                case "bottomleft": // Bottom Left
                    startingPos = new Vector2(0, 1);
                    break;
                case "topcenter": // Top Center
                    startingPos = new Vector2(0.5f, 0);
                    break;
                case "bottomcenter": // Bottom Center
                    startingPos = new Vector2(0.5f, 1);
                    break;
                case "topright": // Top Right
                    startingPos = new Vector2(1, 0);
                    break;
                case "centerright": // Center Right
                    startingPos = new Vector2(1, 0.5f);
                    break;
                case "bottomright": // Bottom Right
                    startingPos = new Vector2(1, 1);
                    break;
                default: // Default to center
                    startingPos = new Vector2(0.5f, 0.5f);
                    break;
            }

            Computer targetComp = ComputerLookup.FindById(TargetCompID) ?? throw new NullReferenceException($"Target Comp with ID {TargetCompID} not found!");

            targetComp.location = new Vector2(
                startingPos.X + offsetVector.X,
                startingPos.Y + offsetVector.Y
                );

            Console.WriteLine($"{targetComp.idName} :: {targetComp.location.X} , {targetComp.location.Y}");
        }
    }
}
