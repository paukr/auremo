using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo.MusicLibrary
{
    public class Path : IComparable
    {
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

        public bool IsStream
        {
            get
            {
                string lowercase = Full.ToLowerInvariant();
                return lowercase.StartsWith("http://") || lowercase.StartsWith("https://");
            }
        }

        public bool CanBeLocal
        {
            get
            {
                string lowercase = Full.ToLowerInvariant();
                return !lowercase.StartsWith("spotify:track:") && !lowercase.StartsWith("http://") && !lowercase.StartsWith("https://");
            }
        }

        public string[] Directories
        {
            get
            {
                if (IsStream)
                {
                    return new string[] { Full };
                }
                else
                {
                    string pathSegment = Full;

                    if (pathSegment.StartsWith("spotify:track:"))
                    {
                        pathSegment.Remove(0, 14);
                    }
                    else if (pathSegment.StartsWith("local:track:"))
                    {
                        pathSegment.Remove(0, 12);
                    }

                    return pathSegment.Split('/');
                }
            }
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
