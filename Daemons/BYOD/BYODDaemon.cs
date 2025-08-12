using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BepInEx;
using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Daemon;
using Pathfinder.GUI;
using Pathfinder.Meta.Load;
using Pathfinder.Util.XML;
using Stuxnet_HN.Daemons.BYOD;
using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.Daemons
{
    [Daemon]
    public class BYODDaemon : BaseDaemon
    {
        public BYODDaemon(Computer computer, string sn, OS os) : base(computer, sn, os) { }

        public bool IncludeExitButton = true;
        public List<BYODElement> Elements = new();

        public int ExitButtonID;

        public override void initFiles()
        {
            if(IncludeExitButton)
            {
                ExitButtonID = PFButton.GetNextID();
            }
            base.initFiles();
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            foreach(var elem in Elements)
            {
                elem.Bounds = bounds;
                elem.Draw();
            }

            if(IncludeExitButton)
            {
                if(Button.doButton(ExitButtonID, bounds.X + 10, bounds.Height + bounds.Y - 30,
                    bounds.Width / 8, 20, "Exit...", Color.Red))
                {
                    os.display.command = "connect";
                }
            }
        }

        private const string VECTOR2_TEMPLATE = "{0},{1}";

        private static string getColorString(Color color)
        {
            return Utils.convertColorToParseableString(color);
        }

        public override XElement GetSaveElement()
        {
            XElement saveElem = new("BYODDaemon");

            foreach(var elem in Elements)
            {
                XElement childElem = null;
                XAttribute[] baseAttrs =
                {
                    new("Position", string.Format(VECTOR2_TEMPLATE, elem.Position.X, elem.Position.Y)),
                    new("Size", string.Format(VECTOR2_TEMPLATE, elem.Size.X, elem.Size.Y))
                };

                if(elem is BYODRectangle rectangle)
                {
                    childElem = new("Rectangle");
                    childElem.Add(new XAttribute("Color", getColorString(rectangle.Color)));
                } else if(elem is BYODImage image)
                {
                    childElem = new("Image");
                    childElem.Add(new XAttribute("Filepath", image.Filepath));
                } else if(elem is BYODLabel label)
                {
                    childElem = new("Label");
                    childElem.Add(
                        new XAttribute("FontType", label.FontType),
                        new XAttribute("FontSize", label.FontSizeMultiplier),
                        new XAttribute("TextColor", getColorString(label.TextColor)),
                        label.Text
                        );
                } else if(elem is BYODButton button)
                {
                    childElem = new("Button");
                    childElem.Add(
                        new XAttribute("OnPressedActions", button.ActionsFilepath),
                        new XAttribute("Color", getColorString(button.Color)),
                        new XAttribute("IsExitButton", button.Exits),
                        button.Text
                        );
                } else if(elem is BYODWarningTape tape)
                {
                    childElem = new("WarningTape");
                    childElem.Add(
                        new XAttribute("PrimaryColor", getColorString(tape.PrimaryColor)),
                        new XAttribute("SecondaryColor", getColorString(tape.SecondaryColor))
                        );
                } else
                {
                    throw new FormatException("Invalid child element of BYODDaemon! The following caused this error: " +
                        childElem.Name);
                }

                if (childElem != null)
                {
                    childElem.Add(baseAttrs);
                    saveElem.Add(childElem);
                }
            }

            return base.GetSaveElement();
        }

        public override void LoadFromXml(ElementInfo info)
        {
            if (info.Children.Count == 0)
            {
                throw new FormatException("BYODDaemon element needs at least one child element!");
            }

            name = info.ReadRequiredAttribute("Name");
            IncludeExitButton = true;

            if(info.Attributes.ContainsKey("IncludeExitButton"))
            {
                IncludeExitButton = bool.Parse(info.Attributes["IncludeExitButton"]);
            }

            foreach (var child in info.Children)
            {
                var elem = ParseElementAsBYODElement(child);
                if(elem != null)
                {
                    Elements.Add(elem);
                }
            }
            base.LoadFromXml(info);
        }

        private static readonly List<string> _validElements = new()
        {
            "Rectangle", "Image", "Label", "Button", "WarningTape"
        };

        public static BYODElement ParseElementAsBYODElement(ElementInfo info)
        {
            string invalidException = "Element <{0} /> is an invalid BYOD element or has been formatted incorrectly" +
                " - please check your code!";

            if (!info.Attributes.ContainsKey("Position") || (info.Name != "Label" && !info.Attributes.ContainsKey("Size")) ||
                !_validElements.Contains(info.Name))
            {
                throw new FormatException(string.Format(invalidException, info.Name));
            }

            if ((info.Name == "Label" || info.Name == "Button") && info.Content.IsNullOrWhiteSpace())
            {
                throw new FormatException(string.Format(invalidException, info.Name));
            }

            Vector2 position = new Vector2().FromString(info.Attributes["Position"]);
            Vector2 size = Vector2.Zero;

            if (info.Name != "Label")
            {
                size = new Vector2().FromString(info.Attributes["Size"]);
            }

            switch (info.Name)
            {
                case "Rectangle":
                    string color = info.ReadRequiredAttribute("Color");
                    BYODRectangle rectangle = new(position, size, color);
                    return rectangle;
                case "Image":
                    string imagePath = info.ReadRequiredAttribute("Path");
                    BYODImage image = new(position, size, imagePath);
                    return image;
                case "Label":
                    string text = info.Content;
                    string font = info.Attributes.ContainsKey("FontType") ? info.Attributes["FontType"] : "normal";
                    float multiplier = info.Attributes.ContainsKey("FontSize") ? float.Parse(info.Attributes["FontSize"]) : 1f;
                    string textColor = info.Attributes.ContainsKey("TextColor") ? info.Attributes["TextColor"]
                        : Utils.convertColorToParseableString(Color.White);
                    BYODLabel label = new(position, multiplier, text, font, textColor);
                    return label;
                case "Button":
                    string actions = info.Attributes.ContainsKey("OnPressedActions")
                        ? info.Attributes["OnPressedActions"]
                        : string.Empty;
                    string buttonText = info.Content;
                    string buttonColor = info.Attributes.ContainsKey("Color") ? info.Attributes["Color"]
                        : Utils.convertColorToParseableString(Color.White);
                    bool exits = !info.Attributes.ContainsKey("IsExitButton") || bool.Parse(info.Attributes["IsExitButton"]);
                    BYODButton button = new(position, size, actions, buttonText, buttonColor, exits);
                    return button;
                case "WarningTape":
                case "TickerTape":
                    string primary = info.ReadRequiredAttribute("PrimaryColor");
                    string secondary = info.ReadRequiredAttribute("SecondaryColor");
                    BYODWarningTape tape = new(position, size, primary, secondary);
                    return tape;
                default:
                    StuxnetCore.Logger.LogWarning("Invalid BYOD Element");
                    return null;
            }
        }
    }
}
