using LibGFX;
using LibGFX.Assets;
using LibGFX.Core;
using LibGFX.Core.GameElements;
using LibGFX.Graphics;
using LibGFX.Graphics.Enviroment;
using LibGFX.Graphics.Lights;
using LibGFX.Graphics.Materials;
using LibGFX.Graphics.Primitives;
using LibGFX.Graphics.Renderer.OpenGL;
using OpenTK.Mathematics;

public class MyGame : Game
{
    private Scene3D _scene;
    private PerspectiveCamera _camera;

    private Mesh _cubeMesh;
    private SGMaterial _defaultMaterial;

    public override void LoadContent(AssetManager assets)
    {
        // Create assets (CPU-side only)
        _defaultMaterial = assets.Add(
            new SGMaterial("DefaultMaterial", new Vector4(0.8f, 0.8f, 0.8f, 1.0f))
        );

        _cubeMesh = assets.Add<Mesh>(
            "CubeMesh",
            Cube.GetMesh(_defaultMaterial)
        );
    }

    public override void Initialize(IRenderDevice renderer)
    {
        // Create camera
        _camera = new PerspectiveCamera(
            new Vector3(0, 3, -6),
            new Vector2(Viewport.Width, Viewport.Height)
        );
        _camera.SetAsCurrent();

        // Create scene
        _scene = new Scene3D();
        _scene.Enviroment = new ProceduralSky();

        // Add light
        _scene.DirectionalLight = new DirectionalLight3D(
            new Vector3(-0.2f, 1.0f, -0.3f),
            new Vector4(1.0f),
            1.0f
        );

        // Add primitive
        var cube = new Primitive("Cube", _cubeMesh);
        cube.Transform.Position = Vector3.Zero;
        _scene.AddGameElement(cube);

        // Initialize scene
        _scene.Init(Viewport, renderer);
    }

    public override void OnStart()
    {
        // Optional final setup before the game loop starts
    }

    public override void Update(float deltaTime)
    {
        _camera.LookAt(Vector3.Zero);
        _scene.UpdatePhysics(deltaTime);
        _scene.Update(deltaTime);
    }

    public override void Render()
    {
        _scene.RenderShadowMaps(Viewport, RenderDevice, _camera);
        _scene.Render(Viewport, RenderDevice, _camera);

        RenderDevice.DrawRenderTarget(_scene.RenderTarget as MSAARenderTarget2D, GLRenderer.Backbuffer);
    }

    public override void OnFrameEnd()
    {
        _scene.EnqueElements();
    }

    public override void Dispose()
    {
        _scene.DisposeScene(RenderDevice);
    }
}