
#2 a)
select g.GenreName, count(*) from SongGenre sg, genre g
where g.GenreId = sg.GenreId
group by sg.GenreId;

#2 b)
select s.StyleName, count(*) from SongStyle ss, style s
where s.StyleId = ss.StyleId
group by ss.StyleId;

#2 c)
select * from album
where NumVersions >= (select NumVersions from album
			order by NumVersions desc limit 9, 1)
ORDER BY NumVersions desc;

#2 d) 
#
select * from Artist
where NumCredits >= (select NumCredits from artist
                    order by NumCredits desc limit 49, 1)
order by NumCredits desc;

#
select * from Artist
where NumVocals >= (select NumVocals from artist
                    order by NumVocals desc limit 49, 1)
order by NumVocals desc;

#
select * from Artist
where NumWritingArrangement >= (select NumWritingArrangement from artist
                    order by NumWritingArrangement desc limit 49, 1)
order by NumWritingArrangement desc;


#2 e)
select asong.SongId, s.SongName, s.Released, a.AlbumName, a.Country, a.Released, count(*) from albumsong asong, album a, song s
where a.AlbumId = asong.AlbumId
      and asong.SongId = s.SongId
group by asong.SongId
order by count(*) desc limit 5;


drop table  if exists DistinctAlbumGenre;
create table if not exists DistinctAlbumGenre as select distinct AlbumId, GenreId  from albumgenre;

select asong.SongId, s.SongName, s.Released, a.AlbumName, a.Country, a.Released, count(*) from song s
INNER  join albumsong asong on asong.SongId = s.SongId
INNER  join album a on a.AlbumId = asong.AlbumId
INNER  join DistinctAlbumGenre dag on a.AlbumId = dag.AlbumId
group by asong.SongId
order by count(*) desc limit 5;

