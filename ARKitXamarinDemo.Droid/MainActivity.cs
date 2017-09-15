using Android.App;
using Android.Widget;
using Android.OS;

namespace ARKitXamarinDemo.Droid
{
    [Activity(Label = "ARKitXamarinDemo.Droid", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Com.Google.AR.Core.Session session = null;

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
        }
    }
}

