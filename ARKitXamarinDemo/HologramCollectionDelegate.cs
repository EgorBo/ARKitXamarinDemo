using Foundation;
using UIKit;
using System.Linq;

using System.Collections.Generic;

namespace ARKitXamarinDemo
{
	public class HologramCollectionDelegate : UICollectionViewDelegate
	{
		List<Hologram> holograms;

		public HologramCollectionDelegate(List<Hologram> holograms)
		{
			this.holograms = holograms;
		}

		public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			holograms[(int)indexPath.Item].Select();
		}
	}
}

