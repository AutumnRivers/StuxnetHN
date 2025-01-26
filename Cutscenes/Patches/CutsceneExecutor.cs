using System;
using System.Collections.Generic;
using System.Linq;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Action;
using Pathfinder.Util;

using Stuxnet_HN.Cutscenes.Actions;
using Stuxnet_HN.Extensions;
using ObjectTypes = Stuxnet_HN.Cutscenes.StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes;

namespace Stuxnet_HN.Cutscenes.Patches
{
    [HarmonyPatch]
    public class CutsceneExecutor
    {
        internal static bool hasSetDelays = false;
        internal static float totalDelay = 0f;

        internal static StuxnetCutscene ActiveCutscene => StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID];

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessor),nameof(PostProcessor.end))]
        static void DrawCutscene()
        {
            if(!StuxnetCore.cutsceneIsActive)
            {
                return;
            }

            StuxnetCutscene cs = ActiveCutscene;
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
                GuiData.spriteBatch.Draw(img.image, dest, source, Color.White * img.opacity, img.currentRotation,
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
                resetInst.Cutscene = ActiveCutscene;
                PathfinderAction resetAction = new TriggerInstruction(resetInst);
                delayHost.AddAction(resetAction, totalDelay);

                hasSetDelays = true;
            }

            StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID] = cs;
        }

        // Tweeners (Rewrite)
        internal static List<CutsceneTweener> tweeners = new();

        // Rotaters (Rewrite)
        internal static List<CutsceneRotater> rotaters = new();

        // Rotating Images
        // Tuple: (string id, float originalRotation, float targetRotation, float rotationDuration/Speed, float tweenAmount, bool clockwise)
        internal static List<Tuple<string, float, float, float, float, bool>> targetRotations =
            new List<Tuple<string, float, float, float, float, bool>>();

        // Resizers (Rewrite)
        internal static List<CutsceneResizer> resizers = new();

        // Tweened Rectangles (Resize)
        // Tuple: (string id, Vector2 targetSize, bool aspectRatio, float tweenDuration, float tweenAmount, Vector2 originSize)
        internal static List<Tuple<string, Vector2, bool, float, float, Vector2>> targetResizeRects =
            new List<Tuple<string, Vector2, bool, float, float, Vector2>>();

        // Fade Images
        // Tuple: (string id, float originalOpacity, bool fadeIn, float duration, float progress)
        internal static List<Tuple<string, float, bool, float, float>> targetFadeImages =
            new List<Tuple<string, float, bool, float, float>>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessor), nameof(PostProcessor.end))]
        static void LerpObjects()
        {
            if(OS.currentInstance == null || OS.currentInstance?.lastGameTime == null
                || !StuxnetCore.cutsceneIsActive || StuxnetCore.activeCutsceneID == "NONE")
            {
                return;
            }

            DoActionIfListIsNotEmpty(tweeners, ActivateTweener);
            //DoActionIfListIsNotEmpty(targetRotations, RotateImage);
            DoActionIfListIsNotEmpty(rotaters, ActivateRotater);
            DoActionIfListIsNotEmpty(targetResizeRects, ResizeRectangle);
            DoActionIfListIsNotEmpty(targetFadeImages, FadeImage);
        }

        private static void DoActionIfListIsNotEmpty<T>(List<T> list, Action<StuxnetCutscene, float, int> actionToRun)
        {
            float gt = (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;

            if (list.Any())
            {
                for(int i = 0; i < list.Count; i++)
                {
                    actionToRun.Invoke(ActiveCutscene, gt, i);
                }
            }
        }

        private static void FadeImage(StuxnetCutscene cs, float gameTime, int index)
        {
            var target = targetFadeImages[index];

            string id = target.Item1;
            float targetOpacity = target.Item3 ? 1.0f : 0.0f;
            float startingOpacity = target.Item3 ? 0.0f : 1.0f;
            StuxnetCutsceneImage refImage = cs.images[id];

            float lerpAmount = gameTime / target.Item4;
            float currentProgress = lerpAmount + target.Item5;

            Console.WriteLine($"D: {lerpAmount} / P: {currentProgress} / O: {refImage.opacity}");

            float newOpacity = MathHelper.Lerp(startingOpacity, targetOpacity, currentProgress);
            refImage.opacity = newOpacity;
            cs.images[id] = refImage;

            if(currentProgress >= 1.0f)
            {
                targetFadeImages.RemoveAt(index);
                return;
            }

            var replaceTuple = Tuple.Create(id, target.Item2, target.Item3, target.Item4, currentProgress);
            targetFadeImages[index] = replaceTuple;
        }

        // Rewrite
        private static void ActivateTweener(StuxnetCutscene cs, float gt, int i)
        {
            CutsceneTweener tweener = tweeners[i];

            gt /= 60; // Fixes speed

            float lerpAmount = gt / tweener.Duration;
            float newAmount = lerpAmount + tweener.Amount;
            tweener.Amount = newAmount;

            if(tweener.Rect != default)
            {
                Rectangle refRect = cs.rectangles[tweener.ID];

                Vector2 currentPos = new(refRect.X, refRect.Y);
                Vector2 newVector = Vector2.Lerp(currentPos, tweener.Target, tweener.Amount);
                refRect.X = (int)newVector.X;
                refRect.Y = (int)newVector.Y;

                cs.rectangles[tweener.ID] = refRect;
            } else if(tweener.Image != null)
            {
                StuxnetCutsceneImage image = cs.images[tweener.ID];

                Vector2 currentPos = new(image.position.X, image.position.Y);
                Vector2 newVector = Vector2.Lerp(currentPos, tweener.Target, tweener.Amount);
                image.position.X = newVector.X;
                image.position.Y = newVector.Y;

                cs.images[tweener.ID] = image;
            }

            if(tweener.Amount >= 1.0f)
            {
                tweeners.RemoveAt(i);
                return;
            }

            tweeners[i] = tweener;
        }

        // Rewrite
        private static void ActivateRotater(StuxnetCutscene cs, float gt, int i)
        {
            CutsceneRotater rotater = rotaters[i];

            bool infinite = rotater.Speed > -0.5f;

            StuxnetCutsceneImage image = cs.images[rotater.ID];

            float origin = image.currentRotation;
            float target = rotater.Target;

            if(infinite)
            {
                target = 359.9f;
            } else { gt /= 60; }
            if(!rotater.Clockwise)
            {
                target *= -1f;
            }

            target = MathHelper.ToRadians(target);
            float lerpAmount;
            float tweenAmount;

            if(rotater.Duration > 0.01f)
            {
                lerpAmount = gt / rotater.Duration;
                tweenAmount = lerpAmount + rotater.Amount;
            } else { tweenAmount = 1.0f; }

            if(infinite)
            {
                lerpAmount = gt * rotater.Speed;
                tweenAmount = lerpAmount + rotater.Amount;
                origin = 0f;
            }
            rotater.Amount = tweenAmount;

            float newAngle = MathHelper.Lerp(origin, target, rotater.Amount);
            cs.images[rotater.ID].currentRotation = newAngle;

            if(rotater.Amount >= 1.0f && !infinite)
            {
                rotaters.RemoveAt(i);
                return;
            } else if(rotater.Amount >= 1.0f && infinite)
            {
                rotater.Amount = 0f;
            }

            rotaters[i] = rotater;
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

            if(target.Item6 == true)
            {
                targetAngle *= -1;
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

        private static void ResizeRectangle(StuxnetCutscene cutscene, float gameSeconds, int index)
        {
            var target = targetResizeRects[index];

            string id = target.Item1;
            float lerpAmount = gameSeconds / target.Item4;
            float tweenAmount = lerpAmount + target.Item5;

            Rectangle refRect = cutscene.rectangles[id];
            Vector2 targetSize = target.Item2;
            Vector2 targetRelativeSize = GetRelativeSize(targetSize.X, targetSize.Y);

            Vector2 originSize = target.Item6;

            bool maintainAspectRatio = target.Item3;

            Vector2 newVec;

            if(maintainAspectRatio)
            {
                newVec = refRect.LerpSizeAspect(targetRelativeSize, originSize, tweenAmount);
            } else
            {
                GraphicsDevice userGraphics = GuiData.spriteBatch.GraphicsDevice;
                Viewport viewport = userGraphics.Viewport;

                Vector2 targetCalcSize = Vector2.Lerp(originSize, targetRelativeSize, tweenAmount);

                Vector2 calcNewSize = new Vector2(
                    targetCalcSize.X * viewport.Width,
                    targetCalcSize.Y * viewport.Height
                    );

                newVec = calcNewSize;
            }

            refRect.Width = (int)newVec.X;
            refRect.Height = (int)newVec.Y;

            cutscene.rectangles[id] = refRect;

            if(tweenAmount >= 1.0f)
            {
                targetResizeRects.RemoveAt(index);
                return;
            }

            var replaceTuple = Tuple.Create(id, targetSize, maintainAspectRatio, target.Item4, tweenAmount, originSize);
            targetResizeRects[index] = replaceTuple;
        }

        internal static void AddTweenedRectangle(string id, Rectangle rect, Vector2 targetVec, float tweenDuration)
        {
            tweeners.Add(new CutsceneTweener(id, rect, targetVec, tweenDuration)); // rewrite
        }

        internal static void AddTweenedImage(string id, StuxnetCutsceneImage img, Vector2 targetVec, float tweenDuration)
        {
            tweeners.Add(new CutsceneTweener(id, img, targetVec, tweenDuration)); // rewrite
        }

        // Tuple: (string id, float originalRotation, float targetRotation, float rotationDuration/Speed, float tweenAmount)
        internal static void AddTimedRotation(string id, float targetAngle, float duration, bool clockwise)
        {
            /*StuxnetCutsceneImage img = ActiveCutscene.images[id];
            var tuple = Tuple.Create(id, img.currentRotation, targetAngle, duration, 0f, clockwise);
            targetRotations.Add(tuple);*/
            rotaters.Add(new CutsceneRotater(id, targetAngle, duration, clockwise));
        }

        internal static void AddInfiniteRotation(string id, float rotationSpeed, bool clockwise)
        {
            /*StuxnetCutsceneImage img = ActiveCutscene.images[id];
            var tuple = Tuple.Create(id, img.currentRotation, -1f, rotationSpeed, 0f, clockwise);
            targetRotations.Add(tuple);*/
            rotaters.Add(new CutsceneRotater(id, rotationSpeed, clockwise));
        }

        // Tuple: (string id, Vector2 targetSize, bool aspectRatio, float tweenDuration, float tweenAmount, Vector2 originSize)
        internal static void AddResizeRectangle(string id, Vector2 targetSize, bool maintainAspectRatio,
            bool tween = false, float tweenDuration = 0f)
        {
            Rectangle rect = ActiveCutscene.rectangles[id];
            Vector2 origin = new Vector2(rect.Width, rect.Height);

            if(!tween)
            {
                Vector2 relativeSize = GetRelativeSize(targetSize.X, targetSize.Y);

                rect.Width = (int)relativeSize.X;
                rect.Height = (int)relativeSize.Y;

                StuxnetCore.cutscenes[StuxnetCore.activeCutsceneID].rectangles[id] = rect;

                return;
            }

            var tuple = Tuple.Create(id, targetSize, maintainAspectRatio, tweenDuration, 0f, origin);
            targetResizeRects.Add(tuple);
        }

        internal static void AddFadeImage(string id, bool fadeIn, float duration)
        {
            var tuple = Tuple.Create(id, 1.0f, fadeIn, duration, 0.0f);
            targetFadeImages.Add(tuple);
        }

        public static Vector2 GetRelativeSize(float width, float height)
        {
            Vector2 relativeVec = new Vector2();
            Viewport viewport = GuiData.spriteBatch.GraphicsDevice.Viewport;

            relativeVec.X = width * viewport.Width;
            relativeVec.Y = height * viewport.Height;

            return relativeVec;
        }

        public static Vector2 GetRelativePosition(float x, float y)
        {
            Vector2 relativeVec = new Vector2();
            Viewport viewport = GuiData.spriteBatch.GraphicsDevice.Viewport;

            relativeVec.X = x * viewport.Width;
            relativeVec.Y = y * viewport.Height;

            return relativeVec;
        }
    }

    internal class CutsceneTweener
    {
        public string ID;
        public Rectangle Rect = default;
        public StuxnetCutsceneImage Image;
        public Vector2 Target;
        public float Duration = 0f;
        public float Amount = 0f;
        public Vector2 Origin = Vector2.Zero;

        public CutsceneTweener() { }

        public CutsceneTweener(string id, Rectangle rect, Vector2 target, float duration)
        {
            ID = id;
            Rect = rect;
            Target = target;
            Duration = duration;
            Origin = new Vector2(rect.X, rect.Y);
        }

        public CutsceneTweener(string id, StuxnetCutsceneImage img, Vector2 target, float duration)
        {
            ID = id;
            Image = img;
            Target = target;
            Duration = duration;
            Origin = new Vector2(img.position.X, img.position.Y);
        }
    }

    internal class CutsceneRotater
    {
        public string ID;
        public float Target;
        public float Duration = 0f;
        public float Speed = -1f;
        public float Amount = 0f;
        public bool Clockwise = true;

        public CutsceneRotater() { }

        public CutsceneRotater(string id, float targetAngle, float duration, bool clockwise = true)
        {
            ID = id;
            Target = targetAngle;
            Duration = duration;
            Clockwise = clockwise;
        }

        public CutsceneRotater(string id, float speed = 1f, bool clockwise = true)
        {
            ID = id;
            Speed = speed;
            Clockwise = clockwise;
        }
    }

    internal class CutsceneResizer
    {
        public string ID;
        public Vector2 Target;
        public bool MaintainAspectRatio;
        public float Duration;
        public float Amount = 0f;
        public Vector2 Origin;
        public bool IsImage = false;

        public CutsceneResizer() { }

        public CutsceneResizer(string id, Vector2 targetSize, bool aspectRatio, float tweenDuration, bool image = false)
        {
            ID = id;
            Target = targetSize;
            MaintainAspectRatio = aspectRatio;
            Duration = tweenDuration;
            IsImage = image;
        }
    }
}
