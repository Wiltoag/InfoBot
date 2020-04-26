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
        private static Texture background = new Texture("padoru.png");
        private static RenderTexture drawer = new RenderTexture(600, 400);
        private static Font trebuchet = new Font("trebuchet.ttf");

        public Padoru(int days)
        {
            var text = new Text(days.ToString(), trebuchet);
            text.CharacterSize = 22;
            text.FillColor = new Color(210, 200, 190);
            text.OutlineColor = new Color(90, 70, 50);
            text.Origin = new Vector2f((int)text.GetLocalBounds().Width / 2, 11);
            text.Position = new Vector2f(274, 259);
            text.OutlineThickness = 2;
            drawer.Clear();
            drawer.Draw(new Sprite(background));
            drawer.Draw(text);
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