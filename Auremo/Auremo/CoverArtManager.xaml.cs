using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.IO;

namespace Auremo
{
    public partial class CoverArtManager : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        MainWindow m_Parent;
        CoverArtMatrixElement m_SelectedMatrixItem = null;

        public CoverArtManager(MainWindow parent)
        {
            InitializeComponent();

            m_Parent = parent;
            MatrixItems = new ObservableCollection<CoverArtMatrixElement>();
            DataContext = this;
            Update();
        }

        public void Update()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string auremo = "Auremo";
            string coverArt = "CoverArt";
            string baseDirectory = System.IO.Path.Combine(root, auremo, coverArt);

            try
            {
                if (Directory.Exists(baseDirectory))
                {
                    IEnumerable<string> artistDirs = Directory.EnumerateDirectories(baseDirectory);

                    foreach (string artistDir in artistDirs)
                    {
                        IEnumerable<string> albumFiles = Directory.EnumerateFiles(artistDir);
                        string artist = System.IO.Path.GetFileName(artistDir);
                        
                        foreach (string albumFile in albumFiles)
                        {
                            try
                            {
                                Stream stream = new FileStream(albumFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                                PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                BitmapFrame bitmap = decoder.Frames[0];
                                stream.Close();

                                string album = System.IO.Path.GetFileNameWithoutExtension(albumFile);
                                CoverArtMatrixElement newItem = new CoverArtMatrixElement(bitmap, artist, album);
                                MatrixItems.Add(newItem);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public ObservableCollection<CoverArtMatrixElement> MatrixItems
        {
            get;
            set;
        }

        public CoverArtMatrixElement SelectedMatrixItem
        {
            get
            {
                return m_SelectedMatrixItem;
            }
            set
            {
                if (value != m_SelectedMatrixItem)
                {
                    m_SelectedMatrixItem = value;
                    NotifyPropertyChanged("SelectedMatrixItem");
                }
            }
        }

        private void OnMatrixItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && sender is FrameworkElement)
            {
                FrameworkElement element = sender as FrameworkElement;

                if (element.DataContext != null && element.DataContext is CoverArtMatrixElement)
                {
                    SelectedMatrixItem = element.DataContext as CoverArtMatrixElement;
                }
            }
        }

        private void OnRemoveCoverClicked(object sender, RoutedEventArgs e)
        {
            if (SelectedMatrixItem != null)
            {
                m_Parent.DataModel.CoverArtRepository.RemoveCover(SelectedMatrixItem.Artist, SelectedMatrixItem.Album);
                MatrixItems.Remove(SelectedMatrixItem);
                SelectedMatrixItem = null;
            }
        }
    }

    public class CoverArtMatrixElement : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        private ImageSource m_Cover = null;
        private string m_Artist = null;
        private string m_Album = null;

        public CoverArtMatrixElement(ImageSource cover, string artist, string album)
        {
            Cover = cover;
            Artist = artist;
            Album = album;
        }

        public ImageSource Cover
        {
            get
            {
                return m_Cover;
            }
            set
            {
                if (value != m_Cover)
                {
                    m_Cover = value;
                    NotifyPropertyChanged("Cover");
                }
            }
        }

        public string Artist
        {
            get
            {
                return m_Artist;
            }
            set
            {
                if (value != m_Artist)
                {
                    m_Artist = value;
                    NotifyPropertyChanged("Artist");
                    NotifyPropertyChanged("ArtistAndAlbum");
                }
            }
        }

        public string Album
        {
            get
            {
                return m_Album;
            }
            set
            {
                if (value != m_Album)
                {
                    m_Album = value;
                    NotifyPropertyChanged("Album");
                    NotifyPropertyChanged("ArtistAndAlbum");
                }
            }
        }

        public string ArtistAndAlbum
        {
            get
            {
                return Artist + ": " + Album;
            }
        }
    }
}
