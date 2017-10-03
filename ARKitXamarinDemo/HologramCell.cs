using Foundation;
using UIKit;
using CoreGraphics;

namespace ARKitXamarinDemo
{
	public class HologramCell : UICollectionViewCell
	{
		UIImageView imageView;
		UILabel label;

		[Export("initWithFrame:")]
		public HologramCell(CGRect frame) : base(frame)
		{
			BackgroundView = new UIView();
			SelectedBackgroundView = new UIView { BackgroundColor = UIColor.White };

			imageView = new UIImageView(ContentView.Frame);
			imageView.BackgroundColor = UIColor.Gray;

			ContentView.AddSubview(imageView);
			imageView.Center = ContentView.Center;
			imageView.Transform = CGAffineTransform.MakeScale(0.9f, 0.9f);

			imageView.Layer.BorderWidth = 4.0f;
			imageView.Layer.BorderColor = new CGColor(0.1f, 0.1f, 0.1f);
			imageView.Layer.CornerRadius = ContentView.Frame.Height / 2;
			imageView.ClipsToBounds = true;

			SelectedBackgroundView.Layer.CornerRadius = SelectedBackgroundView.Frame.Height / 2;

			label = new UILabel(new CGRect(0, frame.Height - 8, frame.Width, 30));
			label.Text = "Mutant";
			label.TextAlignment = UITextAlignment.Center;
			label.Font = UIFont.SystemFontOfSize(10, UIFontWeight.Thin);
			label.TextColor = UIColor.White;
			ContentView.AddSubview(label);
		}

		public UIImage Image { set => imageView.Image = value; }

		public string Text { set => label.Text = value; }
	}
}

