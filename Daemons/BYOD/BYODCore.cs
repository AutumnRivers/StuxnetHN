using BepInEx;
using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Daemons.BYOD
{
    public abstract class BYODElement
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Rectangle Bounds { get; set; }

        public Vector2 ParsedSize
        {
            get
            {
                return ParseSize(Size);
            }
        }
        public Vector2 ParsedPosition
        {
            get
            {
                return ParsePosition(Position);
            }
        }

        public BYODElement(Vector2 pos, Vector2 size)
        {
            Position = pos;
            Size = size;
        }

        public abstract void Draw();

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public Vector2 ParseSize(Vector2 sizePercentage)
        {
            if(sizePercentage.X > 1f || sizePercentage.Y > 1f)
            {
                return new Vector2(sizePercentage.X, sizePercentage.Y);
            }

            Vector2 result = new();
            if(sizePercentage.Y < 0)
            {
                result.X = result.Y = Bounds.Height * sizePercentage.X;
            } else
            {
                result.X = Bounds.Width * sizePercentage.X;
                result.Y = Bounds.Height * sizePercentage.Y;
            }

            return result;
        }

        public Vector2 ParsePosition(Vector2 posPercentage)
        {
            if(posPercentage.X > 1f || posPercentage.Y > 1f)
            {
                return new Vector2(Bounds.X + posPercentage.X, Bounds.Y + posPercentage.Y);
            }

            return new Vector2(
                Bounds.X + (Bounds.Width * posPercentage.X),
                Bounds.Y + (Bounds.Height * posPercentage.Y)
                );
        }
    }

    public class BYODRectangle : BYODElement
    {
        public Color Color;

        public BYODRectangle(Vector2 pos, Vector2 size, string rgb) : base(pos, size)
        {
            Color = Utils.convertStringToColor(rgb);
        }

        public override void Draw()
        {
            RenderedRectangle.doRectangle(
                (int)ParsedPosition.X, (int)ParsedPosition.Y,
                (int)ParsedSize.X, (int)ParsedSize.Y,
                Color);
        }
    }

    public class BYODImage : BYODElement
    {
        public string Filepath;
        public Texture2D Image;

        public BYODImage(Vector2 pos, Vector2 size, string filepath) : base(pos, size)
        {
            Filepath = filepath;
            Image = GetImageFromFilepath(Filepath);
        }

        public override void Draw()
        {
            Rectangle destRect = new()
            {
                X = (int)ParsedPosition.X,
                Y = (int)ParsedPosition.Y,
                Width = (int)ParsedSize.X,
                Height = (int)ParsedSize.Y
            };

            GuiData.spriteBatch.Draw(Image, destRect, Color.White);
        }

        public static Texture2D GetImageFromFilepath(string filepath)
        {
            filepath = ExtensionLoader.ActiveExtensionInfo.FolderPath + "/" + filepath;
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException(string.Format("Image file at path {0} does not exist!", filepath));
            }

            FileStream fileStream = File.OpenRead(filepath);
            Texture2D img = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, fileStream);
            fileStream.Close();
            return img;
        }
    }

    public class BYODLabel : BYODElement
    {
        public string Text;
        public SpriteFont Font;
        public string FontType;
        public float FontSizeMultiplier = 1f;
        public Color TextColor;

        public BYODLabel(Vector2 pos, float sizeMultiplier, string text, SpriteFont font, string color) : base(pos, Vector2.Zero)
        {
            Text = text;
            Font = font;
            FontSizeMultiplier = sizeMultiplier;
            TextColor = Utils.convertStringToColor(color);
        }

        public BYODLabel(Vector2 pos, float sizeMultiplier, string text, string fontType, string color) : base(pos, Vector2.Zero)
        {
            Text = text;
            FontSizeMultiplier = sizeMultiplier;
            TextColor = Utils.convertStringToColor(color);

            Font = fontType.ToLowerInvariant() switch
            {
                "small" => GuiData.smallfont,
                "tiny" or "terminal" => GuiData.tinyfont,
                "title" or "hacknet" => GuiData.titlefont,
                _ => GuiData.font,
            };
            FontType = fontType;
        }

        public override void Draw()
        {
            GuiData.spriteBatch.DrawString(Font, Text, ParsedPosition, TextColor, 0f, Vector2.Zero, FontSizeMultiplier,
                SpriteEffects.None, 1);
        }
    }

    public class BYODButton : BYODElement
    {
        public string ActionsFilepath;
        public string Text;
        public bool Exits = true;
        public Color Color;

        public int ButtonID = PFButton.GetNextID();

        public BYODButton(Vector2 pos, Vector2 size, string actions, string text, string color, bool exits = true) : base(pos, size)
        {
            ActionsFilepath = actions;
            Text = text;
            Color = Utils.convertStringToColor(color);
            Exits = exits;
        }

        public override void Draw()
        {
            bool customButton = Button.doButton(ButtonID, (int)ParsedPosition.X, (int)ParsedPosition.Y,
                (int)ParsedSize.X, (int)ParsedSize.Y, Text, Color);

            if(customButton)
            {
                if(!ActionsFilepath.IsNullOrWhiteSpace())
                {
                    RunnableConditionalActions.LoadIntoOS(ActionsFilepath, OS.currentInstance);
                }

                if(Exits)
                {
                    OS.currentInstance.display.command = "connect";
                }
            }
        }

        public override void Dispose()
        {
            PFButton.ReturnID(ButtonID);
        }
    }

    public class BYODWarningTape : BYODElement
    {
        public Color PrimaryColor;
        public Color SecondaryColor;

        public BYODWarningTape(Vector2 pos, Vector2 size, string primaryColor, string secondColor) : base(pos, size)
        {
            PrimaryColor = Utils.convertStringToColor(primaryColor);
            SecondaryColor = Utils.convertStringToColor(secondColor);
        }

        public override void Draw()
        {
            Rectangle dest = new()
            {
                X = (int)ParsedPosition.X,
                Y = (int)ParsedPosition.Y,
                Width = (int)ParsedSize.X,
                Height = (int)ParsedSize.Y
            };

            PatternDrawer.draw(dest, 1f, SecondaryColor, PrimaryColor, GuiData.spriteBatch);
        }
    }
}
