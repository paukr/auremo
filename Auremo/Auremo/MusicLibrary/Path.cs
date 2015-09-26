using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    public class Path : IComparable
    {
        public static readonly string MopidyLocalPrefix = "local:track:";
        public static readonly string MopidySpotifyPrefix = "spotify:track:";
        public static readonly string HttpPrefix = "http://";
        public static readonly string HttpsPrefix = "http://";

        public Path(string path)
        {
            Full = path;
        }

        public string Full
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Full;
        }
        
        public string[] Directories
        {
            get
            {
                if (IsStream())
                {
                    return new string[] { Full };
                }
                else
                {
                    string pathSegment = Full;

                    if (pathSegment.StartsWith(MopidySpotifyPrefix))
                    {
                        pathSegment.Remove(0, MopidySpotifyPrefix.Length);
                    }
                    else if (pathSegment.StartsWith(MopidyLocalPrefix))
                    {
                        pathSegment.Remove(0, MopidyLocalPrefix.Length);
                    }

                    return pathSegment.Split('/');
                }
            }
        }

        public bool IsLocal()
        {
            return IsLocal(Full);
        }

        public static bool IsLocal(string path)
        {
            string lowercase = path.ToLowerInvariant();
            return !lowercase.StartsWith(MopidySpotifyPrefix) && !lowercase.StartsWith(HttpPrefix) && !lowercase.StartsWith(HttpsPrefix);
        }

        public bool IsSpotify()
        {
            return IsSpotify(Full);
        }

        public static bool IsSpotify(string path)
        {
            return path.StartsWith("spotify:track:");
        }

        public bool IsStream()
        {
            return IsStream(Full);
        }

        public static bool IsStream(string path)
        {
            string lowercase = path.ToLowerInvariant();
            return lowercase.StartsWith("http://") || lowercase.StartsWith("https://");
        }

        public int CompareTo(object o)
        {
            if (o is Path)
            {
                return StringComparer.Ordinal.Compare(Full, (o as Path).Full);
            }
            else
            {
                throw new Exception("Path: attempt to compare to an incompatible object");
            }
        }
    }
}
