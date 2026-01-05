using BulletSharp;
using LibGFX;
using LibGFX.Core;
using LibGFX.Graphics;
using LibGFX.Math;
using LibGFX.Physics;
using LibGFX.Physics.Behaviors;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raycasting
{
    internal class FirstPersonBehavior : IGameBehavior
    {
        private GameElement _element;
        private Vector2 _lastMousePosition;
        private float _mouseSensitivity = 0.05f;

        public void SetElement(GameElement gameElement)
        {
            _element = gameElement;
        }

        public GameElement GetElement()
        {
            return _element;
        }

        public void OnCollide(Collision collision)
        {
            
        }

        public void OnDispose(BaseScene scene, IRenderDevice renderer)
        {
            
        }

        public void OnInit(BaseScene scene, Viewport viewport, IRenderDevice renderer)
        {
            var rigidBody = _element.GetBehavior<RigidBodyBehavior>();
            if (rigidBody == null)
            {
                throw new Exception("FirstPersonBehavior requires RigidBodyBehavior to be attached to the same GameElement.");
            }
            rigidBody.SetAngularFactor(new Vector3(0, 0, 0));

            var window = GFX.Instance.GetWindow();
            if (window != null)
            {
                _lastMousePosition = window.GetMousePosition();
            }
        }

        public void OnRender(BaseScene scene, Viewport viewport, IRenderDevice renderer, Camera camera)
        {
            
        }

        public void OnShadowPass(BaseScene scene, Viewport viewport, IRenderDevice renderer)
        {
            
        }

        public void OnUpdate(BaseScene scene, float dt)
        {
            var rigidBody = _element.GetBehavior<RigidBodyBehavior>();
            if (rigidBody != null)
            {
                var window = GFX.Instance.GetWindow();
                if (window != null)
                {
                    // Hide cursor if window is focused
                    if(window.IsFocused())
                    {
                        window.HideCursor();
                    }
                    else
                    {
                        window.ShowCursor();
                    }

                    // Handle movement input
                    var velocity = new Vector3(0, 0, 0);
                    var speed = 10.0f;
                    if (window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
                    {
                        velocity += _element.Transform.GetFront() * speed;
                    }
                    else if (window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
                    {
                        velocity -= _element.Transform.GetFront() * speed;
                    }

                    if( window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A))
                    {
                        velocity -= _element.Transform.GetRight() * speed;
                    }
                    else if (window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D))
                    {
                        velocity += _element.Transform.GetRight() * speed;
                    }
                    rigidBody.SetLinearVelocity(velocity);

                    // Handle mouse look
                    var viewport = window.GetViewport();
                    var mousePos = window.GetMousePosition();
                    var delta = new Vector2(mousePos.X - viewport.Width / 2, viewport.Height / 2 - mousePos.Y);
                    window.SetMousePosition(viewport.Width / 2, viewport.Height / 2);

                    _element.Transform.Rotate(0, -delta.X * _mouseSensitivity, 0);
                    rigidBody.Sync(); 

                    var camera = Camera.Current;
                    if (camera != null)
                    {
                        camera.Transform.Position = _element.Transform.Position + new Vector3(0, 1.6f, 0);
                        camera.Transform.Rotate(-delta.Y * _mouseSensitivity, -delta.X * _mouseSensitivity, 0);
                    }
                }
            }
        }
    }
}
