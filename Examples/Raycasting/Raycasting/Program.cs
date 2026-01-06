using LibGFX.Graphics.Renderer.OpenGL;

namespace Raycasting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MyGame game = new MyGame();
            game.TargetFrameRate = 250;
            game.Run(new GLRenderer(), 800, 600, "Test", false);
        }
    }
}
