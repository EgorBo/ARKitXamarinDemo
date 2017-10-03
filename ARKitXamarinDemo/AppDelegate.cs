using Foundation;
using UIKit;
using Urho;
using System.Collections.Generic;
using CoreGraphics;
using System;
using Urho.iOS;

namespace ARKitXamarinDemo
{
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate, IUICollectionViewDataSource, IUICollectionViewDelegate
	{
		static NSString holoCellId = new NSString(nameof(HologramCell));
		UIWindow window;
		List<Hologram> availableHolograms;

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			var screenBounds = UIScreen.MainScreen.Bounds;
			window = new UIWindow(screenBounds);

			int iconSize = (int)(screenBounds.Height / 5f);
			var flowLayout = new UICollectionViewFlowLayout {
				HeaderReferenceSize = new CGSize(50, 0),
				ScrollDirection = UICollectionViewScrollDirection.Horizontal,
				ItemSize = new CGSize(iconSize - 10, iconSize - 10),
				MinimumLineSpacing = 12
			};

			availableHolograms = UrhoApp.GenerateHolograms();
			
			var collectionView = new UICollectionView (
				new CGRect (0, screenBounds.Height - iconSize - 5,
				           screenBounds.Width, iconSize), flowLayout);
			
			collectionView.RegisterClassForCell(typeof(HologramCell), holoCellId);
			collectionView.DataSource = this;
			collectionView.BackgroundColor = new UIColor(0, 0, 0, 0);
			collectionView.Delegate = new HologramCollectionDelegate(availableHolograms);

			var surface = new UrhoSurface(screenBounds);

			window.RootViewController = new UIViewController();
			window.RootViewController.View.AddSubview(surface);
			window.RootViewController.View.AddSubview(collectionView);
			window.MakeKeyAndVisible();

			surface.Show<UrhoApp>(new ApplicationOptions("UrhoData") { 
				DelayedStart = true
			});

			return true;
		}

		public UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var cell = (HologramCell)collectionView.DequeueReusableCell(holoCellId, indexPath);
			var holo = availableHolograms[indexPath.Row];
			cell.Image = UIImage.FromBundle(holo.Icon);
			return cell;
		}

		public nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			return availableHolograms.Count;
		}
	}
}

