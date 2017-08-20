using System;
using System.Collections.Generic;
using Urho;
using Urho.Actions;
using Urho.Gui;
using Urho.Resources;
using Urho.Shapes;

namespace ARKitXamarinDemo
{
	public class RulerDemo : ArkitApp
	{
		[Preserve]
		public RulerDemo(ApplicationOptions opts) : base(opts) { }

		List<Node> points = new List<Node>();
		Node textNode;

		Node prevNode;
		Node pointerNode;
		Vector3? cursorPos;

		protected override unsafe void Start()
		{
			UnhandledException += OnUnhandledException;

			base.Start();

			this.Log.LogLevel = LogLevel.Warning;

			pointerNode = Scene.CreateChild();
			pointerNode.SetScale(0.1f);
			var pointer = pointerNode.CreateComponent<Sphere>();
			pointer.Color = Color.Cyan;
			pointerNode.Name = "RulerPoint";

			textNode = pointerNode.CreateChild();
			textNode.SetScale(3);
			textNode.Translate(Vector3.UnitY * 2);
			textNode.AddRef();
			var text = textNode.CreateComponent<Text3D>();
			text.HorizontalAlignment = HorizontalAlignment.Center;
			text.VerticalAlignment = VerticalAlignment.Top;
			text.TextEffect = TextEffect.Stroke;
			text.EffectColor = Color.Black;
			text.SetColor(Color.White);
			text.SetFont(CoreAssets.Fonts.AnonymousPro, 50);

            ContinuesHitTestAtCenter = true;

			Input.TouchEnd += OnTouchEnd;
		}

        void OnTouchEnd(TouchEndEventArgs e)
		{
			if (cursorPos == null)
				return;

			var savedPoint = pointerNode;
			textNode.Parent.RemoveChild(textNode);

			points.Add(savedPoint);
			pointerNode = pointerNode.Clone();

			savedPoint.AddChild(textNode);

			if (points.Count > 1)
			{
				float distance = 0f;
				for (int i = 1; i < points.Count; i++)
					distance += Vector3.Distance(points[i - 1].Position, points[i].Position);
				textNode.GetComponent<Text3D>().Text = distance.ToString("F2") + "m";
			}

			if (prevNode != null)
				AddConnection(savedPoint, prevNode);
			prevNode = savedPoint;

			cursorPos = null;
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);

            //var ray = Camera.GetScreenRay(0.5f, 0.5f);
            //var raycastResult = Octree.RaycastSingle(ray);
            //if (raycastResult != null)
            if (LastHitTest != null)
			{
				/*var pos = raycastResult.Value.Position;
				if (!raycastResult.Value.Node.Name.StartsWith("RulerPoint"))
				{
					pointerNode.Position = pos;
					cursorPos = pos;
				}*/
				pointerNode.Position = LastHitTest.Value;
				cursorPos = LastHitTest.Value;
			}
			else
			{
				cursorPos = null;
			}

			textNode.LookAt(CameraNode.Position, Vector3.UnitY);
			textNode.Rotate(new Quaternion(0, 180, 0));
		}

		void OnUnhandledException(object sender, Urho.UnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			System.Console.WriteLine(e);
		}

		void AddConnection(Node point1, Node point2 = null)
		{
			const float size = 0.03f;
			var node = Scene.CreateChild();
			Vector3 v1 = point1.Position;
			Vector3 v2 = point2?.Position ?? Vector3.Zero;
			var distance = Vector3.Distance(v2, v1);
			node.Scale = new Vector3(size, Math.Abs(distance), size);
			node.Position = (v1 + v2) / 2f;
			node.Rotation = Quaternion.FromRotationTo(Vector3.UnitY, v1 - v2);
			var cylinder = node.CreateComponent<StaticModel>();
			cylinder.Model = CoreAssets.Models.Cylinder;
			cylinder.CastShadows = false;
			cylinder.SetMaterial(Material.FromColor(Color.White, true));
			node.RunActions(new TintTo(1f, 0f, 1f, 1f, 1f));
		}
	}
}
