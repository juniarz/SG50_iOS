using System;
using MonoTouch.Foundation;

namespace SG50
{
    public class APIArgs
    {
        public static APIArgs Empty = new APIArgs();

        public NSMutableDictionary Headers = new NSMutableDictionary();
        public NSMutableDictionary Parameters = new NSMutableDictionary();
    }
}