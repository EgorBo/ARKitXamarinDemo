using System;

namespace ARKitXamarinDemo
{
	public class Hologram
	{
		Action<int> action;

		public Hologram(string icon, int index, Action<int> action)
		{
			this.Icon = icon;
			this.Index = index;
			this.action = action;
		}

		public string Icon { get; set; }

		public int Index { get; set; }

		public void Select()
		{
			action(Index);
		}
	}
}

