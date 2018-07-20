using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TestParserSite
{
    class Program
    {
        static string mainurl_serbia = "https://www.discogs.com/search/?country_exact=Serbia";
        static string mainurl_yugo = "https://www.discogs.com/search/?country_exact=Yugoslavia";
        //static string mainurl = "https://www.discogs.com/search/?genre_exact=Folk%2C+World%2C+%26+Country";
        static void Main(string[] args)
        {
            //work with database
            string connString = "Data Source=localhost;Port=3306;Database=disco;User Id=root;password=djansr8041";
            MySqlConnection conn = new MySqlConnection(connString);
            
            //command.CommandText = "INSERT INTO pet VALUES('Puffball', 'Diane', 'hamster', 'f', '1999-03-30', NULL)";
            //command.CommandText = "CREATE TABLE Albums (albumName VARCHAR(50), owner VARCHAR(20), species VARCHAR(20), sex CHAR(1), birth DATE, death DATE)";
            try
            {
                conn.Open();
                MySqlCommand command = conn.CreateCommand();
                //command.CommandText = @"drop table if exists SongGenre, SongStyle, AlbumStyle, AlbumSong, AlbumGenre, AlbumArtist, Song, Artist, Album, Genre, Style";
                //int r = command.ExecuteNonQuery();
                // Create table Genre
                command.CommandText = @"create table if not exists Genre(GenreId int not null AUTO_INCREMENT PRIMARY KEY,
                                                           GenreRealName varchar(255),
                                                           GenreName varchar(255),
                                                           GenreAlias varchar(255));";
                var r = command.ExecuteNonQuery();

                // Create table Styl
                command.CommandText = @"create table if not exists Style(StyleId int not null AUTO_INCREMENT PRIMARY KEY,
                                                           StyleRealName varchar(255),
                                                           StyleName varchar(255));";
                r = command.ExecuteNonQuery();

                // Create table Song
                command.CommandText = @"create table if not exists Song(SongId int not null AUTO_INCREMENT PRIMARY KEY,
                                                          ExternalSongId varchar(512),  
                                                          SongName varchar(255),
                                                          Released int);";
                r = command.ExecuteNonQuery();

                // Create table Artist
                command.CommandText = @"create table if not exists Artist(ArtistId int not null AUTO_INCREMENT PRIMARY KEY,
                                                          ExternalArtistId int,
                                                          ArtistName varchar(255),
                                                          NumCredits int, NumVocals int, NumWritingArrangement int);";
                r = command.ExecuteNonQuery();

                // Create table Album
                command.CommandText = @"create table if not exists Album(AlbumId int not null AUTO_INCREMENT PRIMARY KEY,
                                                          ExternalAlbumId int,
                                                          AlbumName varchar(255),
                                                          Country varchar(255), Format varchar(255), 
                                                          Released int, NumVersions int, IsCyrilic boolean);";
                r = command.ExecuteNonQuery();

                // Create aggregation table AlbumArtist
                command.CommandText = @"create table if not exists AlbumArtist(AlbumArtistId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                               AlbumId int, ArtistId int, 
                                                                               FOREIGN KEY (AlbumId) REFERENCES Album(AlbumId),
                                                                               FOREIGN KEY (ArtistId) REFERENCES Artist(ArtistId));";
                r = command.ExecuteNonQuery();

                // Create aggregation table AlbumGenre
                command.CommandText = @"create table if not exists AlbumGenre(AlbumGenreId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                              AlbumId int, GenreId int, 
                                                                              FOREIGN KEY (AlbumId) REFERENCES Album(AlbumId),
                                                                              FOREIGN KEY (GenreId) REFERENCES Genre(GenreId));";

                r = command.ExecuteNonQuery();

                // Create aggregation table AlbumStyle
                command.CommandText = @"create table if not exists AlbumStyle(AlbumStyleId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                              AlbumId int , StyleId int,
                                                                              FOREIGN KEY (AlbumId) REFERENCES Album(AlbumId),
                                                                              FOREIGN KEY (StyleId) REFERENCES Style(StyleId));";
                r = command.ExecuteNonQuery();

                // Create aggregation table AlbumSong
                command.CommandText = @"create table if not exists AlbumSong(AlbumSongId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                             AlbumId int, SongId int, 
                                                                             FOREIGN KEY (AlbumId) REFERENCES Album(AlbumId),
                                                                             FOREIGN KEY (SongId) REFERENCES Song(SongId));";
                r = command.ExecuteNonQuery();

                // Create aggregation table SongGenre
                command.CommandText = @"create table if not exists SongGenre(SongGenreId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                             SongId int, GenreId int,
                                                                             FOREIGN KEY (SongId) REFERENCES Song(SongId),
                                                                             FOREIGN KEY (GenreId) REFERENCES Genre(GenreId));";
                r = command.ExecuteNonQuery();

                // Create aggregation table SongStyle
                command.CommandText = @"create table if not exists SongStyle(SongStyleId int not null AUTO_INCREMENT PRIMARY KEY,
                                                                             SongId int, StyleId int,
                                                                             FOREIGN KEY (SongId) REFERENCES Song(SongId),
                                                                             FOREIGN KEY (StyleId) REFERENCES Style(StyleId));";
                r = command.ExecuteNonQuery();

                // Insert starts here
                var genres = ParserHelper.GetAllGenres();
                command.CommandText = "select count(*) from Genre;";
                if(int.Parse(command.ExecuteScalar().ToString()) == 0)
                {
                    foreach (var genre in genres)
                    {
                        string alias = genre.GenreName;
                        if (alias.Contains("Folk")) alias = "Folk";
                        if (alias.Contains("Brass")) alias = "Brass";
                        command.CommandText = "insert into Genre(GenreRealName, GenreName, GenreAlias) " +
                            String.Format(@"VALUES ('{0}', '{1}', '{2}');", genre.GenreExternalName.Replace("'", "''"), genre.GenreName.Replace("'", "''"), alias.Replace("'", "''"));
                        r = command.ExecuteNonQuery();
                    }
                }

                var styles = ParserHelper.GetAllStyles();
                command.CommandText = "select count(*) from Style;";
                if (int.Parse(command.ExecuteScalar().ToString()) == 0)
                {
                    foreach (var style in styles)
                    {
                        command.CommandText = "insert into Style(StyleRealName, StyleName) " +
                            String.Format("VALUES ('{0}', '{1}');", style.StyleExternalName.Replace("'", "''"), style.StyleName.Replace("'", "''"));
                        r = command.ExecuteNonQuery();
                    }
                }

                var url = mainurl_serbia;
                int max_num_pages = 50;
                while(url != "" && max_num_pages > 0)
                {
                    if(max_num_pages == 25)
                    {
                        url = mainurl_yugo;
                    }
                    var data = RequestHelper.GetPageData(url);
                    List<Album> albums = ParserHelper.ExtractAlbumsFromPage(data);
                    if(albums == null)
                    {
                        url = ParserHelper.GetNextPageUrl(data);
                        max_num_pages--;
                        continue;
                    }
                    int numAlbum = 0;
                    foreach (var album in albums)
                    {
                        try
                        {
                            Console.WriteLine(String.Format("Passing album num: {0}", numAlbum));
                            numAlbum++;
                            command.CommandText = "select count(*) from Album where ExternalAlbumId=" + String.Format("{0};", album.ExternalAlbumId);
                            var res = int.Parse(command.ExecuteScalar().ToString());
                            if (res == 0) // if album not already existing
                            {
                                command.CommandText = "insert into Album(ExternalAlbumId, AlbumName, Country, Released, NumVersions, IsCyrilic) " +
                                                    String.Format("VALUES({0}, '{1}', '{2}', {3}, {4}, {5});", album.ExternalAlbumId,
                                                    album.Name.Replace("'", "''"), album.Country.Replace("'", "''"), album.Released, album.NumVersions, album.IsCyrilic);
                                r = command.ExecuteNonQuery();
                            }
                            else
                            {
                                continue;
                            }
                            // Get AlbumId
                            command.CommandText = "select AlbumId from Album where ExternalAlbumId=" + String.Format("{0};", album.ExternalAlbumId);
                            MySqlDataReader reader = command.ExecuteReader();
                            reader.Read();
                            var albumId = reader["AlbumId"];
                            reader.Close();

                            // insert artists
                            if(album.Artist != null)
                            {
                                foreach (var artist in album.Artist)
                                {
                                    command.CommandText = "select count(*) from Artist where ExternalArtistId=" + String.Format("{0};", artist.ExternalArtistId);
                                    if (int.Parse(command.ExecuteScalar().ToString()) == 0)
                                    {
                                        command.CommandText = "insert into Artist(ExternalArtistId, ArtistName, NumCredits, NumVocals, NumWritingArrangement) " +
                                            String.Format("VALUES ({0}, '{1}', {2}, {3}, {4})", artist.ExternalArtistId, artist.Name.Replace("'", "''"), artist.NumCredits, artist.NumVocals, artist.NumWritingArrangement);
                                        r = command.ExecuteNonQuery();
                                    }

                                    // Get ArtistId
                                    command.CommandText = "select ArtistId from Artist where ExternalArtistId=" + String.Format("{0};", artist.ExternalArtistId);
                                    reader = command.ExecuteReader();
                                    reader.Read();
                                    var artistId = reader["ArtistId"];
                                    reader.Close();

                                    // insert into AlbumArtist
                                    command.CommandText = "insert into AlbumArtist(AlbumId, ArtistId) " + String.Format("VALUES({0}, {1});", albumId, artistId);
                                    r = command.ExecuteNonQuery();

                                }
                            }
                            if(album.Tracklist != null)
                            {
                                foreach (var song in album.Tracklist)
                                {
                                    command.CommandText = "select count(*) from Song where ExternalSongId=" + String.Format("'{0}';", song.ExternalSongId.Replace("'", "''"));
                                    if (int.Parse(command.ExecuteScalar().ToString()) == 0) // if song not exist
                                    {
                                        command.CommandText = "insert into Song(ExternalSongId, SongName, Released)" + String.Format("VALUES ('{0}', '{1}', {2})", song.ExternalSongId.Replace("'", "''"),
                                                                                                                                                                    song.Name.Replace("'", "''"),
                                                                                                                                                                    song.Released);
                                        r = command.ExecuteNonQuery();
                                    }

                                    // get SongID
                                    command.CommandText = "select SongId from Song where ExternalSongId=" + String.Format("'{0}';", song.ExternalSongId.Replace("'", "''"));
                                    reader = command.ExecuteReader();
                                    reader.Read();
                                    var songId = reader["SongId"];
                                    reader.Close();

                                    // check if AlbumSong already contains this combination
                                    command.CommandText = String.Format("select * from AlbumSong where AlbumId={0} and SongId={1};", albumId, songId);
                                    reader = command.ExecuteReader();
                                    if (reader.HasRows)
                                    {
                                        reader.Close();
                                        continue;
                                    }
                                    reader.Close();

                                    // insert into AlbumSong
                                    command.CommandText = "insert into AlbumSong(AlbumId, SongId) " + String.Format("VALUES({0}, {1});", albumId, songId);
                                    r = command.ExecuteNonQuery();

                                    // insert into SongGenre
                                    command.CommandText = "select * from Genre";
                                    reader = command.ExecuteReader();
                                    List<int> genreIdsToInsert = new List<int>();
                                    while (reader.Read())
                                    {
                                        int genreId = int.Parse(reader["GenreId"].ToString());
                                        string genreAlias = reader["GenreAlias"].ToString();
                                        if (song.Genres.Contains(genreAlias))
                                        {
                                            genreIdsToInsert.Add(genreId);
                                        }
                                    }
                                    reader.Close();
                                    foreach (var genId in genreIdsToInsert)
                                    {
                                        command.CommandText = "insert into SongGenre(SongId, GenreId) " + String.Format("VALUES({0}, {1});", songId, genId);
                                        r = command.ExecuteNonQuery();
                                    }

                                    // insert into SongStyle
                                    if(song.Styles != null)
                                    {
                                        foreach (var songStyle in song.Styles)
                                        {
                                            command.CommandText = "select StyleId from Style where StyleName=" + String.Format("'{0}'", songStyle.Replace("'", "''"));
                                            reader = command.ExecuteReader();
                                            if (reader.HasRows)
                                            {
                                                reader.Read();
                                                int stId = int.Parse(reader["StyleId"].ToString());
                                                reader.Close();
                                                command.CommandText = "insert into SongStyle(SongId, StyleId) " + String.Format("VALUES({0}, {1});", songId, stId);
                                                r = command.ExecuteNonQuery();
                                            }

                                        }
                                    }
                                }
                            }

                            if(album.Genres != null)
                            {
                                foreach (var albumGenre in album.Genres)
                                {
                                    // get GenreId
                                    command.CommandText = "select GenreId from Genre where GenreRealName=" + String.Format("'{0}';", albumGenre.GenreExternalName.Replace("'", "''"));
                                    reader = command.ExecuteReader();
                                    reader.Read();
                                    var genreId = reader["GenreId"];
                                    reader.Close();

                                    // insert into AlbumGenre
                                    command.CommandText = "insert into AlbumGenre(AlbumId, GenreId) " + String.Format("VALUES ({0}, {1})", albumId, genreId);
                                    r = command.ExecuteNonQuery();
                                }
                            }

                            if(album.Styles != null)
                            {
                                foreach (var albumStyle in album.Styles)
                                {
                                    // get StyleId
                                    command.CommandText = "select StyleId from Style where StyleRealName=" + String.Format("'{0}';", albumStyle.StyleExternalName.Replace("'", "''"));
                                    reader = command.ExecuteReader();
                                    reader.Read();
                                    var styleId = reader["StyleId"];
                                    reader.Close();

                                    // insert into AlbumStyle
                                    command.CommandText = "insert into AlbumStyle(AlbumId, StyleId) " + String.Format("VALUES({0}, {1});", albumId, styleId);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    url = ParserHelper.GetNextPageUrl(data);
                    max_num_pages--;
                }
                conn.Close();
            }
                
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                conn.Close();
            }
            
        }
    }
}
