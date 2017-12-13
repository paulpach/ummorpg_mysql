# UMMORPG Mysql Driver

This is a Mysql Driver for uMMORPG 1.98

This is pretty much a drop in replacement for the sqlite Database.cs that comes with uMMORPG.  

There are a few changes I made in this driver not present in the sqlite version:

* primary keys and indexes to the tables.  This can greatly improve performance when you have lots of users
* Native mysql types,  like boolean and Datetime,   no awkard conversions
* Foreign keys,  make it really easy to do maintenance on your database, and ensure data integrity.



To install follow these instructions:

1) Backup,  you have been warned.
2) Install mysql
3) Create a database and ensure you can connect to it from your server
4) export these environment variables before running unity or your server:
	 MYSQL_HOST=<your database server>
	 MYSQL_DATABASE=<your database name>
	 MYSQL_USER=<user name to connect to your database>
	 MYSQL_PASSWORD=<password to connect to your database>
	 MYSQL_PORT=<port to your database,  typically 3306>
5) Run Unity and open your project
6) Delete Database.cs that comes with uMMORPG
7) add these files to your project
8) Hit play and enjoy
9) When you build a server,  make sure to export those environment variables too


Note, many addons add their own tables or fields.  
They will need to be modified to work with mysql.  
That is out of my control,  it is entirely up to you to update the addons.


This is provided as is,  no guaranty,  I am not responsible if it offers your first born child in sacrifice to the devil.  
I am simply offering it for free for anyone who might want it.