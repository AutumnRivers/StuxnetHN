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

        // Resizers (Rewrite)
        internal static List<CutsceneResizer> resizers = new();

        // Faders (Rewrite)
        internal static List<CutsceneFader> faders = new();

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
            DoActionIfListIsNotEmpty(rotaters, ActivateRotater);
            DoActionIfListIsNotEmpty(resizers, ActivateResizer);
            DoActionIfListIsNotEmpty(faders, ActivateFader);
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

        private static void ActivateFader(StuxnetCutscene cs, float gt, int i)
        {
            CutsceneFader fader = faders[i];

            float targetOpacity = fader.FadeIn ? 1.0f : 0.0f;
            float startingOpacity = fader.FadeIn ? 0.0f : 1.0f;
            float lerpAmount = gt / fader.Duration;
            fader.Amount += lerpAmount;

            float newOpacity = MathHelper.Lerp(startingOpacity, targetOpacity, fader.Amount);

            if(fader.Image != null)
            {
                StuxnetCutsceneImage refImage = cs.images[fader.ID];
                refImage.opacity = newOpacity;
                cs.images[fader.ID] = refImage;
            } else if(fader.Rectangle != default)
            {
                // Register color here
            }

            if (fader.Amount >= 1.0f)
            {
                faders.RemoveAt(i);
                return;
            }

            faders[i] = fader;
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

        private static void ActivateResizer(StuxnetCutscene cs, float gt, int i)
        {
            CutsceneResizer resizer = resizers[i];

            gt /= 60; // Fixes speed
            bool tween = resizer.Tween;

            if (!tween)
            {
                if(resizer.IsImage)
                {
                    StuxnetCutsceneImage image = cs.images[resizer.ID];
                    image.size = resizer.Target;
                    cs.images[resizer.ID] = image;
                } else
                {
                    Rectangle rect = cs.rectangles[resizer.ID];
                    rect.Width = (int)Math.Round(resizer.Target.X);
                    rect.Height = (int)Math.Round(resizer.Target.Y);
                    cs.rectangles[resizer.ID] = rect;
                }

                resizers.RemoveAt(i);
                return;
            }

            float lerpAmount = tween ? gt / resizer.Duration : 0;
            float newAmount = lerpAmount + resizer.Amount;
            resizer.Amount = newAmount;

            if(resizer.IsImage)
            {
                StuxnetCutsceneImage image = cs.images[resizer.ID];
                resizeImage(image);
            } else
            {
                Rectangle rectangle = cs.rectangles[resizer.ID];
                resizeRectangle(rectangle);
            }

            void resizeRectangle(Rectangle rect)
            {
                Vector2 newSize = Vector2.Zero;
                Vector2 currentSize = new(rect.Width, rect.Height);

                if(resizer.MaintainAspectRatio)
                {
                    newSize = rect.LerpSizeAspect(resizer.Target, resizer.Origin, resizer.Amount);
                } else
                {
                    GraphicsDevice userGraphics = GuiData.spriteBatch.GraphicsDevice;
                    Viewport viewport = userGraphics.Viewport;
                    Vector2 targetCalculatedSize = Vector2.Lerp(resizer.Origin, resizer.Target, resizer.Amount);

                    Vector2 calculatedNewSize = new(
                        targetCalculatedSize.X * viewport.Width,
                        targetCalculatedSize.Y * viewport.Height
                        );

                    newSize = calculatedNewSize;
                }

                rect.Width = (int)newSize.X;
                rect.Height = (int)newSize.Y;
                cs.rectangles[resizer.ID] = rect;

                finishUp();
            }

            void resizeImage(StuxnetCutsceneImage image)
            {

            }

            void finishUp()
            {
                if(resizer.Amount >= 1.0f)
                {
                    resizers.RemoveAt(i);
                } else
                {
                    resizers[i] = resizer;
                }
            }
        }

        internal static void AddTweenedRectangle(string id, Rectangle rect, Vector2 targetVec, float tweenDuration)
        {
            tweeners.Add(new CutsceneTweener(id, rect, targetVec, tweenDuration));
        }

        internal static void AddTweenedImage(string id, StuxnetCutsceneImage img, Vector2 targetVec, float tweenDuration)
        {
            tweeners.Add(new CutsceneTweener(id, img, targetVec, tweenDuration));
        }

        internal static void AddTimedRotation(string id, float targetAngle, float duration, bool clockwise)
        {
            rotaters.Add(new CutsceneRotater(id, targetAngle, duration, clockwise));
        }

        internal static void AddInfiniteRotation(string id, float rotationSpeed, bool clockwise)
        {
            rotaters.Add(new CutsceneRotater(id, rotationSpeed, clockwise));
        }

        internal static void AddResizeRectangle(string id, Vector2 targetSize, Vector2 originSize, bool maintainAspectRatio,
            float tweenDuration = 0f)
        {
            resizers.Add(new CutsceneResizer(id, targetSize, maintainAspectRatio, tweenDuration));
        }

        internal static void AddFadeImage(string id, StuxnetCutsceneImage img, bool fadeIn, float duration)
        {
            faders.Add(new CutsceneFader(id, img, fadeIn, duration));
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

    internal class CutsceneTweener : CutsceneActionizer
    {
        public Rectangle Rect = default;
        public StuxnetCutsceneImage Image;
        public Vector2 Target;
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

    internal class CutsceneRotater : CutsceneActionizer
    {
        public float Target;
        public float Speed = -1f;
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

    internal class CutsceneResizer : CutsceneActionizer
    {
        public Vector2 Target;
        public bool MaintainAspectRatio;
        public bool Tween = false;
        public Vector2 Origin;
        public bool IsImage = false;

        public CutsceneResizer() { }

        public CutsceneResizer(string id, Vector2 targetSize, bool aspectRatio, float tweenDuration, bool image = false)
        {
            ID = id;
            Target = targetSize;
            MaintainAspectRatio = aspectRatio;
            if(tweenDuration > 0f)
            {
                Duration = tweenDuration;
                Tween = true;
            }
            IsImage = image;
        }
    }

    internal class CutsceneFader : CutsceneActionizer
    {
        public bool FadeIn;
        public Rectangle Rectangle;
        public StuxnetCutsceneImage Image;

        public CutsceneFader(string id, bool fadeIn, float fadeDuration)
        {
            ID = id;
            FadeIn = fadeIn;
            Duration = fadeDuration;
        }

        public CutsceneFader(string id, Rectangle rect, bool fadeIn, float fadeDuration)
        {
            ID = id;
            FadeIn = fadeIn;
            Duration = fadeDuration;
            Rectangle = rect;
        }

        public CutsceneFader(string id, StuxnetCutsceneImage image, bool fadeIn, float fadeDuration)
        {
            ID = id;
            FadeIn = fadeIn;
            Duration = fadeDuration;
            Image = image;
        }
    }

    internal abstract class CutsceneActionizer
    {
        public string ID;
        public float Duration;
        public float Amount = 0f;
    }
}
