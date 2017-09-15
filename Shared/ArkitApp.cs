// 
// ArkitApp: provides a Urho.Application subclass that blends Urho with
// ARKit.   
//

using System.Diagnostics;
using Urho;
using Urho.Physics;

namespace ARKitXamarinDemo
{
	/// <summary>
	/// ARKitApp blends UrhoSharp with ARKit by providing an application that has been
	/// configured with a basic scene, a camera, a light and can be fed frames from 
	/// ARKit's ARSession.
	/// </summary>
	public partial class ArkitApp : Urho.Application
	{
		[Preserve]
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
			Zone.AmbientColor = Color.White * 0.15f;
			Scene.CreateComponent<PhysicsWorld>();

			// Camera
			CameraNode = Scene.CreateChild(name: "Camera");
			Camera = CameraNode.CreateComponent<Camera>();

			// Light
			LightNode = Scene.CreateChild(name: "DirectionalLight");
			LightNode.SetDirection(new Vector3(0.75f, -1.0f, 0f));
			Light = LightNode.CreateComponent<Light>();
			Light.LightType = LightType.Directional;
			Light.CastShadows = true;
			Light.Brightness = 1.5f;
			Light.ShadowResolution = 4;
			Light.ShadowIntensity = 0.75f;
			Renderer.ShadowMapSize *= 4;

			// Viewport
			Viewport = new Viewport(Context, Scene, Camera, null);
			Viewport.SetClearColor(Color.Transparent);
			Renderer.SetViewport(0, Viewport);

			DebugHud = new MonoDebugHud(this);
			DebugHud.FpsOnly = true;
			DebugHud.Show(Color.Black, 40);

			AnchorsNode = Scene.CreateChild();
			FeaturePointsCloudeNode = Scene.CreateChild();
		}

		Node CreatePlaneNode(string id)
		{
			var node = AnchorsNode.CreateChild(id);
			var planeNode = node.CreateChild("SubPlane");
			var plane = planeNode.CreateComponent<StaticModel>();
			planeNode.Position = new Vector3();
			plane.Model = CoreAssets.Models.Plane;

			var tileMaterial = new Material();
			tileMaterial.SetTexture(TextureUnit.Diffuse, ResourceCache.GetTexture2D("Textures/PlaneTile.png"));
			var tech = new Technique();
			var pass = tech.CreatePass("alpha");
			pass.DepthWrite = false;
			pass.BlendMode = BlendMode.Alpha;
			pass.PixelShader = "PlaneTile";
			pass.VertexShader = "PlaneTile";
			tileMaterial.SetTechnique(0, tech);
			tileMaterial.SetShaderParameter("MeshColor", new Color(Randoms.Next(), 1, Randoms.Next()));
			tileMaterial.SetShaderParameter("MeshAlpha", 0.75f); // set 0.0f if you want to hide them
			tileMaterial.SetShaderParameter("MeshScale", 32.0f);

			var planeRb = planeNode.CreateComponent<RigidBody>();
			planeRb.Friction = 1.5f;
			CollisionShape shape = planeNode.CreateComponent<CollisionShape>();
			shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
			plane.Material = tileMaterial;

			return node;
		}

		protected override void Start()
		{
			UnhandledException += OnUnhandledException; 
			CreateArScene();
			StartSession();
		}

		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			System.Console.WriteLine(e.Exception);
			if (Debugger.IsAttached)
				Debugger.Break();
		}

		partial void StartSession();
	}
}