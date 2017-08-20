using System.Linq;
using Urho;
using Urho.Actions;
using Urho.Navigation;

namespace ARKitXamarinDemo
{
    public class CrowdDemo : ArkitApp
	{
		Node armyNode;
		CrowdManager crowdManager;
		bool debug = true;
		bool surfaceIsValid;
		bool positionIsSelected;
		Node cursorNode;
		StaticModel cursorModel;
		Material lastMutantMat;

		const string WalkingAnimation = @"Animations/Mutant_Run.ani";
		const string IdleAnimation = @"Animations/Mutant_Idle0.ani";
		const string DeathAnimation = @"Animations/Mutant_Death.ani";
		const string MutantModel = @"Models/Mutant.mdl";
		const string MutantMaterial = @"Materials/mutant_M.xml";

        [Preserve]
		public CrowdDemo(ApplicationOptions opts) : base(opts) { }

		protected override async void Start()
		{
			base.Start();

            cursorNode = Scene.CreateChild();
            cursorNode.Position = Vector3.UnitZ * 100; //hide cursor at start - pos at (0,0,100) 
            cursorModel = cursorNode.CreateComponent<Urho.Shapes.Plane>();
            cursorModel.ViewMask = 0x80000000; //hide from raycasts (Raycast() uses a differen viewmask so the cursor won't be visible for it)
			cursorNode.RunActions(new RepeatForever(new ScaleTo(0.3f, 0.15f), new ScaleTo(0.3f, 0.2f)));

            var cursorMaterial = new Material();
			cursorMaterial.SetTexture(TextureUnit.Diffuse, ResourceCache.GetTexture2D("Textures/Cursor.png"));
			cursorMaterial.SetTechnique(0, CoreAssets.Techniques.DiffAlpha);
			cursorModel.Material = cursorMaterial;

			Input.TouchEnd += args => OnGestureTapped(args.X, args.Y);
            UnhandledException += OnUnhandledException;
		}

		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			System.Console.WriteLine(e);
		}

		void SubscribeToEvents()
		{
			crowdManager.CrowdAgentReposition += args => {
				Node node = args.Node;
				Vector3 velocity = args.Velocity * -1;
				var animCtrl = node.GetComponent<AnimationController>();
				if (animCtrl != null)
				{
					float speed = velocity.Length;
					if (animCtrl.IsPlaying(WalkingAnimation))
					{
						float speedRatio = speed / args.CrowdAgent.MaxSpeed;
						// Face the direction of its velocity but moderate the turning speed based on the speed ratio as we do not have timeStep here
						node.SetRotationSilent(Quaternion.FromRotationTo(Vector3.UnitZ, velocity));
						// Throttle the animation speed based on agent speed ratio (ratio = 1 is full throttle)
						animCtrl.SetSpeed(WalkingAnimation, speedRatio);
					}
					else
						animCtrl.Play(WalkingAnimation, 0, true, 0.1f);

					// If speed is too low then stopping the animation
					if (speed < args.CrowdAgent.Radius)
					{
						animCtrl.Stop(WalkingAnimation, 0.8f);
						animCtrl.Play(IdleAnimation, 0, true, 0.2f);
					}
				}
			};
		}

		async void KillAll()
		{
			foreach (var node in armyNode.Children.ToArray())
			{
				var anim = node.GetComponent<AnimationController>();
				var agent = node.GetComponent<CrowdAgent>();
				agent?.Remove();
				anim.Play(DeathAnimation, 0, false, 0.4f);
				await Delay(Randoms.Next(0f, 0.2f));
			}
		}

