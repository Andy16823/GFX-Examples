using LibGFX.Graphics.Renderer.OpenGL;

namespace Raycasting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MyGame game = new MyGame();
            game.Run(new GLRenderer());
        }
    }
}
