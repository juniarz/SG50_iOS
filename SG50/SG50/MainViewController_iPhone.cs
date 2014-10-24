using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Newtonsoft.Json.Linq;

namespace SG50
{
    public partial class MainViewController_iPhone : UIViewController
    {

        public MainViewController_iPhone(IntPtr handle)
            : base(handle)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }
    }
}