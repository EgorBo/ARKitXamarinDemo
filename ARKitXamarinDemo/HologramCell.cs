using Foundation;
using UIKit;
using CoreGraphics;

namespace ARKitXamarinDemo
{
	public class HologramCell : UICollectionViewCell
	{
		UIImageView imageView;

		[Export("initWithFrame:")]
		public HologramCell(CGRect frame) : base(frame)
		{
			BackgroundView = new UIView();
			SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Green };

			imageView = new UIImageView(ContentView.Frame);
			imageView.BackgroundColor = UIColor.Gray;

			ContentView.AddSubview(imageView);
			imageView.Center = ContentView.Center;
			imageView.Transform = CGAffineTransform.MakeScale(0.9f, 0.9f);

			imageView.Layer.BorderWidth = 1.0f;
			imageView.Layer.BorderColor = new CGColor(0.1f, 0.1f, 0.1f);
			imageView.Layer.CornerRadius = ContentView.Frame.Height / 2;
			imageView.ClipsToBounds = true;

			SelectedBackgroundView.Layer.CornerRadius = SelectedBackgroundView.Frame.Height / 2;
		}

		public UIImage Image
		{
			set => imageView.Image = value;
		}
	}
}

