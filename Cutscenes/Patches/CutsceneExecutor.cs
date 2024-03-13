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

using ObjectTypes = Stuxnet_HN.Cutscenes.StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes;

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

            foreach(var id in cs.activeImages)
            {
                StuxnetCutsceneImage img = cs.images[id];
                Rectangle dest = new Rectangle()
                {
                    X = (int)img.GetCalculatedPosition().X,
                    Y = (int)img.GetCalculatedPosition().Y,
                    Width = (int)img.GetCalculatedSize().X,
                    Height = (int)img.GetCalculatedSize().Y
                };
                Rectangle source = new Rectangle()
                {
                    X = 0, Y = 0,
                    Width = (int)img.image.Width,
                    Height = (int)img.image.Height
                };
                Vector2 origin = new Vector2()
                {
                    X = img.image.Width / 2,
                    Y = img.image.Height / 2
                };
                GuiData.spriteBatch.Draw(img.image, dest, source, Color.White, img.currentRotation,
                    origin, SpriteEffects.None, 0.1f);
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

        // Tuple: (string id, Rectangle rect, Vector2 targetVec, float tweenDuration, float tweenAmount, Vector2 originPos)
        internal static List<Tuple<string, Rectangle, Vector2, float, float, Vector2>> targetVectorRects =
            new List<Tuple<string, Rectangle, Vector2, float, float, Vector2>>();

        // Tuple: (string id, StuxnetCutsceneImage img, Vector2 targetVec, float tweenDuration, float tweenAmount, Vector2 originPos)
        internal static List<Tuple<string, StuxnetCutsceneImage, Vector2, float, float, Vector2>> targetVectorImgs =
            new List<Tuple<string, StuxnetCutsceneImage, Vector2, float, float, Vector2>>();

        // Tuple: (string id, float originalRotation, float targetRotation, float rotationDuration/Speed, float tweenAmount, bool clockwise)
        internal static List<Tuple<string, float, float, float, float, bool>> targetRotations =
            new List<Tuple<string, float, float, float, float, bool>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessor), nameof(PostProcessor.end))]
        static void LerpObjects()
        {
            if(OS.currentInstance == null || OS.currentInstance?.lastGameTime == null
                || !StuxnetCore.cutsceneIsActive || StuxnetCore.activeCutsceneID == "NONE")
            {
                return;
            }

            float gt = (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;

            StuxnetCutscene cs = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID];

            if(targetVectorRects.Any())
            {
                for (int i = 0; i < targetVectorRects.Count; i++)
                {
                    TweenRectangle(cs, gt, i);
                }
            }

            if(targetVectorImgs.Any())
            {
                for (int i = 0; i < targetVectorImgs.Count; i++)
                {
                    TweenImage(cs, gt, i);
                }
            }

            if(targetRotations.Any())
            {
                for (int i = 0; i < targetRotations.Count; i++)
                {
                    RotateImage(cs, gt, i);
                }
            }
        }

        private static void TweenRectangle(StuxnetCutscene cs, float gt, int i)
        {
            var target = targetVectorRects[i];

            string id = target.Item1;
            float lerpAmount = gt / target.Item4;
            float tweenAmount = lerpAmount + target.Item5;

            Rectangle refRect = cs.rectangles[id];
            Vector2 currentPos = target.Item6;

            Vector2 newVec = Vector2.Lerp(currentPos, target.Item3, tweenAmount);
            refRect.X = (int)newVec.X;
            refRect.Y = (int)newVec.Y;
            cs.rectangles[id] = refRect;

            if (tweenAmount >= 1.0f)
            {
                targetVectorRects.RemoveAt(i);
                return;
            }

            var replaceTuple = Tuple.Create(id, refRect, target.Item3, target.Item4, tweenAmount, target.Item6);
            targetVectorRects[i] = replaceTuple;
        }

        private static void TweenImage(StuxnetCutscene cs, float gt, int i)
        {
            var target = targetVectorImgs[i];

            string id = target.Item1;
            float lerpAmount = gt / target.Item4;
            float tweenAmount = lerpAmount + target.Item5;

            StuxnetCutsceneImage refImg = cs.images[id];
            Vector2 currentPos = target.Item6;

            Vector2 newVec = Vector2.Lerp(currentPos, target.Item3, tweenAmount);
            refImg.position.X = newVec.X;
            refImg.position.Y = newVec.Y;
            cs.images[id] = refImg;

            if(tweenAmount >= 1.0f)
            {
                targetVectorImgs.RemoveAt(i);
                return;
            }

            var replaceTuple = Tuple.Create(id, refImg, target.Item3, target.Item4, tweenAmount, target.Item6);
            targetVectorImgs[i] = replaceTuple;
        }

        private static void RotateImage(StuxnetCutscene cutscene, float gameSeconds, int index)
        {
            var target = targetRotations[index];

            bool infinite = false;

            string id = target.Item1;
            float originAngle = target.Item2;
            float targetAngle = target.Item3;

            if(targetAngle == -1f)
            {
                targetAngle = 359.9f;
                infinite = true;
            }

            targetAngle = MathHelper.ToRadians(targetAngle);

            float lerpAmount;
            float tweenAmount;

            if (target.Item4 > 0.01f)
            {
                lerpAmount = gameSeconds / target.Item4;
                tweenAmount = lerpAmount + target.Item5;
                Console.WriteLine(tweenAmount);
            } else
            {
                tweenAmount = 1.0f;
            }

            if(infinite)
            {
                lerpAmount = gameSeconds * target.Item4;
                tweenAmount = lerpAmount + target.Item5;
                originAngle = 0f;
            }

            float newAngle = MathHelper.Lerp(originAngle, targetAngle, tweenAmount);
            cutscene.images[id].currentRotation = newAngle;

            if(tweenAmount >= 1.0f && !infinite)
            {
                targetRotations.RemoveAt(index);
                return;
            } else if(tweenAmount >= 1.0f && infinite)
            {
                tweenAmount = 0f;
            }

            var replaceTuple = Tuple.Create(target.Item1, originAngle, target.Item3, target.Item4, tweenAmount, target.Item6);
            targetRotations[index] = replaceTuple;
        }

        internal static void AddTweenedRectangle(string id, Rectangle rect, Vector2 targetVec, float tweenDuration)
        {
            Vector2 origin = new Vector2(rect.X, rect.Y);
            var tuple = Tuple.Create(id, rect, targetVec, tweenDuration, 0f, origin);
            targetVectorRects.Add(tuple);
        }

        internal static void AddTweenedImage(string id, StuxnetCutsceneImage img, Vector2 targetVec, float tweenDuration)
        {
            Vector2 origin = new Vector2(img.position.X, img.position.Y);
            var tuple = Tuple.Create(id, img, targetVec, tweenDuration, 0f, origin);
            targetVectorImgs.Add(tuple);
        }

        // Tuple: (string id, float originalRotation, float targetRotation, float rotationDuration/Speed, float tweenAmount)
        internal static void AddTimedRotation(string id, float targetAngle, float duration, bool clockwise)
        {
            StuxnetCutsceneImage img = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID].images[id];
            var tuple = Tuple.Create(id, img.currentRotation, targetAngle, duration, 0f, clockwise);
            targetRotations.Add(tuple);
        }

        internal static void AddInfiniteRotation(string id, float rotationSpeed, bool clockwise)
        {
            StuxnetCutsceneImage img = StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID].images[id];
            var tuple = Tuple.Create(id, img.currentRotation, -1f, rotationSpeed, 0f, clockwise);
            targetRotations.Add(tuple);
        }
    }
}
