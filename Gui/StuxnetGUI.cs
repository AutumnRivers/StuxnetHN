using Microsoft.Xna.Framework;

namespace Stuxnet_HN.Gui
{
    public interface IStuxnetGuiElement
    {
        public void Draw(Rectangle bounds);
    }

    public interface IUpdateableGuiElement : IStuxnetGuiElement
    {
        public void Update(GameTime gameTime);
    }
}
