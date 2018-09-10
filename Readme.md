# UMMORPG Mysql Addon

This is a Mysql Addon for uMMORPG 1.136 and [previous versions](https://github.com/paulpach/ummorpg_mysql/releases)

This is pretty much a drop in replacement for the sqlite Database.cs that comes with uMMORPG.  

There are a few enhancement I made in this addon not present in the sqlite version:

* Primary keys and indexes.  This can greatly improve performance when you have lots of users
* Native mysql types such as boolean and Datetime, no awkard conversions
* Foreign keys,  make it really easy to do maintenance on your database and ensure data integrity
* utf8, character names in any language
* Normalized the tables.
* Optimize database access.  Don't do so many round trips to the database for inventory, skills and equipment.


## Installation instructions

1. Backup,  you have been warned
2. Install mysql
3. edit my.cnf or my.ini and add this:
```
[mysqld]
init_connect='SET collation_connection = utf8mb4_unicode_ci' 
init_connect='SET NAMES utf8mb4' 
character-set-server=utf8mb4 
collation-server=utf8mb4_unicode_ci 
```
4. If using mysql 8.0,  also add this to your my.cnf or my.ini
```
default-authentication-plugin=mysql_native_password
```

5. restart mysql
6. Create a database and ensure you can connect to it from your server
7. set these [environment variables](https://www.youtube.com/watch?v=bEroNNzqlF4) before running unity or your server:
~~~~
MYSQL_HOST=<your database server>
MYSQL_DATABASE=<your database name>
MYSQL_USER=<user name to connect to your database>
MYSQL_PASSWORD=<password to connect to your database>
MYSQL_PORT=<port to your database,  typically 3306>`
~~~~
8. Run Unity and open your project
9. Delete Database.cs that comes with uMMORPG
10. Add these files to your project.  You don't need the [Addons](Addons) folder if you don't have NetworkZones.
11. Hit play and enjoy
12. When you build a server,  make sure to export those environment variables too
13. (Optional) If you use NetworkZones,  follow [these instructions](Addons/NetworkZones/Readme.md).

Note, many addons add their own tables and columns.  
They will need to be modified to work with mysql.  
That is out of my control,  it is entirely up to you to update the addons.

If you don’t want to use environment variables, change where these fields are coming from in the method `ConnectionString` near the top. I use environment variables because I deploy my server in docker containers.

This is provided as is,  no warranty,  I am not responsible if it offers your first born child in sacrifice to the devil.  
I am simply offering it for free for anyone who might want it.
