FhizMod - Fhizban's uMMORPG Modifications & Add-Ons
------------------------------------------------------------------------------------------

All modifications & add-ons require the uMMORPG Asset and assume that you use either a fresh
project or a correctly updated project. Modifications & Add-ons relate to the stated version
of uMMORPG. If you have any further questions, contact me on the unity forums.

Modifications require you to change the core script codes. Add-Ons on the other hand will
make use of the upcoming Add-On system.

* uMMORPG on the asset store: 	https://www.assetstore.unity3d.com/en/#!/content/51212
* uMMORPG official discussion: 	https://forum.unity3d.com/threads/ummorpg-official-thread.376636/
* uMMORPG official documentation:	https://noobtuts.com/unity/MMORPG

Check out all my uMMORPG Modfications & Add-Ons:	http://ummorpg.critical-hit.biz

It is recommended to backup your projects before applying any modifications to it!

mySql Modification for uMMORPG 1.68
==========================================================================================

Original Code by camta005

1. Setup a mySql database wherever you host your server. Setup access rights correctly, so
that your server can connect to it. Some server setups also require further configuration,
otherwise the script generates errors. Modify your .cnf file if that is the case:

/etc/my.cnf
or
/etc/mysql/mysql.conf.d/mysqld.cnf

I used WinSCP to find the file (do this however you want) and edited it with notepad, adding the following:

[mysqld]
character-set-server=utf8
collation-server=utf8_general_ci

(Look to see if the file already has the [mysqld] heading)

Note: Here are a few other things that could be noteworthy: Tunnel port 7777 on your local
machine. Provide port tunnels to your server for the mySQL database 3306 (TCP) as well as
7777 (both UDP and TCP) for the server.

2. Log into mySql and create a new database with character set utf8 and collation utf8_general_ci.

3. Import the provided file uMMORPG_mySql.sql into your database.

4. From the provided files: Move the MySql.Data.dll into the /Assets/uMMORPG/Plugins folder in your project.

5. From the provided files: Copy Constants.cs and edit your database credentials, put the
file into your projects /Assets/uMMORPG/Scripts folder.

6. From the provided files: Copy Database_mySql.cs and add it to your projects script folder.
/Assets/uMMORPG/Scripts

7. Locate your original database.cs and change the following two lines:

	* public class database {
	* static database {
	
	into
	
	* public class database_mysqlite {
	* static database_mysqlite {
	
	The engine now uses the new database_mysql.cs file instead of the original one. This
	way you can revert back to the original system if you ever want/need to.
	
	Note: Any changes applied to your original database.cs file are not represented in
	the new database_mysql.cs file. You have to apply these changes manually, that is
	beyond the responsibility of this modification.
	
8. Save your project, re-compile both server and client (this is recommended, especially
on Linux). Upload and run.

Note: You should always re-compile both the server and the client before starting a new
test run. Otherwise this can result in unexpected errors that are hard to track down, for
example the ReadString/ReadBytes too long bug might relate to this.

------------------------------------------------------------------------------------------

Final Notes:

The mySql.sql file contains a new row in accounts, as well as two new tables
(warehouse_player and warehouse_guild). These are required for my next modification, the
updated warehouse script. The code is fully functional without a warehouse!

The only thing that happens is that your accounts table has one more row and that
there are two tables sitting in your database that are not used right now.


Thats all!

------------------------------------------------------------------------------------------