using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SFML;
using SFML.System;
using SFML.Window;
using SFML.Graphics;

namespace InfoBot
{
    internal class Padoru : IDisposable
    {
        private static Texture dark = new Texture("dark.png");
        private static RenderTexture drawer = new RenderTexture(600, 400);
        private static Texture light = new Texture("light.png");
        private static Texture lightBulb = new Texture("light_bulb.png");
        private static Texture padoru = new Texture("padoru.png");
        private static Font trebuchet = new Font("trebuchet.ttf");
        private static Texture wall = new Texture("wall.png");
        private static Texture[] xmas;

        public Padoru(int days)
        {
            if (xmas == null)
            {
                xmas = new Texture[11];
                for (int i = 1; i <= 11; i++)
                {
                    var str = "xmas" + i.ToString() + ".png";
                    if (File.Exists(str))
                        xmas[i - 1] = new Texture(str);
                    else
                        xmas[i - 1] = null;
                }
            }
            var addState = RenderStates.Default;
            addState.BlendMode = BlendMode.Add;
            var text = new Text(days.ToString(), trebuchet);
            text.CharacterSize = 22;
            text.FillColor = new Color(170, 170, 170);
            text.OutlineColor = new Color(25, 25, 40);
            text.Origin = new Vector2f((int)text.GetLocalBounds().Width / 2, 11);
            text.Position = new Vector2f(274, 259);
            text.OutlineThickness = 2;
            drawer.Clear();
            drawer.Draw(new Sprite(wall));
            if (days / 30 < 5)
                drawer.Draw(new Sprite(xmas[6]));
            if (days / 30 < 4)
                drawer.Draw(new Sprite(xmas[7]));
            if (days / 30 < 9)
                drawer.Draw(new Sprite(xmas[2]));
            if (days / 30 < 8)
                drawer.Draw(new Sprite(xmas[3]));
            if (days / 30 < 6)
                drawer.Draw(new Sprite(xmas[5]));
            if (days / 30 < 2)
                drawer.Draw(new Sprite(xmas[9]));
            drawer.Draw(new Sprite(padoru));
            drawer.Draw(text);
            if (days / 30 < 11)
                drawer.Draw(new Sprite(xmas[0]));
            if (days / 30 < 10)
                drawer.Draw(new Sprite(xmas[1]));
            if (days / 30 < 3)
                drawer.Draw(new Sprite(xmas[8]));
            if (days / 30 < 1)
                drawer.Draw(new Sprite(xmas[10]));
            if (days / 30 < 7)
                drawer.Draw(new Sprite(xmas[4]));
            drawer.Draw(new Sprite(lightBulb));
            drawer.Draw(new Sprite(light), addState);
            drawer.Draw(new Sprite(dark));
            drawer.Display();
            drawer.Texture.CopyToImage().SaveToFile("out.jpg");
            Output = new FileStream("out.jpg", FileMode.Open, FileAccess.Read);
        }

        public Stream Output { get; private set; }

        public void Dispose()
        {
            Output.Dispose();
        }
    }
}