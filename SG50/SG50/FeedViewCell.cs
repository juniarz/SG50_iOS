
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json.Linq;
using MonoTouch.MediaPlayer;
using RestSharp;

namespace SG50
{
	public partial class FeedViewCell : UITableViewCell
	{
		public static readonly UINib Nib = UINib.FromName ("FeedViewCell", NSBundle.MainBundle);
		public static readonly NSString Key = new NSString ("FeedViewCell");
		MPMoviePlayerController moviePlayer;
		UILabel titleLbl;
		UILabel userLbl;
		UILabel likesLbl;
		UIImageView coverImageView;
		UIButton playBtn;
		UIButton likeBtn;
		UIButton flagBtn;
		JToken data;

		public FeedViewCell (IntPtr handle) : base (handle)
		{
		}

		public static FeedViewCell Create (JToken data)
		{
			FeedViewCell ret = (FeedViewCell)Nib.Instantiate (null, null) [0];

			//Init
			ret.titleLbl = ret.ViewWithTag (1) as UILabel;
			ret.userLbl = ret.ViewWithTag (2) as UILabel;
			ret.coverImageView = ret.ViewWithTag (3) as UIImageView;
			ret.playBtn = ret.ViewWithTag (4) as UIButton;
			ret.likeBtn = ret.ViewWithTag (5) as UIButton;
			ret.likesLbl = ret.ViewWithTag (6) as UILabel;
			ret.flagBtn = ret.ViewWithTag (7) as UIButton;
			ret.moviePlayer = new MPMoviePlayerController ();
			ret.moviePlayer.View.Frame = ret.coverImageView.Frame;
			ret.moviePlayer.View.Bounds = ret.coverImageView.Bounds;
			ret.moviePlayer.View.Hidden = true;
			ret.AddSubview (ret.moviePlayer.View);

			NSUrlRequest req = NSUrlRequest.FromUrl (new NSUrl (data ["video"] ["cover"].ToString ()));
			NSUrlConnection.SendAsynchronousRequest(req, NSOperationQueue.MainQueue, delegate(NSUrlResponse response, NSData _data, NSError error) {
				if (error != null) {
					return;
				}

				ret.InvokeOnMainThread(() => {
					ret.coverImageView.Image = new UIImage (_data);
				});
			});

			ret.playBtn.AddTarget (ret, new MonoTouch.ObjCRuntime.Selector ("PlayButton_Clicked"), UIControlEvent.TouchUpInside);
			ret.likeBtn.AddTarget (ret, new MonoTouch.ObjCRuntime.Selector ("LikeButton_Clicked"), UIControlEvent.TouchUpInside);
			ret.flagBtn.AddTarget (ret, new MonoTouch.ObjCRuntime.Selector ("FlagButton_Clicked"), UIControlEvent.TouchUpInside);

			ret.Update (data);

			return ret;
		}

		public void Update(JToken data) {
			this.data = data;

			titleLbl.Text = data ["title"].ToString ();
			userLbl.Text = "Uploaded by: " + data ["user"].ToString ();
			likesLbl.Text = data ["likes"].ToString () + " Likes";

			moviePlayer.ContentUrl = new NSUrl (data ["video"] ["url"].ToString ());

			bool liked = Convert.ToBoolean (data ["liked"].ToString ());

			if (liked) {
				likeBtn.TitleLabel.TextColor = UIColor.FromRGB ((float) 255, (float) 153, (float) 204);
			} else {
				likeBtn.TitleLabel.TextColor = UIColor.FromRGB ((float) 224, (float) 224, (float) 224);
			}
		}

		[Export ("PlayButton_Clicked")]
		void PlayButton_Clicked ()
		{
			PlayVideo ();
		}

		[Export ("LikeButton_Clicked")]
		void LikeButton_Clicked ()
		{
			if (Convert.ToBoolean (data ["liked"].ToString ())) {
				Unlike ();
			} else {
				Like ();
			}
		}

