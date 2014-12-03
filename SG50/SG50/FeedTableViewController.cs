using System;
using System.Collections.Generic;
using System.Text;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json.Linq;
using RestSharp;
using MonoTouch.MediaPlayer;
using MonoTouch.MobileCoreServices;
using System.Drawing;

namespace SG50
{
	[Register ("FeedTableViewController")]
	class FeedTableViewController : UITableViewController
	{
		List<JToken> feeds = new List<JToken> ();
		int max_page = 0;
		bool IsLoading = false;

		public FeedTableViewController (IntPtr handle)
			: base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			LoadFeeds (max_page, delegate {
				InvokeOnMainThread (delegate {
					this.TableView.ReloadData ();
				});
			});

			UIRefreshControl refreshControl = new UIRefreshControl ();
			refreshControl.AddTarget (delegate(object sender, EventArgs e) {
				feeds.Clear ();
				max_page = 0;
				LoadFeeds (0, delegate {
					InvokeOnMainThread (delegate {
						this.TableView.ReloadData ();
					});
				});
				refreshControl.EndRefreshing ();
			}, UIControlEvent.ValueChanged);

			TableView.AddSubview (refreshControl);

			TableView.Scrolled += (object sender, EventArgs e) => {
				UIScrollView ScrollView = sender as UIScrollView;

				float Height = ScrollView.Frame.Size.Height;
				float ContentYOffset = ScrollView.ContentOffset.Y;
				float DistanceFromBottom = ScrollView.ContentSize.Height - ContentYOffset;
				if (ScrollView.ContentSize.Height > 0 && DistanceFromBottom + 20 < Height) {
					if (!IsLoading) {
						IsLoading = true;
						max_page += 1;
						Console.WriteLine ("Loading page: " + max_page);
						Console.WriteLine ("Now Feeds: " + feeds.Count);
						LoadFeeds (max_page, delegate {
							InvokeOnMainThread (delegate {
								IsLoading = false;
								Console.WriteLine ("Done loading page: " + max_page);
								Console.WriteLine ("New Feeds: " + feeds.Count);

								this.TableView.ReloadData();
							});
						});
					}
				}
			};

			this.SetToolbarItems (new UIBarButtonItem[] {
				new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem (UIImage.FromBundle ("sg50_logo").ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal), new UIBarButtonItemStyle (), delegate(object sender, EventArgs e) {
					if (!UIImagePickerController.IsSourceTypeAvailable (UIImagePickerControllerSourceType.Camera)) {
						return;
					}
						
					UIImagePickerController cameraUI = new UIImagePickerController ();
					cameraUI.SourceType = UIImagePickerControllerSourceType.Camera;
					cameraUI.MediaTypes = new string[] { UTType.Movie };
					cameraUI.AllowsEditing = true;
					cameraUI.FinishedPickingMedia += (object obj, UIImagePickerMediaPickedEventArgs info) => {
						String mediaType = info.MediaType;

						if (mediaType == UTType.Movie) {
							String moviePath = info.MediaUrl.AbsoluteString;
							Console.WriteLine("Movie Path: " + moviePath);
							if (UIVideo.IsCompatibleWithSavedPhotosAlbum (moviePath)) {
								UIVideo.SaveToPhotosAlbum (moviePath, delegate(string path, NSError error) {
									if (error != null) {
										UIAlertView alert = new UIAlertView ("Error", "Video saving failed.", null, "Ok", null);
										alert.Clicked += (object sender1, UIButtonEventArgs e1) => {
											InvokeOnMainThread(() => {
												DismissViewController (true, null);
												NavigationController.ToolbarHidden = false;
											});
										};
										alert.Show ();
									} else {
										UIAlertView alert = new UIAlertView ("Video Saved", "Saved to photo album.", null, "Ok", null);
										alert.Clicked += (object sender1, UIButtonEventArgs e1) => {
											InvokeOnMainThread(() => {
												DismissViewController (true, null);
												NavigationController.ToolbarHidden = false;
											});
										};
										alert.Show ();
									}
								});
							} else {
								UIAlertView alert = new UIAlertView ("Error", "The video is not compatible with Photos Album.", null, "Ok", null);
								alert.Clicked += (object sender1, UIButtonEventArgs e1) => {
									InvokeOnMainThread(() => {
										DismissViewController (true, null);
										NavigationController.ToolbarHidden = false;
									});
								};
								alert.Show ();
							}
						}

					};
					PresentViewController (cameraUI, true, null);
					NavigationController.ToolbarHidden = true;
				}) { Width = 120 },
				new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace)
			}, false);

			this.NavigationController.Toolbar.Translucent = true;
			this.NavigationController.ToolbarHidden = false;
		}

		public void LoadFeeds (int page, NSAction OnCompleted = null)
		{
			APITask task = new APITask ("feeds");
			APIArgs args = new APIArgs ();
			args.Parameters.Add (NSObject.FromObject ("page"), NSObject.FromObject (page));

			task.CallAsync (args, (response) => {
				var data = JArray.Parse (response.Content);

				foreach (JToken token in data) {
					feeds.Add (token);
				}

				if (OnCompleted != null) {
					OnCompleted.Invoke ();
				}
			}, (response) => {
				InvokeOnMainThread(() => {
					UIAlertView alert = new UIAlertView ("An error has occured...", response.ErrorMessage + " | " + response.Content, null, "Ok", null);
					alert.Show ();
				});
			}, Method.GET);
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
