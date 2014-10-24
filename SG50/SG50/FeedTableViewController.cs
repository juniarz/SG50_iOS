using System;
using System.Collections.Generic;
using System.Text;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json.Linq;
using RestSharp;
using MonoTouch.MediaPlayer;

namespace SG50
{
	[Register ("FeedTableViewController")]
	class FeedTableViewController : UITableViewController
	{
		List<JToken> feeds = new List<JToken> ();

		public FeedTableViewController (IntPtr handle)
			: base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			LoadFeeds ();
		}

		public async void LoadFeeds ()
		{
			APITask task = new APITask ("feed");
			APIArgs args = new APIArgs ();
			args.Parameters.Add (NSObject.FromObject ("accesstoken"), NSObject.FromObject ("acdcb58208f767fc204f36ecd74afc30"));
			args.Parameters.Add (NSObject.FromObject ("type"), NSObject.FromObject ("test"));

			try {
				IRestResponse response = await task.CallAsync (args);

				if (response.ErrorException == null) {
					var data = JObject.Parse (response.Content);

					foreach (JToken token in data.Values()) {
						feeds.Add (token);
					}

					this.TableView.ReloadData ();
				} else {
					Console.WriteLine(response.ErrorException.Message);
				}
			} catch (Exception ex) {
				UIAlertView alert = new UIAlertView ("An error has occured...", ex.Message, null, "Ok", null);
				alert.Show ();
			}
		}

		public override int NumberOfSections (UITableView tableView)
		{
			return 1;
		}

		public override int RowsInSection (UITableView tableView, int section)
		{
			return feeds.Count;
		}

		public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			FeedViewCell cell = tableView.DequeueReusableCell ("FeedViewCell") as FeedViewCell;

			if (cell == null) {
				cell = FeedViewCell.Create (feeds [indexPath.Row]);
			}

			return cell;
		}
	}
}
