using System;
using System.Runtime.InteropServices;
using CoreVideo;
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

			CreateArScene();

			// Mutant
			mutantNode = Scene.CreateChild();
			mutantNode.Position = new Vector3(0, -1f, 2f);
			mutantNode.SetScale(0.5f);
			var mutant = mutantNode.CreateComponent<AnimatedModel>();
			mutant.Model = ResourceCache.GetModel("Models/Mutant.mdl");
			mutant.SetMaterial(ResourceCache.GetMaterial("Materials/mutant_M.xml"));
			var animation = mutantNode.CreateComponent<AnimationController>();
			animation.Play("Animations/Mutant_HipHop1.ani", 0, true, 0.2f);

			Input.TouchEnd += OnTouched;
		}

		void OnTouched(TouchEndEventArgs e)
		{
		}

		void OnUnhandledException(object sender, Urho.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			System.Console.WriteLine(e);
		}
	}
}
