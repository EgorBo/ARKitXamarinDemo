using Foundation;
using System.Threading.Tasks;
using UIKit;
using ARKit;
using System;
using Urho;
using AVFoundation;

namespace ARKitXamarinDemo
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		public override UIWindow Window { get; set; }

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			LaunchUrho();
			return true;
		}

		// 
		// Launching urho starts the task once control has returned to the OS at startup
		// hence the await here.
		//
		async void LaunchUrho()
		{
			await Task.Yield();

			var mutantDemo = new MutantDemo(new ApplicationOptions() { 
				ResourcePaths = new string[] { "UrhoData" },
				Orientation = ApplicationOptions.OrientationType.Landscape
			});
			mutantDemo.Run();
		}
	}
}

