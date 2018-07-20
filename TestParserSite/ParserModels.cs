using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParserSite
{
    public class Song
    {
        public string Name { get; set; }
        public int Released { get; set; }
        public string ExternalSongId { get; set; }
        public string Genres { get; set; }
        public List<string> Styles { get; set; }
    }

    public class Genre
    {
        public string GenreExternalName { get; set; }
        public string GenreName { get; set; }
    }

    public class Style
    {
        public string StyleExternalName { get; set; }
        public string StyleName { get; set; }
    }

    public class Artist
    {
        public string Name { get; set; }
        public int ExternalArtistId { get; set; }
        public int NumCredits { get; set; }
        public int NumVocals { get; set; }
        public int NumWritingArrangement { get; set; }
    }

    public class Album
    {
        public int ExternalAlbumId { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Format { get; set; }
        public int Released { get; set; }
        public List<Genre> Genres { get; set; }
        public List<Style> Styles { get; set; }
        public List<Song> Tracklist { get; set; }
        public List<Artist> Artist { get; set; }
        public int NumVersions { get; set; }
        public bool IsCyrilic { get; set; }
    }
}
