#3 e)
use disco;
drop table if exists ZanroviStat;
create table ZanroviStat(BrojZanrova varchar(255), BrojAlbuma int);

insert into ZanroviStat(BrojZanrova, BrojAlbuma)
select '1', count(*)  from album a
where a.AlbumId in (select ag.AlbumId from albumgenre ag
group by ag.AlbumId
having count(ag.AlbumId) = 1);

insert into ZanroviStat(BrojZanrova, BrojAlbuma)
select '2', count(*)  from album a
where a.AlbumId in (select ag.AlbumId from albumgenre ag
group by ag.AlbumId
having count(ag.AlbumId) = 2);

insert into ZanroviStat(BrojZanrova, BrojAlbuma)
select '3', count(*)  from album a
where a.AlbumId in (select ag.AlbumId from albumgenre ag
group by ag.AlbumId
having count(ag.AlbumId) = 3);

insert into ZanroviStat(BrojZanrova, BrojAlbuma)
select 'vise od 4', count(*)  from album a
where a.AlbumId in (select ag.AlbumId from albumgenre ag
group by ag.AlbumId
having count(ag.AlbumId) >= 4);


select * from ZanroviStat