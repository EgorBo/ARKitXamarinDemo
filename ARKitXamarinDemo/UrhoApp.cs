using System;
using System.Collections.Generic;
using Urho;
using Urho.Actions;
using Urho.Resources;

namespace ARKitXamarinDemo
{
	public class UrhoApp : ArkitApp
	{
		[Preserve]
		public UrhoApp(ApplicationOptions opts) : base(opts) { }

		Node rootNode;
		bool scaling;

		protected override unsafe void Start()
		{
			UnhandledException += OnUnhandledException;
			Log.LogLevel = LogLevel.Debug;

			base.Start();

			Input.TouchBegin += OnTouchBegin;
			Input.TouchEnd += OnTouchEnd;
		}

		internal static List<Hologram> GenerateHolograms()
		{
			var result = new List<Hologram>();
			for (int i = 0; i < 12; i++) {
				string icon = "Icons/" + i;
				result.Add(new Hologram(i > 3 ? "Icons/todo" : icon, i, index => ((UrhoApp)Current).OnHologramSelected(index)));
			}

			return result;
		}

		void OnHologramSelected(int index)
		{
			rootNode?.Remove();
			rootNode = Scene.CreateChild();
			rootNode.SetScale(0.4f);
			var direction = CameraNode.Direction;
			direction.Normalize();
			rootNode.Position = new Vector3(direction.X, -0.5f, direction.Z) * 1.5f;

			if (index > 3)
				return;

			// Mutant
			if (index == 0)
			{
				var planeNode = rootNode.CreateChild();
				planeNode.Scale = new Vector3(10, 0.01f, 10);
				var plane = planeNode.CreateComponent<Urho.SharpReality.TransparentPlaneWithShadows>();

				var model = rootNode.CreateComponent<AnimatedModel>();
				model.CastShadows = true;
				model.Model = ResourceCache.GetModel("Models/Mutant.mdl");
				model.Material = ResourceCache.GetMaterial("Materials/mutant_M.xml");

				var animation = rootNode.CreateComponent<AnimationController>();
				animation.Play("Animations/Mutant_HipHop1.ani", 0, true, 0.2f);
			}
			// Mushroom
			else if (index == 1)
			{
				rootNode.SetScale(0.2f);
				var model = rootNode.CreateComponent<StaticModel>();
				model.Model = ResourceCache.GetModel("Models/Mushroom.mdl");
				model.Material = ResourceCache.GetMaterial("Materials/MushroomWind.xml");
			}
			// Earth
			else if (index == 2)
			{
				var model = rootNode.CreateComponent<StaticModel>();
				model.Model = CoreAssets.Models.Sphere;
				model.Material = Material.FromImage("Textures/Earth.jpg");
				rootNode.RunActionsAsync(new RepeatForever(new RotateBy(duration: 1f, deltaAngleX: 0, deltaAngleY: -15, deltaAngleZ: 0)));
			}
			// Moon
			else if (index == 3)
			{
				var model = rootNode.CreateComponent<StaticModel>();
				model.Model = CoreAssets.Models.Sphere;
				model.Material = Material.FromImage("Textures/Moon.jpg");
				rootNode.RunActionsAsync(new RepeatForever(new RotateBy(duration: 1f, deltaAngleX: 0, deltaAngleY: -15, deltaAngleZ: 0)));
			}
		}

		void OnTouchBegin(TouchBeginEventArgs e)
		{
			scaling = false;
		}

		void OnTouchEnd(TouchEndEventArgs e)
		{
			if (scaling)
				return;

			var pos = HitTest(e.X / (float)Graphics.Width, e.Y / (float)Graphics.Height);
			if (pos != null)
				rootNode.Position = pos.Value;
		}

		protected override void OnUpdate(float timeStep)
		{
			// Scale up\down
			if (Input.NumTouches == 2)
			{
				scaling = true;
				var state1 = Input.GetTouch(0);
				var state2 = Input.GetTouch(1);
				var distance1 = IntVector2.Distance(state1.Position, state2.Position);
				var distance2 = IntVector2.Distance(state1.LastPosition, state2.LastPosition);
				rootNode.SetScale(rootNode.Scale.X + (distance1 - distance2) / 5000f);
			}

			base.OnUpdate(timeStep);
		}

		void OnUnhandledException(object sender, Urho.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			System.Console.WriteLine(e);
		}
	}
}