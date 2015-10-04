using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class CoverArtFetchTask
    {
        public enum RequestType { Fetch, Delete };

        public CoverArtFetchTask(RequestType request, string artist, string album)
        {
            Request = request;
            Artist = artist;
            Album = album;
        }

        public RequestType Request
        {
            get;
            private set;
        }

        public string Artist
        {
            get;
            private set;
        }

        public string Album
        {
            get;
            private set;
        }
    }
}
