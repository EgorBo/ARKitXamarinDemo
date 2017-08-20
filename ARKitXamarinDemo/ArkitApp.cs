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

		void CreateArScene()
		{
			// 3D scene with Octree and Zone
			Scene = new Scene(Context);
			Octree = Scene.CreateComponent<Octree>();
			Zone = Scene.CreateComponent<Zone>();
			Zone.AmbientColor = Color.White * 0.2f;

			// Light
			LightNode = Scene.CreateChild(name: "DirectionalLight");
            LightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
			Light = LightNode.CreateComponent<Light>();
			Light.LightType = LightType.Directional;
			Light.CastShadows = true;

			// Camera
			CameraNode = Scene.CreateChild(name: "Camera");
			Camera = CameraNode.CreateComponent<Camera>();

			// Viewport
			Viewport = new Viewport(Context, Scene, Camera, null);
			Viewport.SetClearColor(Color.Transparent);
			Renderer.SetViewport(0, Viewport);

			DebugHud = new MonoDebugHud(this);
			DebugHud.FpsOnly = true;
			DebugHud.Show(Color.Black, 45);

			AnchorsNode = Scene.CreateChild();
		}

		protected override void Start ()
		{
			CreateArScene ();

			arSessionDelegate = new UrhoARSessionDelegate(this);
			ARSession = new ARSession() { Delegate = arSessionDelegate };
			var config = new ARWorldTrackingConfiguration();
			config.PlaneDetection = ARPlaneDetection.Horizontal;
            ARSession.Run(config, ARSessionRunOptions.RemoveExistingAnchors);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		unsafe protected void ApplyTransform(Node node, OpenTK.Matrix4 matrix)
		{
			Matrix4 urhoTransform = *(Matrix4*)(void*)&matrix;
			var rotation = urhoTransform.Rotation;
			rotation.Z *= -1;
			var pos = matrix.Row3;
			node.Position = new Vector3(pos.X, pos.Y, -pos.Z);
			node.Rotation = rotation;
		}

		public unsafe void ProcessARFrame(ARSession session, ARFrame frame)
		{
			var arcamera = frame?.Camera;
			var transform = arcamera.Transform;
			var prj = arcamera.ProjectionMatrix;

			//Urho accepts projection matrix in DirectX format (negative row3 + transpose)
			var urhoProjection = new Matrix4(
				 prj.M11,  prj.M21, -prj.M31,  prj.M41,
				 prj.M12,  prj.M22, -prj.M32,  prj.M42,
				 prj.M13,  prj.M23, -prj.M33,  prj.M43,
				 prj.M14,  prj.M24, -prj.M34,  prj.M44);
			
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
				cmd->SetShaderParameter("CameraScale", 1f);

				Viewport.RenderPath = rp;
				yuvTexturesInited = true;
			}

			// display tracking state (quality)
			DebugHud.AdditionalText = $"{arcamera.TrackingState}\n";
			if (arcamera.TrackingStateReason != ARTrackingStateReason.None)
				DebugHud.AdditionalText += arcamera.TrackingStateReason;

			// see "Render with Realistic Lighting"
			// https://developer.apple.com/documentation/arkit/displaying_an_ar_experience_with_metal
			var ambientIntensity = (float)frame.LightEstimate.AmbientIntensity / 1000f;
			Light.Brightness = 0.5f + ambientIntensity / 2;
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

                //Debug.WriteLine($"Camera texture - Y:{wY}x{hY}, UV:{wUv}x{hUv}, Ratio:{wUv/(float)hUv}\nScreen: {Graphics.Width}x{Graphics.Height} ({Graphics.Width/(float)Graphics.Height})");

                cameraYtexture.SetData(0, 0, 0, wY, hY, (void*)yPtr);
                cameraUVtexture.SetData(0, 0, 0, wUv, hUv, (void*)uvPtr);
            }
		}

		protected Vector3? HitTest(float screenX = 0.5f, float screenY = 0.5f)
		{
			var result = ARSession?.CurrentFrame?.HitTest(new CoreGraphics.CGPoint(screenX, screenY),
				ARHitTestResultType.ExistingPlaneUsingExtent)?.FirstOrDefault();
			if (result != null)
			{
				var row = result.WorldTransform.Row3;
				return new Vector3(row.X, row.Y, -row.Z);
			}
			return null;
		}
	}

	class UrhoARSessionDelegate : ARSessionDelegate
	{
		WeakReference<ArkitApp> arkitApp;

		public UrhoARSessionDelegate (ArkitApp arkitApp)
		{
			this.arkitApp = new WeakReference<ArkitApp>(arkitApp);
		}

		public override void CameraDidChangeTrackingState (ARSession session, ARCamera camera)
		{
			Console.WriteLine ("CameraDidChangeTrackingState");
		}

		public override void DidUpdateFrame (ARSession session, ARFrame frame)
		{
            if (arkitApp.TryGetTarget(out var ap))
                Urho.Application.InvokeOnMain(() => ap.ProcessARFrame(session, frame));
		}

		public override void DidFail (ARSession session, Foundation.NSError error)
		{
			Console.WriteLine ("DidFail");
		}

		public override void SessionWasInterrupted (ARSession session)
		{
			Console.WriteLine ("SessionWasInterrupted");
		}

		public override void SessionInterruptionEnded (ARSession session)
		{
			Console.WriteLine ("SessionInterruptionEnded");
		}

		public override void DidAddAnchors(ARSession session, ARAnchor[] anchors)
		{
			Console.WriteLine ("DidAddAnchors");
		}

		public override void DidRemoveAnchors(ARSession session, ARAnchor[] anchors)
		{
			Console.WriteLine ("DidRemoveAnchors");
		}

		public override void DidUpdateAnchors(ARSession session, ARAnchor[] anchors)
		{
			Console.WriteLine ("DidUpdateAnchors");
		}
	}
}