using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Pathfinder.GUI;
using Pathfinder.Options;

namespace StuxnetHN.Audio.Options
{
    // Taken from https://github.com/prodzpod/ZeroDayToolKit/blob/main/Options/OptionSlider.cs
    public class OptionSlider : Option
    {
        public float Value;
        public int ButtonID = PFButton.GetNextID();
        public override int SizeX => 210;
        public override int SizeY => 30;

        public float MinValue = 0.0f;
        public float MaxValue = 1.0f;
        public float Step = 0.001f;

        public OptionSlider(string name, string description = "", float defVal = 0.5f) : base(name, description)
        {
            Value = defVal;
        }

        public override void Draw(int x, int y)
        {
            TextItem.doLabel(new Vector2(x, y), Name, new Color?(), 200f);
            var nameBox = GuiData.font.MeasureString(Name);
            int xOffset = (int)nameBox.X + 5;
            Value = SliderBar.doSliderBar(ButtonID, x + xOffset, y, SizeX, SizeY, MaxValue, MinValue, Value, Step);
            TextItem.doSmallLabel(new Vector2(x, y + nameBox.Y + 2), Description, new Color?());
        }
    }
}