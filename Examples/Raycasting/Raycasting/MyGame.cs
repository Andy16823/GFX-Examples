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
using System.Xml.XPath;

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
        // Create default material and debug material
        _defaultMaterial = assets.Add(
            new SGMaterial("DefaultMaterial", new Vector4(0.8f, 0.8f, 0.8f, 1.0f))
        );
        _debugMaterial = assets.Add(
            new SGMaterial("DebugMaterial", new Vector4(1.0f, 0.0f, 0.0f, 1.0f))
        );

        // Create cube and sphere meshes
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
        // Create compute raycaster and initialize it with the renderer
        _computeRaycaster = new ComputeRaycast();
        _computeRaycaster.Init(renderer);

        // Setup camera
        _camera = new PerspectiveCamera(
            new Vector3(0, 3, -6),
            new Vector2(Viewport.Width, Viewport.Height)
        );
        _camera.SetAsCurrent();

        // Setup scene
        _scene = new Scene3D();
        _scene.Enviroment = new ProceduralSky();
        _scene.PhysicsHandler = new PhysicsHandler3D(new Vector3(0, -9.6f, 0));
        _scene.DirectionalLight = new DirectionalLight3D(
            new Vector3(-0.2f, 1.0f, -0.3f),
            new Vector4(1.0f),
            1.0f
        );

        // Create ground cube
        var cube = new Primitive("Cube", _cubeMesh);
        cube.Transform.Position = Vector3.Zero;
        cube.Transform.Scale = new Vector3(50.0f, 0.25f, 50.0f);
        var cubeCollider = cube.AddBehavior<BoxCollider>(new BoxCollider(_scene.PhysicsHandler));
        cubeCollider.CreateCollider(0f);
        _scene.AddGameElement(cube);

        // Create test cube
        _testCube = new Primitive("Cube2", _cubeMesh);
        _testCube.Transform.Position = new Vector3(0.0f, 1.5f, 0.0f);
        _testCube.Transform.Scale = new Vector3(2.0f, 5.0f, 2.0f);
        var cube2Collider = _testCube.AddBehavior<BoxCollider>(new BoxCollider(_scene.PhysicsHandler));
        cube2Collider.CreateCollider(0f);
        _scene.AddGameElement(_testCube);

        // Create debug sphere
        _debugSphere = new Primitive("DebugSphere", _sphereMesh);
        _debugSphere.Transform.Scale = new Vector3(0.2f);
        _scene.AddGameElement(_debugSphere);

        // Create player (movable capsule)
        var player = new Empty("Player", new Vector3(0.0f, 1.0f, -5.0f));
        var playerRigidBdy = player.AddBehavior<CapsuleRigidBody>(new CapsuleRigidBody(_scene.PhysicsHandler));
        playerRigidBdy.CreateRigidBody(10.0f);
        var fpsBehavior = player.AddBehavior<FirstPersonBehavior>(new FirstPersonBehavior());
        _scene.AddGameElement(player);

        // Initialize the scene
        _scene.Init(Viewport, renderer);
    }

    public override void OnStart()
    {
        
    }

    public override void Update(float deltaTime)
    {
        // Perform raycast each frame
        this.PerformRaycast(deltaTime);

        // Switch raycast method based on user input
        if (Window.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F1))
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

        // Close the window if Escape is pressed
        if (Window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
        {
            Window.Close();
        }

        // Update the scene
        _scene.UpdatePhysics(deltaTime);
        _scene.Update(deltaTime);
    }

    public override void Render()
    {
        // Render the scene
        _scene.RenderShadowMaps(Viewport, RenderDevice, _camera);
        _scene.Render(Viewport, RenderDevice, _camera);

        // Render the scene render target to the backbuffer
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

    private void PerformRaycast(float dt)
    {
        // First we need to create a ray from the camera through the screen point (cursor position)
        var ray = Ray.FromScreenPoint(
                    _camera,
                    Window.GetViewport(),
                    Viewport.Width / 2, Viewport.Height / 2);

        // Now we can perform the raycast based on the selected method
        var result = new HitResult() { hit = false };
        switch (_raycastType)
        {
            case RaycastType.PhysicsEngine:
                // Perform raycast using bullet3 physics engine (very fast = usage for games)
                // Usage example: gameplay, shooting, AI line of sight, etc.
                // Note: The hitElement is set automatically by the physics engine.
                // Note: Allways prefer this method for gameplay related raycasts.
                result = Raycast.PerformRaycast(ray, _scene.PhysicsHandler);
                break;
            case RaycastType.CPUMesh:
                // Perform raycast using CPU mesh intersection (faster for single mesh, slower for complex scenes)
                // Usage example: precise mesh interaction, editor tools, etc.
                // Note: FreeCPUResources must be set to false in order to use this method.
                // Note: You need to set the reference to the game element manually after the raycast.
                result = MeshRaycast.IntersectsMesh(ray, _testCube.Transform, _testCube.Mesh);
                if(result.hit)
                {
                    result.hitElement = _testCube;
                }
                break;
            case RaycastType.GPUMesh:
                // Perform raycast using GPU Compute Shader (for complex scenes and many objects)
                // Usage example: complex scene interaction, large number of objects
                // Note: Dont use it with V-Sync enabled, as it may cause synchronization issues.
                // Note: You need to set the reference to the game element manually after the raycast.
                result = _computeRaycaster.PerformRaycast(ray, _testCube.Transform, _testCube.Mesh);
                if (result.hit)
                {
                    result.hitElement = _testCube;
                }
                break;
            default:
                break;
        }

        // With the result, we can now visualize the hit point or perform further actions.
        if (result.hit)
        {
            _debugSphere.Visible = true;
            _debugSphere.Transform.Position = result.hitLocation;
        }
        else
        {
            _debugSphere.Visible = false;
            _debugSphere.Transform.Position = new Vector3(0, -1000, 0);
        }

        Window.SetTitle($"Raycasting Example - Raycast Type: {_raycastType} - Hit: {result.hit} - FPS: {1f / dt}");
    }
}