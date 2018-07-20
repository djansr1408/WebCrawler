using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestParserSite
{
    public class ParserHelper
    {
        public static string[] CyrilicToLatinL = "a,b,v,g,d,e,zh,z,i,j,k,l,m,n,o,p,r,s,t,u,f,kh,c,ch,sh,sch,j,y,j,e,yu,ya".Split(',');
        public static string[] CyrilicToLatinU = "A,B,V,G,D,E,Zh,Z,I,J,K,L,M,N,O,P,R,S,T,U,F,Kh,C,Ch,Sh,Sch,J,Y,J,E,Yu,Ya".Split(',');

        public static string GetNextPageUrl(string htmlData)
        {
            if (htmlData == "")
                return "";
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlData);
            var nextPageUrl = htmlDoc.DocumentNode.SelectNodes("//a[@rel='next']") == null ? "" : htmlDoc.DocumentNode.SelectNodes("//a[@rel='next']").
                First().GetAttributeValue("href", "");
            if (nextPageUrl == "")
                return "";
            nextPageUrl = "https://www.discogs.com" + nextPageUrl;
            return nextPageUrl;
        }

        public static List<Album> ExtractAlbumsFromPage(string htmlData)
        {
            if(htmlData == "")
            {
                return new List<Album>();
            }
            try
            {
                List<Album> extractedAlbums = new List<Album>();
                if (string.IsNullOrEmpty(htmlData))
                    return extractedAlbums;
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlData);
                var albums = htmlDoc.DocumentNode.SelectNodes("//div[@class='cards cards_layout_large']//div").Where(x =>
                x.GetAttributeValue("class", "").StartsWith("card ")).ToList();
                int i = 0;
                foreach (HtmlNode set in albums)
                {
                    //Console.WriteLine("Parse {0} album.", i);
                    var albumUrl = set.QuerySelector("h4 a").GetAttributeValue("href", "");
                    albumUrl = string.Format("https://www.discogs.com/{0}", albumUrl);
                    Console.WriteLine(albumUrl);
                    var artist = set.QuerySelector("h5 a").GetAttributeValue("href", "");
                    Console.WriteLine(artist);
                    artist = Path.GetFileName(artist);
                    var albumData = RequestHelper.GetPageData(albumUrl);
                    Album album = ParserHelper.ParseAlbum(albumData);
                    if (album != null) extractedAlbums.Add(album);
                    i++;
                    //if (i == 1) break;
                }
                return extractedAlbums;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Album>();
            }
        }

        public static Album ParseAlbum(string htmlData)
        {
            if(htmlData == "")
            {
                return null;
            }
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlData);

                var infoTitle = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//h1") == null ? null :
                    htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//h1").First();

                /* ExternalAlbumId */
                var externalAlbumIdStr = htmlDoc.DocumentNode.SelectNodes("//head//meta[@property='og:url']") == null ? "" :
                    htmlDoc.DocumentNode.SelectNodes("//head//meta[@property='og:url']").First().GetAttributeValue("content", "");
                int externalAlbumId = Int32.Parse(Path.GetFileName(externalAlbumIdStr));

                /* Album name */
                var albumNameSection = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//h1").First();
                string albumName = "";
                if (albumNameSection.LastChild.LastChild.LastChild == null)
                {
                    albumName = albumNameSection.LastChild.InnerText.Trim();
                }
                else
                {
                    albumName = albumNameSection.LastChild.LastChild.InnerText.Trim();
                }

                /* Artist */
                var artistLinks = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//h1//a");
                var artists = new List<Artist>();
                foreach (var artistStr in artistLinks)
                {
                    if (artistStr.GetAttributeValue("href", "").Contains("artist"))
                    {
                        var artistUrl = "https://www.discogs.com" + artistStr.GetAttributeValue("href", "");
                        var artistData = RequestHelper.GetPageData(artistUrl);
                        var artist = ParseArtist(artistData);
                        if (artist != null) artists.Add(artist);
                    }
                }
                if (artists.Count == 0)
                {
                    artists.Add(new Artist { Name = "Unknown", ExternalArtistId = -1 });
                }

                /* Released */
                var releasedStr = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//a[contains(@href, 'year')]") == null ? "" :
                    htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//a[contains(@href, 'year')]").First().InnerText;
                int released = ParseYearFromDate(releasedStr);

                /* Format */
                var profileDivs = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//div");
                var formatStr = "";
                if (profileDivs != null)
                {
                    for (int i = 0; i < profileDivs.Count - 1; i++)
                    {
                        if (profileDivs[i].InnerHtml.Contains("Format:"))
                        {
                            formatStr = profileDivs[i + 1].InnerText.Trim();
                            break;
                            i++;
                        }
                    }
                }

                /* Genre, Style */
                List<Genre> albumGenres = new List<Genre>();
                List<Style> albumStyles = new List<Style>();
                var profileHrefs = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//a");
                foreach (var profileHref in profileHrefs)
                {
                    if (profileHref.GetAttributeValue("href", "").Contains("genre"))
                    {
                        albumGenres.Add(new Genre
                        {
                            GenreExternalName = Path.GetFileName(profileHref.GetAttributeValue("href", "")),
                            GenreName = profileHref.InnerText.Trim()
                        });
                    }
                    if (profileHref.GetAttributeValue("href", "").Contains("style"))
                    {
                        albumStyles.Add(new Style
                        {
                            StyleExternalName = Path.GetFileName(profileHref.GetAttributeValue("href", "")),
                            StyleName = profileHref.InnerText.Trim()
                        });
                    }
                }

                /* Country */
                var country = htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//a[contains(@href, 'country')]") == null ? "" :
                    htmlDoc.DocumentNode.SelectNodes("//div[@class='profile']//a[contains(@href, 'country')]").First().InnerText.Trim();

                /* numVersions */
                var versions = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'm_versions')]");
                int numVersions = 1;
                if (versions != null)
                {
                    var viewAll = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'm_versions')]//h3//a");
                    if (viewAll == null)
                    {
                        var numVersionsStr = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'm_versions')]//tr[contains(@class, 'card r_tr')]");
                        numVersions = numVersionsStr.Count;
                    }
                    else
                    {
                        var allVersionsLink = viewAll.First().GetAttributeValue("href", "");
                        allVersionsLink = "https://www.discogs.com" + allVersionsLink;
                        var versionsData = RequestHelper.GetPageData(allVersionsLink);
                        numVersions = ParseNumVersions(versionsData);
                    }
                }

                /* Cyrilic or latin */
                bool isCyrilic = !Regex.IsMatch(albumName, @"\P{IsCyrillic}");
                if(isCyrilic)
                {
                    albumName = CyrilicToLatin(albumName);
                }

                Album album = new Album
                {
                    Name = albumName,
                    Country = country,
                    Format = formatStr,
                    ExternalAlbumId = externalAlbumId,
                    NumVersions = numVersions,
                    Artist = artists,
                    Released = released,
                    Tracklist = new List<Song>(),
                    IsCyrilic = isCyrilic,
                    Genres = albumGenres,
                    Styles = albumStyles
                };

                /* Tracklist */
                var tracklistSections = htmlDoc.DocumentNode.SelectNodes("//div[@class='section tracklist']//table[@class='playlist']//tr[contains(@class, ' tracklist_track track')]");
                foreach (var tracklistSection in tracklistSections)
                {
                    var tracklistLink = tracklistSection.SelectNodes("td[@class='track tracklist_track_title ']//a") == null ? "" :
                        tracklistSection.SelectNodes("td[@class='track tracklist_track_title ']//a").First().GetAttributeValue("href", "");

                    /*if (tracklistLink == "")
                    {
                        continue;
                        string songName = tracklistSection.SelectNodes("td[@class='track tracklist_track_title ']//a") == null ? "" :
                        tracklistSection.SelectNodes("td[@class='track tracklist_track_title ']//a").First().InnerText.Trim();
                        Song s = new Song
                        {
                            Name = songName,
                            Genres = "",
                            Styles = new List<string>()
                        };
                        album.Tracklist.Add(s);
                    }*/
                    if(tracklistLink != "")
                    {
                        tracklistLink = string.Format("https://www.discogs.com{0}", tracklistLink);
                        var tracklistData = RequestHelper.GetPageData(tracklistLink);
                        Song song = ParseSong(tracklistData);
                        if (song != null) album.Tracklist.Add(song);
                    }
                }

                return album;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Parse album: " + ex.Message);
                return null;
            }
            
        }

        public static Song ParseSong(string htmlData)
        {
            if(htmlData == "")
            {
                return null;
            }
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlData);
                var songInfo = htmlDoc.DocumentNode.SelectNodes("//div[@class='TrackOverview']").First();

                /* Song name */
                var songName = songInfo.SelectNodes("//div[@class='TrackTitle']//h1").First().InnerText.Trim();

                /* ExternalId */
                var externalSongId = htmlDoc.DocumentNode.SelectNodes("//meta[@property='og:url']").First().GetAttributeValue("content", "");
                externalSongId = Path.GetFileName(externalSongId);

                var trackFacts = songInfo.SelectNodes("//div[@class='TrackFact']");

                Song song = new Song
                {
                    Name = songName == null ? "" : songName,
                    Released = 0,
                    ExternalSongId = externalSongId,
                    Styles = new List<string>()
                };

                foreach (var tf in trackFacts)
                {
                    var tmp = tf.QuerySelector("h4");
                    if (tmp.InnerText == "Release Date")
                    {
                        int songReleased = Int32.Parse(tf.InnerText.Replace("Release Date", ""));
                        song.Released = songReleased;
                    }
                    if (tmp.InnerText == "Genres")
                    {
                        string genres = tf.InnerText.Replace("Genres", "").Trim();
                        song.Genres = genres;
                    }
                    if (tmp.InnerText == "Styles")
                    {
                        string[] styles = tf.InnerText.Replace("Styles", "").Trim().
                                                        Split(',');
                        foreach (var style in styles)
                        {
                            if (style.Any(x => char.IsLetter(x)))
                            {
                                song.Styles.Add(style.Trim());
                            }
                        }
                    }
                }

                return song;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Parse song: " + ex.Message);
                return null;
            }
            
        }

        public static Artist ParseArtist(string htmlData)
        {
            if(htmlData == "")
            {
                return null;
            }
            try
            {
                if (htmlData.Contains("This artist is used as a placeholder"))
                {
                    return new Artist { Name = "Various", ExternalArtistId = 0, NumCredits = 0, NumVocals = 0, NumWritingArrangement = 0 };
                }
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlData);
                var artistIdLink = htmlDoc.DocumentNode.SelectNodes("//meta[@property='og:url']").First().
                    GetAttributeValue("content", "");
                string[] words = Path.GetFileName(artistIdLink).Split('-');
                var externalArtistId = Int32.Parse(words[0]);
                var artistName = Path.GetFileName(artistIdLink).Replace(words[0] + "-", "").Replace("-", " ");
                var credits = htmlDoc.DocumentNode.SelectNodes("//ul[@class='facets_nav']//a[@data-credit-type='Credits']");
                int numCredits = 0;
                int numVocals = 0;
                int numWritingArrangement = 0;
                if (credits != null)
                {
                    foreach (var credit in credits)
                    {
                        if (credit.GetAttributeValue("data-credit-subtype", "") == "All")
                        {
                            numCredits = Int32.Parse(credit.QuerySelector("span").InnerText.Trim());
                        }
                        if (credit.GetAttributeValue("data-credit-subtype", "") == "Vocals")
                        {
                            numVocals = Int32.Parse(credit.QuerySelector("span").InnerText.Trim());
                        }
                        if (credit.GetAttributeValue("data-credit-subtype", "") == "Writing-Arrangement")
                        {
                            numWritingArrangement = Int32.Parse(credit.QuerySelector("span").InnerText.Trim());
                        }
                    }
                }

                return new Artist
                {
                    Name = artistName,
                    ExternalArtistId = externalArtistId,
                    NumCredits = numCredits,
                    NumVocals = numVocals,
                    NumWritingArrangement = numWritingArrangement
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine("Parse artist: " + ex.Message);
                return null;
            }
            

        }

        public static int ParseNumVersions(string htmlData)
        {
            if (htmlData == "") return 1;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlData);
            var numVersions = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'm_versions')]//tr[contains(@class, 'card r_tr')]") == null ? 1 : 
                htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'm_versions')]//tr[contains(@class, 'card r_tr')]").Count;
            return numVersions;
        }

        public static int ParseYearFromDate(string str)
        {
            str = str.Trim();
            int year = 0;
            if (str.Length >= 4)
            {
                year = Int32.Parse(str.Substring(str.Length - 4));
            }
            return year;
        }

        public static string CyrilicToLatin(string s)
        {
            var sb = new StringBuilder((int)(s.Length * 1.5));
            foreach (char c in s)
            {
                if (c >= '\x430' && c <= '\x44f') sb.Append(CyrilicToLatinL[c - '\x430']);
                else if (c >= '\x410' && c <= '\x42f') sb.Append(CyrilicToLatinU[c - '\x410']);
                else if (c == '\x401') sb.Append("Yo");
                else if (c == '\x451') sb.Append("yo");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static List<Genre> GetAllGenres()
        {
            var htmlData = System.IO.File.ReadAllText(@"..//../../genresHtml.txt");
            if (htmlData == "") return new List<Genre>();
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlData);

            var genresStr = htmlDoc.DocumentNode.SelectNodes("//div[@class='react-modal-content']//ul[@class='facets_nav']//li");
            List<Genre> genres = new List<Genre>();
            foreach (var genreStr in genresStr)
            {
                var extName = genreStr.QuerySelector("a").GetAttributeValue("href", "").Split('=').Last();
                var genreName = genreStr.InnerText.Trim().Split('\n').Last().Trim();
                var genre = new Genre { GenreExternalName = extName, GenreName = genreName };
                genres.Add(genre);
            }
            return genres;
        }

        public static List<Style> GetAllStyles()
        {
            var htmlData = System.IO.File.ReadAllText(@"..//../../stylesHtml.txt");
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlData);

            var stylesStr = htmlDoc.DocumentNode.SelectNodes("//div[@class='react-modal-content']//ul[contains(@class, 'facets_nav')]//li");
            List<Style> styles = new List<Style>();
            foreach(var styleStr in stylesStr)
            {
                var extName = styleStr.QuerySelector("a").GetAttributeValue("href", "").Split('=').Last();
                var styleName = styleStr.InnerText.Trim().Split('\n').Last().Trim();
                var style = new Style { StyleExternalName = extName, StyleName = styleName };
                styles.Add(style);
            }
            return styles;
        }

    }
}