		Vector3? Raycast(float x, float y)
		{
			Ray cameraRay = Camera.GetScreenRay(x, y);
			var result = Scene.GetComponent<Octree>().RaycastSingle(cameraRay, 
                RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
			if (result != null)
			{
				return result.Value.Position;
			}
			return null;
		}

        void HighlightMaterial(Material material, bool higlight)
		{
            material.SetShaderParameter("OutlineColor", higlight ? new Color(1f, 0.75f, 0, 0.5f) : Color.Transparent);
            material.SetShaderParameter("OutlineWidth", higlight ? 0.009f : 0f);
		}

        void SpawnMutant(Vector3 pos, string name = "Mutant")
        {
            Node mutantNode = armyNode.CreateChild(name);
            mutantNode.Position = pos;
            mutantNode.SetScale(0.2f);
            var modelObject = mutantNode.CreateComponent<AnimatedModel>();

            modelObject.CastShadows = true;
            modelObject.Model = ResourceCache.GetModel(MutantModel);
            modelObject.SetMaterial(ResourceCache.GetMaterial(MutantMaterial).Clone());
            modelObject.Material.SetTechnique(0, ResourceCache.GetTechnique("Techniques/DiffOutline.xml"));
            HighlightMaterial(modelObject.Material, false);

            mutantNode.CreateComponent<AnimationController>().Play(IdleAnimation, 0, true, 0.2f);

            // Create the CrowdAgent
            var agent = mutantNode.CreateComponent<CrowdAgent>();
            agent.Height = 0.2f;
            agent.NavigationPushiness = NavigationPushiness.Medium;
            agent.MaxSpeed = 0.4f;
            agent.MaxAccel = 0.4f;
            agent.Radius = 0.05f;
            agent.NavigationQuality = NavigationQuality.Medium;
        }

		protected override void OnUpdate(float timeStep)
		{
			if (lastMutantMat != null)
			{
                HighlightMaterial(lastMutantMat, false);
				lastMutantMat = null;
			}

			base.OnUpdate(timeStep);
			if (positionIsSelected)
			{
				Ray cameraRay = Camera.GetScreenRay(0.5f, 0.5f);
				var result = Octree.RaycastSingle(cameraRay);
				if (result?.Node?.Name?.StartsWith("Mutant") == true)
				{
					var mat = ((StaticModel)result.Value.Drawable).Material;
                    HighlightMaterial(mat, true);
					lastMutantMat = mat;
				}

				return;
			}

			var point = HitTest();
			if (point != null)
			{
				surfaceIsValid = true;
				cursorNode.Position = point.Value;
			}
			cursorModel.Material.SetShaderParameter(CoreAssets.ShaderParameters.MatDiffColor, surfaceIsValid ? Color.White : Color.Red);
		}

		void OnGestureTapped(int argsX, int argsY)
		{
            // 3 touches at the same time kill everybody :-)
			if (Input.NumTouches == 3)
				KillAll();

			NavigationMesh navMesh;

			if (surfaceIsValid && !positionIsSelected)
			{
                var hitPos = cursorNode.Position - Vector3.UnitZ * 0.01f;
				positionIsSelected = true;

				navMesh = Scene.CreateComponent<NavigationMesh>();

				//this plane is a workaround 
				//TODO: build a navmesh using spatial data

				var planeNode = Scene.CreateChild();

				var plane = planeNode.CreateComponent<StaticModel>();
				plane.Model = CoreAssets.Models.Plane;
				plane.SetMaterial(Material.FromColor(Color.Transparent));
				planeNode.Scale = new Vector3(100, 1, 100);
                planeNode.Position = hitPos;

				Scene.CreateComponent<Navigable>();

				navMesh.CellSize = 0.2f;
				navMesh.CellHeight = 0.042f;
				navMesh.DrawOffMeshConnections = true;
				navMesh.DrawNavAreas = true;
				navMesh.TileSize = 2;
				navMesh.AgentRadius = 0.1f;

				navMesh.Build();

				crowdManager = Scene.CreateComponent<CrowdManager>();
				var parameters = crowdManager.GetObstacleAvoidanceParams(0);
				parameters.VelBias = 0.5f;
				parameters.AdaptiveDivs = 7;
				parameters.AdaptiveRings = 3;
				parameters.AdaptiveDepth = 3;
				crowdManager.SetObstacleAvoidanceParams(0, parameters);
				armyNode = Scene.CreateChild();

				SubscribeToEvents();

				int mutantIndex = 1;
				for (int i = 0; i < 3; i++)
					for (int j = 0; j < 3; j++)
						SpawnMutant(new Vector3(hitPos.X + 0.15f * i, hitPos.Y, hitPos.Z + 0.13f * j), "Mutant " + mutantIndex++);

				return;
			}

			if (positionIsSelected)
			{
				var hitPos = Raycast((float)argsX / Graphics.Width, (float)argsY / Graphics.Height);
				if (hitPos == null)
					return;

                cursorNode.Position = hitPos.Value + Vector3.UnitY * 0.1f;
                
				navMesh = Scene.GetComponent<NavigationMesh>();
				Vector3 pathPos = navMesh.FindNearestPoint(hitPos.Value, new Vector3(0.1f, 0.1f, 0.1f));
				Scene.GetComponent<CrowdManager>().SetCrowdTarget(pathPos, Scene);
			}
		}
	}
}