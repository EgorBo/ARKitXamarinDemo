// 
// ArkitApp: provides a Urho.Application subclass that blends Urho with
// ARKit.   
//
using System;
using System.Linq;
using ARKit;
using Urho;
using System.Runtime.CompilerServices;
using Urho.Urho2D;
using UIKit;
using System.Diagnostics;
using Urho.Gui;
using Urho.Navigation;
using Urho.Physics;
using System.Collections.Generic;

namespace ARKitXamarinDemo
{
	/// <summary>
	/// ARKitApp blends UrhoSharp with ARKit by providing an application that has been
	/// configured with a basic scene, a camera, a light and can be fed frames from 
	/// ARKit's ARSession.
	/// </summary>
	public class ArkitApp : Urho.Application
	{
		Texture2D cameraYtexture;
		Texture2D cameraUVtexture;
		bool yuvTexturesInited;
		ARSessionDelegate arSessionDelegate;

		[Preserve]
		/// <summary>
		/// Creates a new instance of ARKIt.   You can provide an custom ARSessionDelegate
		/// that would implement your callbacks for ARSession.  
		/// </summary>
		/// <remarks>
		/// If you do not provide a value,
		/// a default implementation that calls the ProcessARFrame on each ARSessionDelegate.DidUpdateFrame
		/// call is provided.
		/// </remarks>
		public ArkitApp(ApplicationOptions opts) : base(opts) { }

		public Viewport Viewport { get; private set; }
		public Scene Scene { get; private set; }
		public Zone Zone { get; private set; }
		public Octree Octree { get; private set; }
		public Node CameraNode { get; private set; }
		public Camera Camera { get; private set; }
		public Node LightNode { get; private set; }
		public Light Light { get; private set; }
		public MonoDebugHud DebugHud { get; private set; }
		public ARSession ARSession { get; private set; }
		public Node AnchorsNode { get; private set; }
		public Node FeaturePointsCloudeNode { get; private set; }
		public bool ContinuesHitTestAtCenter { get; set; }
		public Vector3? LastHitTest { get; private set; }
		public bool PlaneDetectionEnabled { get; set; } = true;

		void CreateArScene()
		{
			// 3D scene with Octree and Zone
			Scene = new Scene(Context);
			Octree = Scene.CreateComponent<Octree>();
			Zone = Scene.CreateComponent<Zone>();
			Zone.AmbientColor = Color.White * 0.2f;
			Scene.CreateComponent<PhysicsWorld>();

			// Camera
			CameraNode = Scene.CreateChild(name: "Camera");
			Camera = CameraNode.CreateComponent<Camera>();

			// Light
			LightNode = Scene.CreateChild(name: "DirectionalLight");
			LightNode.SetDirection(new Vector3(0.8f, -1.0f, 0f));
			Light = LightNode.CreateComponent<Light>();
			Light.LightType = LightType.Directional;
			Light.CastShadows = true;
			Light.Brightness = 1.5f;
			Light.ShadowResolution = 8;
			Light.ShadowIntensity = 0.5f;
			Renderer.ShadowMapSize *= 8;

			// Viewport
			Viewport = new Viewport(Context, Scene, Camera, null);
			Viewport.SetClearColor(Color.Transparent);
			Renderer.SetViewport(0, Viewport);

			DebugHud = new MonoDebugHud(this);
			DebugHud.FpsOnly = true;
			DebugHud.Show(Color.Black, 45);

			AnchorsNode = Scene.CreateChild();
			FeaturePointsCloudeNode = Scene.CreateChild();
		}

