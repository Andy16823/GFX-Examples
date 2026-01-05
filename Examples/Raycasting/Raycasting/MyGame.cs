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
using LibGFX.Physics;
using LibGFX.Physics.Behaviors3D;
using OpenTK.Mathematics;
using Raycasting;
using System.Diagnostics;

public enum RaycastType
{
    PhysicsEngine,
    CPUMesh,
    GPUMesh
}

public class MyGame : Game
{
    private RaycastType _raycastType = RaycastType.PhysicsEngine;
    private Scene3D _scene;
    private PerspectiveCamera _camera;
    private Mesh _cubeMesh;
    private Mesh _sphereMesh;
    private SGMaterial _defaultMaterial;
    private SGMaterial _debugMaterial;
    private Primitive _testCube;
    private Primitive _debugSphere;
    private ComputeRaycast _computeRaycaster;

    public MyGame()
    {
        this.FreeCPUResources = false;
    }

    public override void LoadContent(AssetManager assets)
    {
        _defaultMaterial = assets.Add(
            new SGMaterial("DefaultMaterial", new Vector4(0.8f, 0.8f, 0.8f, 1.0f))
        );

        _debugMaterial = assets.Add(
            new SGMaterial("DebugMaterial", new Vector4(1.0f, 0.0f, 0.0f, 1.0f))
        );

        _cubeMesh = assets.Add<Mesh>(
            "CubeMesh",
            Cube.GetMesh(_defaultMaterial)
        );

        _sphereMesh = assets.Add<Mesh>(
            "SphereMesh",
            Sphere.GetMesh(_debugMaterial)
        );
    }

    public override void Initialize(IRenderDevice renderer)
    {
        _computeRaycaster = new ComputeRaycast();
        _computeRaycaster.Init(renderer);

        _camera = new PerspectiveCamera(
            new Vector3(0, 3, -6),
            new Vector2(Viewport.Width, Viewport.Height)
        );
        _camera.SetAsCurrent();

        _scene = new Scene3D();
        _scene.Enviroment = new ProceduralSky();
        _scene.PhysicsHandler = new PhysicsHandler3D(new Vector3(0, -9.6f, 0));
        _scene.DirectionalLight = new DirectionalLight3D(
            new Vector3(-0.2f, 1.0f, -0.3f),
            new Vector4(1.0f),
            1.0f
        );

        var cube = new Primitive("Cube", _cubeMesh);
        cube.Transform.Position = Vector3.Zero;
        cube.Transform.Scale = new Vector3(50.0f, 0.25f, 50.0f);
        var cubeCollider = cube.AddBehavior<BoxCollider>(new BoxCollider(_scene.PhysicsHandler));
        cubeCollider.CreateCollider(0f);
        _scene.AddGameElement(cube);

        _testCube = new Primitive("Cube2", _cubeMesh);
        _testCube.Transform.Position = new Vector3(0.0f, 1.5f, 0.0f);
        _testCube.Transform.Scale = new Vector3(2.0f, 5.0f, 2.0f);
        var cube2Collider = _testCube.AddBehavior<BoxCollider>(new BoxCollider(_scene.PhysicsHandler));
        cube2Collider.CreateCollider(0f);
        _scene.AddGameElement(_testCube);

        _debugSphere = new Primitive("DebugSphere", _sphereMesh);
        _debugSphere.Transform.Scale = new Vector3(0.2f);
        _scene.AddGameElement(_debugSphere);

        var player = new Empty("Player", new Vector3(0.0f, 1.0f, -5.0f));
        var playerRigidBdy = player.AddBehavior<CapsuleRigidBody>(new CapsuleRigidBody(_scene.PhysicsHandler));
        playerRigidBdy.CreateRigidBody(10.0f);
        var fpsBehavior = player.AddBehavior<FirstPersonBehavior>(new FirstPersonBehavior());
        _scene.AddGameElement(player);

        _scene.Init(Viewport, renderer);
    }

    public override void OnStart()
    {
        
    }

    public override void Update(float deltaTime)
    {
        this.PerformRaycast();

        if(Window.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F1))
        {
            _raycastType = RaycastType.PhysicsEngine;
            Debug.WriteLine("Raycast Type: Physics Engine");
        }
        else if(Window.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F2))
        {
            _raycastType = RaycastType.CPUMesh;
            Debug.WriteLine("Raycast Type: CPU Mesh");
        }
        else if(Window.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F3))
        {
            _raycastType = RaycastType.GPUMesh;
            Debug.WriteLine("Raycast Type: GPU Mesh");
        }

        if (Window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
        {
            Window.Close();
        }

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
        _computeRaycaster.Dispose(RenderDevice);
        _scene.DisposeScene(RenderDevice);
    }

    private void PerformRaycast()
    {
        var cursorPos = Window.GetMousePosition();
        switch (_raycastType)
        {
            case RaycastType.PhysicsEngine:
                var hitResult = Raycast.PerformRaycastFromScreen(
                    _camera,
                    Window.GetViewport(),
                    _scene.PhysicsHandler as PhysicsHandler3D,
                    (int)cursorPos.X, (int)cursorPos.Y);

                if (hitResult.hit)
                {
                    _debugSphere.Transform.Position = hitResult.hitLocation;
                }
                break;
            case RaycastType.CPUMesh:
                var ray = MeshRaycast.ScreenPointToWorldRay(
                    _camera,
                    Window.GetViewport(),
                    cursorPos.X, cursorPos.Y);

                var hit = MeshRaycast.IntersectsMesh(
                    ray,
                    _testCube.Transform,
                    _testCube.Mesh
                );

                if (hit.Hit)
                {
                    _debugSphere.Transform.Position = hit.Position;
                }
                break;
            case RaycastType.GPUMesh:
                var ray2 = MeshRaycast.ScreenPointToWorldRay(
                    _camera,
                    Window.GetViewport(),
                    cursorPos.X, cursorPos.Y);

                var hit2 = _computeRaycaster.PerformRaycast(
                    ray2,
                    _testCube.Transform,
                    _testCube.Mesh);

                if (hit2.TriangleIndex != -1)
                {
                    _debugSphere.Transform.Position = hit2.Position.Xyz;
                }
                break;
            default:
                break;
        }
    }
}