		[Export ("FlagButton_Clicked")]
		void FlagButton_Clicked ()
		{
			Flag ();
		}

		void PlayVideo() {
			coverImageView.Hidden = true;
			playBtn.Hidden = true;
			moviePlayer.View.Hidden = false;
			moviePlayer.Play();

			NSNotificationCenter.DefaultCenter.AddObserver ("MPMoviePlayerPlaybackDidFinishNotification", delegate(NSNotification obj) {
				InvokeOnMainThread(() => {
					moviePlayer.Fullscreen = false;
					moviePlayer.View.Hidden = true;
					playBtn.Hidden = false;
					coverImageView.Hidden = false;
				});
				obj.Dispose();
			});
		}

		void UpdateLikesAndLiked(bool isLike) {
			InvokeOnMainThread (() => {
				data ["likes"] = Convert.ToInt32 (data ["likes"].ToString ()) - (isLike ? -1 : 1);
				data ["liked"] = isLike;
				Update (data);
			});
		}

		async void Like() {

			UpdateLikesAndLiked (true);

			APITask task = new APITask ("feed/" + data["present_id"] + "/like");
			APIArgs args = new APIArgs ();
			args.Parameters.Add (NSObject.FromObject ("accesstoken"), NSObject.FromObject ("acdcb58208f767fc204f36ecd74afc30"));

			try {
				IRestResponse response = await task.CallAsync (args);
				if (response.StatusCode != System.Net.HttpStatusCode.NoContent) {
					UIAlertView alert = new UIAlertView ("Oops!", "Failed to like, please try again!", null, "Ok", null);
					alert.Show ();
					UpdateLikesAndLiked (false);
				}
			} catch (Exception ex) {
				UIAlertView alert = new UIAlertView ("An error has occured...", ex.Message, null, "Ok", null);
				alert.Show ();
				UpdateLikesAndLiked (false);
			}
		}

		async void Unlike() {

			UpdateLikesAndLiked (false);

			APITask task = new APITask ("feed/" + data["present_id"] + "/unlike");
			APIArgs args = new APIArgs ();
			args.Parameters.Add (NSObject.FromObject ("accesstoken"), NSObject.FromObject ("acdcb58208f767fc204f36ecd74afc30"));

			try {
				IRestResponse response = await task.CallAsync (args);
				if (response.StatusCode != System.Net.HttpStatusCode.NoContent) {
					UIAlertView alert = new UIAlertView ("Oops!", "Failed to unlike, please try again!", null, "Ok", null);
					alert.Show ();
					UpdateLikesAndLiked (true);
				}
			} catch (Exception ex) {
				UIAlertView alert = new UIAlertView ("An error has occured...", ex.Message, null, "Ok", null);
				alert.Show ();
				UpdateLikesAndLiked (true);
			}
		}

		void Flag() {

			UIAlertView prompt = new UIAlertView ("Are you sure?", "Flag as inappropriate?", null, "No", new String[] { "Yes" });
			prompt.Clicked += async (object sender, UIButtonEventArgs e) => {

				if (e.ButtonIndex == 1) {
					APITask task = new APITask ("feed/" + data["present_id"] + "/flag");
					APIArgs args = new APIArgs ();
					args.Parameters.Add (NSObject.FromObject ("accesstoken"), NSObject.FromObject ("acdcb58208f767fc204f36ecd74afc30"));

					try {
						IRestResponse response = await task.CallAsync (args);
						if (response.StatusCode != System.Net.HttpStatusCode.NoContent) {
							UIAlertView alert = new UIAlertView ("Oops!", "Failed to flag, please try again!", null, "Ok", null);
							alert.Show ();
						}
					} catch (Exception ex) {
						UIAlertView alert = new UIAlertView ("An error has occured...", ex.Message, null, "Ok", null);
						alert.Show ();
						UpdateLikesAndLiked (true);
					}
				}

			};
			prompt.Show ();
		}
	}
}