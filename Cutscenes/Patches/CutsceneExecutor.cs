using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Action;
using Pathfinder.Util;

using Stuxnet_HN.Cutscenes.Actions;
using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.Cutscenes.Patches
{
    [HarmonyPatch]
    public class CutsceneExecutor
    {
        internal static bool hasSetDelays = false;
        internal static float totalDelay = 0f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessor),nameof(PostProcessor.end))]
        static void DrawCutscene()
        {
            if(!StuxnetCore.cutsceneIsActive)
            {
                return;
            }

            StuxnetCutscene cs = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID];
            Computer delayHostComp = ComputerLookup.FindById(cs.delayHostID);
            DelayableActionSystem delayHost = DelayableActionSystem.FindDelayableActionSystemOnComputer(delayHostComp);

            foreach (var rect in cs.activeRectangles)
            {
                Rectangle r = cs.rectangles[rect];
                RenderedRectangle.doRectangle(r.X, r.Y, r.Width, r.Height, Color.White);
            }

            if(!hasSetDelays)
            {
                foreach(var inst in cs.instructions)
                {
                    PathfinderAction instAction = new TriggerInstruction(inst);
                    delayHost.AddAction(instAction, inst.Delay);
                }

                totalDelay = cs.instructions.Last().Delay;

                StuxnetCutsceneInstruction resetInst = StuxnetCutsceneInstruction.CreateResetInstruction();
                resetInst.Cutscene = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID];
                PathfinderAction resetAction = new TriggerInstruction(resetInst);
                delayHost.AddAction(resetAction, totalDelay);

                hasSetDelays = true;
            }

            StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID] = cs;
        }

        // Tuple: (string id, Rectangle rect, Vector2 targetVec, float tweenDuration, float tweenAmount
        internal static List<Tuple<string, Rectangle, Vector2, float, float>> targetVectorRects =
            new List<Tuple<string, Rectangle, Vector2, float, float>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessor), nameof(PostProcessor.end))]
        static void LerpObjects()
        {
            if(!targetVectorRects.Any())
            {
                return;
            }

            float gt = (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;

            StuxnetCutscene cs = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID];

            for(int i = 0; i < targetVectorRects.Count; i++)
            {
                var target = targetVectorRects[i];

                string id = target.Item1;
                float lerpAmount = gt / target.Item4;
                float tweenAmount = lerpAmount + target.Item5;
                Console.WriteLine(tweenAmount);

                Rectangle refRect = cs.rectangles[id];
                Vector2 currentPos = new Vector2(target.Item2.X, target.Item2.Y);

                Vector2 newVec = Vector2.Lerp(currentPos, target.Item3, tweenAmount);
                refRect.X = (int)newVec.X;
                refRect.Y = (int)newVec.Y;
                cs.rectangles[id] = refRect;

                if(currentPos == target.Item3)
                {
                    targetVectorRects.RemoveAt(i);
                    continue;
                }

                var replaceTuple = Tuple.Create(id, refRect, target.Item3, target.Item4, tweenAmount);
                targetVectorRects[i] = replaceTuple;
            }
        }

        internal static void AddTweenedRectangle(string id, Rectangle rect, Vector2 targetVec, float tweenDuration)
        {
            var tuple = Tuple.Create(id, rect, targetVec, tweenDuration, 0f);
            targetVectorRects.Add(tuple);
        }
    }
}