		protected override void Start()
		{
			CreateArScene();

			arSessionDelegate = new UrhoARSessionDelegate(this);
			ARSession = new ARSession { Delegate = arSessionDelegate };
			var config = new ARWorldTrackingConfiguration();
			//config.WorldAlignment = ARWorldAlignment.GravityAndHeading;
			config.PlaneDetection = ARPlaneDetection.Horizontal;
			ARSession.Run(config, ARSessionRunOptions.RemoveExistingAnchors);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		unsafe protected void ApplyTransform(Node node, OpenTK.NMatrix4 matrix)
		{
			Matrix4 urhoTransform = *(Matrix4*)(void*)&matrix;
			var rotation = urhoTransform.Rotation;
			rotation.Z *= -1;
			var pos = urhoTransform.Row3;
			node.SetWorldPosition(new Vector3(pos.X, pos.Y, -pos.Z));
			node.Rotation = rotation;
		}

		public unsafe void ProcessARFrame(ARSession session, ARFrame frame)
		{
			var arcamera = frame?.Camera;
			var transform = arcamera.Transform;
			var prj = arcamera.GetProjectionMatrix(UIInterfaceOrientation.LandscapeRight, new CoreGraphics.CGSize(Graphics.Width, Graphics.Height), 0.01f, 30f);

			//Urho accepts projection matrix in DirectX format (negative row3 + transpose)
			var urhoProjection = *(Matrix4*)(void*)&prj;
			urhoProjection.Row2 *= -1;
			urhoProjection.Transpose();

			Camera.SetProjection(urhoProjection);
			ApplyTransform(CameraNode, transform);

			if (!yuvTexturesInited)
			{
				var img = frame.CapturedImage;

				// texture for Y-plane;
				cameraYtexture = new Texture2D();
				cameraYtexture.SetNumLevels(1);
				cameraYtexture.FilterMode = TextureFilterMode.Bilinear;
				cameraYtexture.SetAddressMode(TextureCoordinate.U, TextureAddressMode.Clamp);
				cameraYtexture.SetAddressMode(TextureCoordinate.V, TextureAddressMode.Clamp);
				cameraYtexture.SetSize((int)img.Width, (int)img.Height, Graphics.LuminanceFormat, TextureUsage.Dynamic);
				cameraYtexture.Name = nameof(cameraYtexture);
				ResourceCache.AddManualResource(cameraYtexture);

				// texture for UV-plane;
				cameraUVtexture = new Texture2D();
				cameraUVtexture.SetNumLevels(1);
				cameraUVtexture.SetSize((int)img.GetWidthOfPlane(1), (int)img.GetHeightOfPlane(1), Graphics.LuminanceAlphaFormat, TextureUsage.Dynamic);
				cameraUVtexture.FilterMode = TextureFilterMode.Bilinear;
				cameraUVtexture.SetAddressMode(TextureCoordinate.U, TextureAddressMode.Clamp);
				cameraUVtexture.SetAddressMode(TextureCoordinate.V, TextureAddressMode.Clamp);
				cameraUVtexture.Name = nameof(cameraUVtexture);
				ResourceCache.AddManualResource(cameraUVtexture);

				RenderPath rp = new RenderPath();
				rp.Load(ResourceCache.GetXmlFile("ARRenderPath.xml"));
				var cmd = rp.GetCommand(1); //see ARRenderPath.xml, second command.
				cmd->SetTextureName(TextureUnit.Diffuse, cameraYtexture.Name); //sDiffMap
				cmd->SetTextureName(TextureUnit.Normal, cameraUVtexture.Name); //sNormalMap

				var capturedImage = frame.CapturedImage;
				var nativeBounds = UIScreen.MainScreen.NativeBounds;
				float imageAspect = (float)capturedImage.Width / (float)capturedImage.Height;
				float screenAspect = (float)nativeBounds.Size.Height / (float)nativeBounds.Size.Width;

				cmd->SetShaderParameter("CameraScale", screenAspect / imageAspect);

				//rp.Append(CoreAssets.PostProcess.FXAA2);
				Viewport.RenderPath = rp;
				yuvTexturesInited = true;
			}

			if (ContinuesHitTestAtCenter)
				LastHitTest = HitTest();


			// display tracking state (quality)
			DebugHud.AdditionalText = $"{arcamera.TrackingState}\n";
			if (arcamera.TrackingStateReason != ARTrackingStateReason.None)
				DebugHud.AdditionalText += arcamera.TrackingStateReason;

			// see "Render with Realistic Lighting"
			// https://developer.apple.com/documentation/arkit/displaying_an_ar_experience_with_metal
			var ambientIntensity = (float)frame.LightEstimate.AmbientIntensity / 1000f;
			//Light.Brightness = 0.5f + ambientIntensity / 2;
			DebugHud.AdditionalText += "\nAmb: " + ambientIntensity.ToString("F1");

			//use outside of InvokeOnMain?
			if (yuvTexturesInited)
				UpdateBackground(frame);

			// required!
			frame.Dispose();
		}

		unsafe void UpdateBackground(ARFrame frame)
		{
			using (var img = frame.CapturedImage)
			{
				var yPtr = img.BaseAddress;
				var uvPtr = img.GetBaseAddress(1);

				if (yPtr == IntPtr.Zero || uvPtr == IntPtr.Zero)
					return;

				int wY = (int)img.Width;
				int hY = (int)img.Height;
				int wUv = (int)img.GetWidthOfPlane(1);
				int hUv = (int)img.GetHeightOfPlane(1);

				cameraYtexture.SetData(0, 0, 0, wY, hY, (void*)yPtr);
				cameraUVtexture.SetData(0, 0, 0, wUv, hUv, (void*)uvPtr);
			}
		}

		public Vector3? HitTest(float screenX = 0.5f, float screenY = 0.5f) =>
			HitTest(ARSession?.CurrentFrame, screenX, screenY);

		Vector3? HitTest(ARFrame frame, float screenX = 0.5f, float screenY = 0.5f)
		{
			var result = frame?.HitTest(new CoreGraphics.CGPoint(screenX, screenY),
				ARHitTestResultType.ExistingPlaneUsingExtent
				)?.FirstOrDefault();

			if (result != null && result.Distance > 0.2f)
			{
				var row = result.WorldTransform.Column3;
				return new Vector3(row.X, row.Y, -row.Z);
			}
			return null;
		}

		internal void DidAddAnchors(ARAnchor[] anchors)
		{
			if (!PlaneDetectionEnabled)
				return;
			
			foreach (var anchor in anchors)
			{
				UpdateAnchor(null, anchor);
			}
		}

		internal void DidRemoveAnchors(ARAnchor[] anchors)
		{
			if (!PlaneDetectionEnabled)
				return;
			
			foreach (var anchor in anchors)
			{
				AnchorsNode.GetChild(anchor.Identifier.ToString())?.Remove();
			}
		}

		internal void DidUpdateAnchors(ARAnchor[] anchors)
		{
			if (!PlaneDetectionEnabled)
				return;
			
			foreach (var anchor in anchors)
			{
				var node = AnchorsNode.GetChild(anchor.Identifier.ToString());
				UpdateAnchor(node, anchor);
			}
		}

		void UpdateAnchor(Node node, ARAnchor anchor)
		{
			if (anchor is ARPlaneAnchor planeAnchor)
			{
				Material tileMaterial = null;
				Node planeNode = null;
				if (node == null)
				{
					var id = planeAnchor.Identifier.ToString();
					node = AnchorsNode.CreateChild(id);
					planeNode = node.CreateChild("SubPlane");
					var plane = planeNode.CreateComponent<StaticModel>();
					planeNode.Position = new Vector3();
					plane.Model = CoreAssets.Models.Plane;

					tileMaterial = new Material();
					tileMaterial.SetTexture(TextureUnit.Diffuse, ResourceCache.GetTexture2D("Textures/PlaneTile.png"));
					var tech = new Technique();
					var pass = tech.CreatePass("alpha");
					pass.DepthWrite = false;
					pass.BlendMode = BlendMode.Alpha;
					pass.PixelShader = "PlaneTile";
					pass.VertexShader = "PlaneTile";
					tileMaterial.SetTechnique(0, tech);
					tileMaterial.SetShaderParameter("MeshColor", new Color(Randoms.Next(), 1, Randoms.Next()));
					tileMaterial.SetShaderParameter("MeshAlpha", 0.8f); // set 0.0f if you want to hide them
					tileMaterial.SetShaderParameter("MeshScale", 32.0f);

					var planeRb = planeNode.CreateComponent<RigidBody>();
					planeRb.Friction = 1.5f;
					CollisionShape shape = planeNode.CreateComponent<CollisionShape>();
					shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);

					plane.Material = tileMaterial;
				}
				else
				{
					planeNode = node.GetChild("SubPlane");
					tileMaterial = planeNode.GetComponent<StaticModel>().Material;
				}

				ApplyTransform(node, planeAnchor.Transform);

				planeNode.Scale = new Vector3(planeAnchor.Extent.X, 0.1f, planeAnchor.Extent.Z);
				planeNode.Position = new Vector3(planeAnchor.Center.X, planeAnchor.Center.Y, -planeAnchor.Center.Z);

				//var animation = new ValueAnimation();
				//animation.SetKeyFrame(0.0f, 0.3f);
				//animation.SetKeyFrame(0.5f, 0.0f);
				//tileMaterial.SetShaderParameterAnimation("MeshAlpha", animation, WrapMode.Once, 1.0f);

				Debug.WriteLine($"ARPlaneAnchor  Extent({planeAnchor.Extent}), Center({planeAnchor.Center}), Position({planeAnchor.Transform.Row3}");
			}
		}
	}

