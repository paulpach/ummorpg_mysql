# UMMORPG Mysql Addon

This is a Mysql Addon for uMMORPG 1.144 and [previous versions](https://github.com/paulpach/ummorpg_mysql/releases)

This is pretty much a drop in replacement for the sqlite Database.cs that comes with uMMORPG.  

There are a few enhancement I made in this addon not present in the sqlite version:

* Primary keys and indexes.  This can greatly improve performance when you have lots of users
* Native mysql types such as boolean and Datetime, no awkard conversions
* Foreign keys,  make it really easy to do maintenance on your database and ensure data integrity
* utf8, character names in any language
* Normalized the tables.
* Optimize database access.  Don't do so many round trips to the database for inventory, skills and equipment.

# How to contribute

Send me pull requests if you want to see some changes.

Open issues if you find a bug

Or buy me a beer in my [patreon page](https://www.patreon.com/user?u=13679599) if you want to provide brain fuel.

# Installation instructions

### 1. Backup  
you have been warned

### 2. Install mysql
I recommend mysql 8.0 or later [community edition](https://dev.mysql.com/downloads/). 

### 3. Set character encoding (if MySQL version 7 or earlier)
If using MySQL 7 or earlier,  the default character set is `latin1`, which causes problems for the mysql driver.
You need to change it to utf8mb4 or you will get exceptions

edit my.cnf or my.ini and add these settings
```
[mysqld]
init_connect='SET collation_connection = utf8mb4_unicode_ci' 
init_connect='SET NAMES utf8mb4' 
character-set-server=utf8mb4 
collation-server=utf8mb4_unicode_ci 
```

### 4. Set mysql native password (if MySQL 8 or later)

By default,  mysql 8 uses `caching_sha2_password` authentication method.  We must use a .net 3.5 driver.  It is too old and does not support this authentication method.   You must change mysql to use `mysql_native_password` instead.

Add this to your my.cnf or my.ini
```
default-authentication-plugin=mysql_native_password
```

### 5. Restart mysql

### 6. Validate 

Log into mysql and type:
```
show variables like "character_set_server";
```

Make sure that the `character_set_server` is set to `utf8mb4`.   If it didn't take the settings search for [all mysql configuration files](https://dev.mysql.com/doc/refman/8.0/en/option-files.html) in your system,  one of them might be overriding your setting.  

```
show variables like "default_authentication_plugin";
```

Make sure it says `mysql_native_password`

### 7. Create a database 
Create a user and database in mysql for your game.  For example:

```
create database ummorpg;
create user 'ummorpg'@'%'  identified by 'db_password';
grant all on ummorpg.* to 'ummorpg'@'%';
```

Make sure you can connect to your database from your server using the newly created account and database.

### 8. Set environment variables

Now you must tell ummorpg how to get to that database. Out of the box you do that by setting environment variables before running unity or your server. 

For windows: [environment variables](https://www.youtube.com/watch?v=bEroNNzqlF4).
For linux and mac,  add them to your `~/.bash_profile` 

~~~~
MYSQL_HOST=localhost
MYSQL_DATABASE=ummorpg
MYSQL_USER=ummorpg
MYSQL_PASSWORD=db_password
MYSQL_PORT=3306
~~~~

Adjust the settings according to your set up

If you don’t want to use environment variables, change the method `ConnectionString` near the top in `Database_MySql.cs`. I use environment variables because I deploy my server in docker containers.  

### 9. Run Unity and open your project

### 10. Delete Database.cs that comes with uMMORPG

### 11. Add the addon

Download all files from this repository and add them to your project. Put them wherever you want.

You don't need the [Addons](Addons) folder if you don't have NetworkZones.

### 12. Set up NetworkZones (optional)

follow [these instructions](Addons/NetworkZones/Readme.md).

### 13. Hit play and enjoy

# Docker instructions

### 1. Download and Install Docker

Depending on the operating system you want to use follow these directions: https://docs.docker.com/install/  

Note: According to the MySQL Docker help page you cannot set the value 'MYSQL_HOST=localhost' as it casues issue. So far I have not had an issue leaving it out of the configuration.

### 2. Create MySQL container (if MySQL version 7 or earlier)

```
docker run --name mysql \
-p 3306:3306 \
--restart always \
-v /docker/mysql/datadir:/var/lib/mysql \
-e MYSQL_ROOT_PASSWORD=CHANGEMEPLEASE \
-e MYSQL_DATABASE=ummorpg \
-e MYSQL_USER=ummorpg \
-e MYSQL_PASSWORD=db_password \
-d mysql:5.7.24 \
--character-set-server=utf8mb4 \
--collation-server=utf8mb4_unicode_ci
```

### 3. Create MySQL container (if MySQL 8 or later) THIS IS UNTESTED AT THIS TIME.

```
docker run --name mysql \
-p 3306:3306 \
--restart always \
-v /docker/mysql/datadir:/var/lib/mysql \
-e MYSQL_ROOT_PASSWORD=CHANGEMEPLEASE \
-e MYSQL_DATABASE=ummorpg \
-e MYSQL_USER=ummorpg \
-e MYSQL_PASSWORD=db_password \
-d mysql:latest \
--character-set-server=utf8mb4 \
--collation-server=utf8mb4_unicode_ci \
--default-authentication-plugin=mysql_native_password
```

### 4. For more information about MySQL in Docker please see this page: https://hub.docker.com/_/mysql/  

# Troubleshooting
Many addons add their own tables and columns.  
They will need to be modified to work with mysql.  
That is out of my control,  it is entirely up to you to update the addons.
If you do adapt the addons,  consider sending me a pull request so that other people can benefit.

If you get `KeyNotFoundException: The given key was not present in the dicionary` it is likely that you are using the wrong character set.  Go back to step 6 and make sure it is correctly configured.

This is provided as is,  no warranty,  I am not responsible if it offers your first born child in sacrifice to the devil.  
I am simply offering it for free for anyone who might want it.


