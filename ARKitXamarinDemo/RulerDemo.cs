using System;
using System.Collections.Generic;
using Urho;
using Urho.Actions;
using Urho.Gui;
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
        Cursor realCursor;

		protected override unsafe void Start()
		{
			UnhandledException += OnUnhandledException;

			base.Start();

			Log.LogLevel = LogLevel.Warning;
            realCursor = Scene.CreateComponent<Cursor>();
			Input.TouchEnd += OnTouchEnd;
		}

        void OnTouchEnd(TouchEndEventArgs e)
		{
            if (realCursor != null)
            {
                if (realCursor.Position != null)
                {
                    var pos = realCursor.Position.Value;
                    realCursor.Remove();
                    realCursor = null;

                    var fakePlaneNode = Scene.CreateChild();
                    fakePlaneNode.Position = pos;
                    fakePlaneNode.Scale = new Vector3(100, 1, 100);
                    var fakePlane = fakePlaneNode.CreateComponent<Urho.Shapes.Plane>();
                    fakePlane.Color = Color.Transparent;

					pointerNode = Scene.CreateChild();
					pointerNode.SetScale(0.03f);
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
                }
                return;
            }

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

            if (realCursor != null)
                return;

            var ray = Camera.GetScreenRay(0.5f, 0.5f);
            var raycastResult = Octree.RaycastSingle(ray);
            if (raycastResult != null)
			{
				var pos = raycastResult.Value.Position;
				if (!raycastResult.Value.Node.Name.StartsWith("RulerPoint"))
				{
					pointerNode.Position = pos;
					cursorPos = pos;
				}
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
			const float size = 0.02f;
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

	public class Cursor : Component
	{
		public Node CursorNode { get; private set; }
		public StaticModel CursorModel { get; private set; }
		public Vector3? Position => app.LastHitTest;

		ArkitApp app;
		bool continuesHitTest;

		[Preserve]
		public Cursor()
		{
			ReceiveSceneUpdates = true;
		}

		public override void OnAttachedToNode(Node node)
		{
			CursorNode = Node.CreateChild();
			CursorNode.Position = Vector3.UnitZ * 100; //hide cursor at start - pos at (0,0,100) 
			CursorModel = CursorNode.CreateComponent<Urho.Shapes.Plane>();
			CursorModel.ViewMask = 0x80000000; //hide from raycasts (Raycast() uses a differen viewmask so the cursor won't be visible for it)
			CursorNode.RunActions(new RepeatForever(new ScaleTo(0.3f, 0.15f), new ScaleTo(0.3f, 0.2f)));

			var cursorMaterial = new Material();
			cursorMaterial.SetTexture(TextureUnit.Diffuse, Application.ResourceCache.GetTexture2D("Textures/Cursor.png"));
			cursorMaterial.SetTechnique(0, CoreAssets.Techniques.DiffAlpha);
			CursorModel.Material = cursorMaterial;

			app = (ArkitApp)Application;
			continuesHitTest = app.ContinuesHitTestAtCenter;
			app.ContinuesHitTestAtCenter = true;
		}

		protected override void OnDeleted()
		{
			CursorModel?.Remove();
			app.ContinuesHitTestAtCenter = continuesHitTest;
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			if (app?.LastHitTest != null)
			{
				CursorNode.Position = app.LastHitTest.Value;
			}
		}
	}

}
