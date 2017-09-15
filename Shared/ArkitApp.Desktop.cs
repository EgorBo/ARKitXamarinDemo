using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Urho;
using Urho.Physics;

namespace ARKitXamarinDemo
{
	partial class ArkitApp
	{
		private float Yaw;
		private float Pitch;

		// we don't have inside-out tracking here
		// so let's just move the camera via mouse
		partial void StartSession()
		{
			Input.SetMouseVisible(true);
			//emulate detected horizontal surface

			var node = CreatePlaneNode("Plane1");
			node.Position = new Vector3(0, -1, 0);
			node.Scale = new Vector3(10, 1, 10);
		}

		protected override void OnUpdate(float timeStep)
		{
			MoveCameraTouches(timeStep);

			const float moveSpeed = 2f;
			if (Input.GetKeyDown(Key.W)) CameraNode.Translate( Vector3.UnitZ * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.S)) CameraNode.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.A)) CameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.D)) CameraNode.Translate( Vector3.UnitX * moveSpeed * timeStep);
			base.OnUpdate(timeStep);

			if (ContinuesHitTestAtCenter)
				LastHitTest = HitTest();
		}
		
		protected void MoveCameraTouches(float timeStep)
		{
			var input = Input;
			for (uint i = 0, num = input.NumTouches; i < num; ++i)
			{
				TouchState state = input.GetTouch(i);
				if (state.TouchedElement != null)
					continue;

				if (state.Delta.X != 0 || state.Delta.Y != 0)
				{
					var camera = CameraNode.GetComponent<Camera>();
					if (camera == null)
						return;

					var graphics = Graphics;
					Yaw += 2 * camera.Fov / graphics.Height * state.Delta.X;
					Pitch += 2 * camera.Fov / graphics.Height * state.Delta.Y;
					CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
				}
				else
				{
					var cursor = UI.Cursor;
					if (cursor != null && cursor.Visible)
						cursor.Position = state.Position;
				}
			}
		}

		public Vector3? HitTest(float screenX = 0.5f, float screenY = 0.5f)
		{
			Ray cameraRay = Camera.GetScreenRay(screenX, screenY);
			var result = Scene.GetComponent<Octree>().Raycast(cameraRay,
				RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
			if (result != null)
			{
				Vector3? pos = null;
				foreach (var rayCastResult in result)
				{
					if (rayCastResult.Node.Name.Contains("Plane"))
					{
						pos = rayCastResult.Position;
						break;
					}
				}
				return pos;
			}
			return null;
		}
	}
}
