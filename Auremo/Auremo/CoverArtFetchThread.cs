/*
 * Copyright 2014 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Auremo
{
    public class CoverArtFetchThread
    {
        private CoverArtRepository m_Parent = null;
        private string m_BaseDirectory = null;

        public CoverArtFetchThread(CoverArtRepository owner)
        {
            SetUpBaseDirectory();
            m_Parent = owner;
        }

        public void Start()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Uncaught exception in Auremo.CoverArtFetchThread.\n" +
                                               "Please take a screenshot of this message and send it to the developer.\n\n" +
                                               e.ToString(),
                                               "Auremo has crashed!");
                throw;
            }
        }

        private void SetUpBaseDirectory()
        {
            string root = m_BaseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string auremo = "Auremo";
            string coverArt = "CoverArt";
            m_BaseDirectory = Path.Combine(root, auremo, coverArt);

            try
            {   // TODO quite a few combines here.
                if (!Directory.Exists(Path.Combine(root, auremo)))
                {
                    Directory.CreateDirectory(Path.Combine(root, auremo));
                }

                if (!Directory.Exists(Path.Combine(root, auremo, coverArt)))
                {
                    Directory.CreateDirectory(Path.Combine(root, auremo, coverArt));
                }
            }
            catch (Exception)
            {
            }
        }

        #region Run loop

        private void Run()
        {
            CoverArtFetchTask request = m_Parent.PopRequest();

            while (request != null)
            {
                if (request.Request == CoverArtFetchTask.RequestType.Delete)
                {
                    RemoveCoverFromDiskCache(request.Artist, request.Album);
                }
                else
                {
                    ImageSource image = CoverOf(request.Artist, request.Album);
                    m_Parent.CoverArtFetchFinished(request.Artist, request.Album, image);
                }

                request = m_Parent.PopRequest();
            }
        }

        private ImageSource CoverOf(string artist, string album)
        {
            BitmapFrame result = FetchCoverFromDiskCache(artist, album);

            if (result == null)
            {
                result = FetchCoverFromMusicBrainz(artist, album);

                if (result != null)
                {
                    SaveCoverToDiskCache(artist, album, result);
                }
            }

            return result;
        }

        #endregion

        #region Disk cache load/store

        private BitmapFrame FetchCoverFromDiskCache(string artist, string album)
        {
            BitmapFrame result = null;

            try
            {
                string directory = Path.Combine(m_BaseDirectory, Utils.EncodeFilename(artist));
                string filename = Path.Combine(directory, Utils.EncodeFilename(album) + ".png");
                
                if (Directory.Exists(directory) && File.Exists(filename))
                {
                    Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    result = decoder.Frames[0];
                    stream.Close();
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        private void SaveCoverToDiskCache(string artist, string album, BitmapFrame image)
        {
            try
            {
                string directory = Path.Combine(m_BaseDirectory, Utils.EncodeFilename(artist));

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string filename = Path.Combine(directory, Utils.EncodeFilename(album) + ".png");
                Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Interlace = PngInterlaceOption.Off;
                encoder.Frames.Add(image);
                encoder.Save(stream);
                stream.Close();
            }
            catch (Exception)
            {
            }
        }

        private void RemoveCoverFromDiskCache(string artist, string album)
        {
            try
            {
                string directory = Path.Combine(m_BaseDirectory, Utils.EncodeFilename(artist));

                if (Directory.Exists(directory))
                {
                    string filename = Path.Combine(directory, Utils.EncodeFilename(album) + ".png");
                    File.Delete(filename);
                }

                
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region MusicBrainz fetch interface

        private BitmapFrame FetchCoverFromMusicBrainz(string artist, string album)
        {
            IList<string> mbids = FetchMusicBrainzMbids(artist, album);
            string artUrl = FetchCoverArtArchiveLink(mbids);
            return FetchCoverImage(artUrl);
        }

        private IList<string> FetchMusicBrainzMbids(string artist, string album)
        {
            try
            {
                string url = "http://musicbrainz.org/ws/2/release/?query=artist:" + HttpUtility.UrlPathEncode(artist) + "+release-accent:" + HttpUtility.UrlPathEncode(album);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.UserAgent = "Auremo MPD client";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                IList<string> mbids = new List<string>();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = response.GetResponseStream();

                    if (stream != null)
                    {
                        XmlReader xml = XmlReader.Create(stream);

                        while (xml.ReadToFollowing("release") && xml.MoveToAttribute("id"))
                        {
                            mbids.Add(xml.Value);
                        }
                    }
                }

                response.Close();
                return mbids.Count == 0 ? null : mbids;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string FetchCoverArtArchiveLink(IList<string> mbids)
        {
            if (mbids == null)
            {
                return null;
            }

            foreach (string mbid in mbids)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create("http://coverartarchive.org/release/" + mbid + "/") as HttpWebRequest;
                    request.UserAgent = "Auremo MPD client";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream stream = response.GetResponseStream();
                        if (stream != null)
                        {
                            StreamReader reader = new StreamReader(stream);
                            string json = reader.ReadToEnd();
                            stream.Close();
                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            CoverArtArchiveResponse entries = serializer.Deserialize<CoverArtArchiveResponse>(json);

                            if (entries != null)
                            {
                                foreach (CoverArtArchiveEntry entry in entries.images)
                                {
                                    if (entry.front)
                                    {
                                        stream.Close();
                                        return entry.image;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException we)
                {
                    if (we.Response is HttpWebResponse)
                    {
                        HttpWebResponse response = we.Response as HttpWebResponse;

                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        private BitmapFrame FetchCoverImage(string artUrl)
        {
            try
            {
                JpegBitmapDecoder decoder = new JpegBitmapDecoder(new Uri(artUrl), BitmapCreateOptions.None, BitmapCacheOption.Default);
                return decoder.Frames[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        class CoverArtArchiveResponse
        {
            public CoverArtArchiveEntry[] images { get; set; }
        }

        class CoverArtArchiveEntry
        {
            public bool front { get; set; }
            public string image { get; set; }
        }

        #endregion
    }
}
