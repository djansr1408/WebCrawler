#3 b)
use disco;
select g.GenreName, count(*) from genre g, albumgenre ag
where g.GenreId = ag.GenreId
group by g.GenreId
order by count(*) desc limit 6;