using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using RestSharp;

namespace SG50
{
    class APITask
    {
        public String API = "";
        public static String API_TEMPLATE = "http://sg50-private-api.herokuapp.com/api/";

        public APITask(String API)
        {
            this.API = API;
        }

        public class APIEventHandlerArgs : EventArgs
        {
            public String ErrorMessage = "";
            public String Result = "";
        }

        public delegate void APIEventHandler(object sender, APIEventHandlerArgs e);
        public event APIEventHandler OnErrorEventHandler;
        public event APIEventHandler OnSuccessEventHandler;

        protected void OnError(object sender, APIEventHandlerArgs e)
        {
            if (OnErrorEventHandler != null)
            {
                // Call the Event
                OnErrorEventHandler(this, e);
            }
        }

        protected void OnSuccess(object sender, APIEventHandlerArgs e)
        {
            if (OnSuccessEventHandler != null)
            {
                // Call the Event
                OnSuccessEventHandler(this, e);
            }
        }

        public void Call(APIArgs args)
        {
            var client = new RestClient(API_TEMPLATE);

            var request = GetRequest(args);

            client.ExecuteAsync(request, response =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    OnError(this, new APIEventHandlerArgs { ErrorMessage = response.Content });
                }
                else
                {
                    OnSuccess(this, new APIEventHandlerArgs { Result = response.Content });
                }
            });
        }

        public Task<IRestResponse> CallAsync(APIArgs args)
        {
            var client = new RestClient(API_TEMPLATE);

            var request = GetRequest(args);

            var tcs = new TaskCompletionSource<IRestResponse>();
            client.ExecuteAsync(request, response =>
            {
                if (response.ErrorException != null)
                    tcs.TrySetException(response.ErrorException);
                else
                    tcs.TrySetResult(response);
            });

            return tcs.Task;
        }

        private RestRequest GetRequest(APIArgs args)
        {
            var request = new RestRequest(String.Format("{0}/?{1}", API, Guid.NewGuid().ToString().Replace("-", "")), Method.POST);

            foreach (NSString key in args.Headers.Keys)
            {
                request.AddHeader(key, args.Headers.ValueForKey(key).ToString());
            }

            foreach (NSString key in args.Parameters.Keys)
            {
                request.AddParameter(key, args.Parameters.ValueForKey(key));
            }

            return request;
        }
    }
}