using System;
using Urho;
using Urho.Actions;
using Urho.Gui;
using Urho.Resources;
using Urho.Shapes;
using Urho.Urho2D;

namespace ARKitXamarinDemo
{
	public class MutantDemo : ArkitApp
	{
		[Preserve]
		public MutantDemo(ApplicationOptions opts) : base(opts) { }

		Node mutantNode;

		protected override unsafe void Start()
		{
			UnhandledException += OnUnhandledException;
			Log.LogLevel = LogLevel.Debug;

			base.Start ();

			// Mutant
			mutantNode = Scene.CreateChild();
			mutantNode.Position = new Vector3(0, -1f, 1f);
			mutantNode.SetScale(0.5f);
			var mutant = mutantNode.CreateComponent<AnimatedModel>();
			mutant.Model = ResourceCache.GetModel("Models/Mutant.mdl");
			mutant.SetMaterial(ResourceCache.GetMaterial("Materials/mutant_M.xml"));
			var animation = mutantNode.CreateComponent<AnimationController>();
			animation.Play("Animations/Mutant_HipHop1.ani", 0, true, 0.2f);

			Input.TouchMove += OnTouchMove;
		}


		void OnTouchMove(TouchMoveEventArgs e)
		{
			float speed = 0.001f;
			mutantNode.Translate(new Vector3(0/*e.DX * speed*/, -e.DY * speed, 0));
		}

		void OnUnhandledException(object sender, Urho.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			System.Console.WriteLine(e);
		}
	}
}
