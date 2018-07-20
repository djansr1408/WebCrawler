#3 d)
use disco;
drop table if exists PismoStat;
create table PismoStat(Pismo varchar(255), BrojAlbuma int);


select @cirilica := count(*) from album where IsCyrilic = TRUE;
select @latinica := count(*) from album where IsCyrilic = FALSE;
select @ukupno := count(*) from Album;


insert into PismoStat(Pismo, BrojAlbuma)
VALUES ('Cirilica', @cirilica);

insert into PismoStat(Pismo, BrojAlbuma)
VALUES ('Latinica', @latinica);

select * from PismoStat
