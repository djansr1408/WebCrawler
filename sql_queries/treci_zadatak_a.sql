#3 a)
drop table if exists Dekade;
create table Dekade(Dekada varchar(255), BrojAlbuma int);

insert into Dekade(Dekada, BrojAlbuma)
select '1950-1960', count(*) from Album
where Released >= 1950 and Released < 1960
union
select '1960-1970', count(*) from Album
where Released >= 1960 and Released < 1970
union
select '1970-1980', count(*) from Album
where Released >= 1960 and Released < 1970
union
select '1970-1980', count(*) from Album
where Released >= 1970 and Released < 1980
union
select '1980-1990', count(*) from Album
where Released >= 1980 and Released < 1990
union
select '1990-2000', count(*) from Album
where Released >= 1990 and Released < 2000
union
select '2000-2010', count(*) from Album
where Released >= 2000 and Released < 2010
union
select '2010-2020', count(*) from Album
where Released >= 2010 and Released < 2020;
