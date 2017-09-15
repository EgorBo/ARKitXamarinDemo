using Urho;

namespace ARKitXamarinDemo.Desktop
{
	class Program
	{

		static void Run<T>() where T : ArkitApp
		{
			Urho.Application.CreateInstance<T>(
				new ApplicationOptions {
				ResourcePaths = new[] { "../../../UrhoAssets" },
				TouchEmulation = true,
				HighDpi = false,
				Orientation = ApplicationOptions.OrientationType.Landscape
			}).Run();
		}

		static void Main(string[] args)
		{
			Run<MutantDemo>();
			//Run<CrowdDemo>();
		}
	}
}