	class UrhoARSessionDelegate : ARSessionDelegate
	{
		WeakReference<ArkitApp> arkitApp;

		public UrhoARSessionDelegate(ArkitApp arkitApp)
		{
			this.arkitApp = new WeakReference<ArkitApp>(arkitApp);
		}

		public override void CameraDidChangeTrackingState(ARSession session, ARCamera camera)
		{
			Console.WriteLine("CameraDidChangeTrackingState");
		}

		public override void DidUpdateFrame(ARSession session, ARFrame frame)
		{
			if (arkitApp.TryGetTarget(out var ap))
				Urho.Application.InvokeOnMain(() => ap.ProcessARFrame(session, frame));
		}

		public override void DidFail(ARSession session, Foundation.NSError error)
		{
			Console.WriteLine("DidFail");
		}

		public override void WasInterrupted(ARSession session)
		{
			base.WasInterrupted(session);
		}

		public override void InterruptionEnded(ARSession session)
		{
			base.InterruptionEnded(session);
		}

		public override void DidAddAnchors(ARSession session, ARAnchor[] anchors)
		{
			if (arkitApp.TryGetTarget(out var ap))
				Urho.Application.InvokeOnMain(() => ap.DidAddAnchors(anchors));
		}

		public override void DidRemoveAnchors(ARSession session, ARAnchor[] anchors)
		{
			if (arkitApp.TryGetTarget(out var ap))
				Urho.Application.InvokeOnMain(() => ap.DidRemoveAnchors(anchors));
		}

		public override void DidUpdateAnchors(ARSession session, ARAnchor[] anchors)
		{
			if (arkitApp.TryGetTarget(out var ap))
				Urho.Application.InvokeOnMain(() => ap.DidUpdateAnchors(anchors));
		}
	}
}