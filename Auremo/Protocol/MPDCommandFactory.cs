namespace Auremo
{
    public static class MPDCommandFactory
    {
        // The commands are in the order in which they appear in the
        // protocol spec.

        #region Querying MPD's status

        public static MPDCommand CurrentSong()
        {
            return new MPDCommand("currentsong");
        }

        public static MPDCommand Status()
        {
            return new MPDCommand("status");
        }

        public static MPDCommand Stats()
        {
            return new MPDCommand("stats");
        }

        #endregion

        #region Playback options

        public static MPDCommand Consume(bool on)
        {
            return new MPDCommand("consume", on ? "1" : "0");
        }

        public static MPDCommand Crossfade(int duration)
        {
            return new MPDCommand("crossfade", duration);
        }

        public static MPDCommand MixRampdb(double threshold)
        {
            return new MPDCommand("mixrampdb", threshold);
        }

        public static MPDCommand MixRampDelay(double duration)
        {
            return new MPDCommand("mixrampdelay", duration);
        }

        public static MPDCommand Random(bool to)
        {
            return new MPDCommand("random", to);
        }

        public static MPDCommand Repeat(bool to)
        {
            return new MPDCommand("repeat", to);
        }

        public static MPDCommand SetVol(int vol)
        {
            return new MPDCommand("setvol", vol);
        }

        public static MPDCommand Single(bool on)
        {
            return new MPDCommand("single", on ? "1" : "0");
        }

        // TODO: replay gain!

        #endregion

        #region Controlling playback

        public static MPDCommand Next()
        {
            return new MPDCommand("next");
        }

        public static MPDCommand Pause()
        {
            return new MPDCommand("pause");
        }

        public static MPDCommand Play()
        {
            return new MPDCommand("play");
        }

        public static MPDCommand PlayId(int id)
        {
            return new MPDCommand("playid", id);
        }

        public static MPDCommand Previous()
        {
            return new MPDCommand("previous");
        }

        public static MPDCommand Seek(int songIndex, int position)
        {
            return new MPDCommand("seek", songIndex, position);
        }

        public static MPDCommand Stop()
        {
            return new MPDCommand("stop");
        }

        #endregion

        #region The current playlist

        public static MPDCommand Add(string path)
        {
            return new MPDCommand("add", path);
        }

        public static MPDCommand AddId(string path, int position)
        {
            return new MPDCommand("addid", path, position);
        }

        public static MPDCommand Clear()
        {
            return new MPDCommand("clear");
        }

        public static MPDCommand DeleteId(int id)
        {
            return new MPDCommand("deleteid", id);
        }

        public static MPDCommand MoveId(int id, int position)
        {
            return new MPDCommand("moveid", id, position);
        }

        public static MPDCommand PlaylistInfo()
        {
            return new MPDCommand("playlistinfo");
        }

        public static MPDCommand Shuffle()
        {
            return new MPDCommand("shuffle");
        }

        #endregion

        #region Stored playlists

        public static MPDCommand ListPlaylistInfo(string playlist)
        {
            return new MPDCommand("listplaylistinfo", playlist);
        }

        public static MPDCommand Load(string name)
        {
            return new MPDCommand("load", name);
        }

        public static MPDCommand Rename(string oldName, string newName)
        {
            return new MPDCommand("rename", oldName, newName);
        }

        public static MPDCommand Rm(string name)
        {
            return new MPDCommand("rm", name);
        }

        public static MPDCommand Save(string name)
        {
            return new MPDCommand("save", name);
        }

        #endregion

        #region The music database

        public static MPDCommand ListAllInfo()
        {
            return new MPDCommand("listallinfo");
        }

        public static MPDCommand LsInfo()
        {
            return new MPDCommand("lsinfo");
        }

        public static MPDCommand Search(string type, string what)
        {
            return new MPDCommand("search", type, what);
        }

        public static MPDCommand Update()
        {
            return new MPDCommand("update");
        }

        #endregion

        #region Connection settings

        public static MPDCommand Close()
        {
            return new MPDCommand("close");
        }

        public static MPDCommand Password(string password)
        {
            return new MPDCommand("password", password);
        }

        #endregion

        #region Audio output devices

        public static MPDCommand EnableOutput(int index)
        {
            return new MPDCommand("enableoutput", index);
        }

        public static MPDCommand DisableOutput(int index)
        {
            return new MPDCommand("disableoutput", index);
        }

        public static MPDCommand Outputs()
        {
            return new MPDCommand("outputs");
        }

        #endregion
    }
}
