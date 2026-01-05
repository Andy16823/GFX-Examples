using LibGFX.Graphics.Renderer.OpenGL;

namespace GetStarted
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MyGame game= new MyGame();
            game.Run(new GLRenderer());
        }
    }
}
