

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using MySql.Data;								// From MySql.Data.dll in Plugins folder
using MySql.Data.MySqlClient;                   // From MySql.Data.dll in Plugins folder


using SqlParameter = MySql.Data.MySqlClient.MySqlParameter;


/// <summary>
/// Database class for mysql
/// Ported from the original uMMORPG version 1.98  Database.cs
/// </summary>
public partial class Database
{
    
    private static string connectionString = null;

    /// <summary>
    /// produces the connection string based on environment variables
    /// </summary>
    /// <value>The connection string</value>
    private static string ConnectionString {
        get {
            
            if (connectionString == null)
            {
                var connectionStringBuilder = new MySqlConnectionStringBuilder
                {
                    Server = GetEnv("MYSQL_HOST") ?? "localhost",
                    Database = GetEnv("MYSQL_DATABASE") ?? "ummorpg",
                    UserID = GetEnv("MYSQL_USER") ?? "ummorpg",
                    Password = GetEnv("MYSQL_PASSWORD") ?? "",
                    Port = GetUIntEnv("MYSQL_PORT", 3306),
                    CharacterSet = "utf8"

                };
                connectionString = connectionStringBuilder.ConnectionString;
            }

            return connectionString;
        }
    }

    private static void Transaction(Action<MySqlCommand> action)
    {
        using (var connection = new MySqlConnection(ConnectionString) )
        {

            connection.Open();
            MySqlTransaction transaction = null;

            try
            {

                transaction = connection.BeginTransaction();

                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.Transaction = transaction;

                action(command);

                transaction.Commit();

            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                throw ex;
            }
        }
    }

    private static String GetEnv(String name)
    {
        return Environment.GetEnvironmentVariable(name);

    }

    private static uint GetUIntEnv(String name, uint defaultValue = 0)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (value == null)
            return defaultValue;

        uint result;

        if (uint.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    private static void InitializeSchema()
    {

        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS accounts (
            name VARCHAR(16) NOT NULL,
            password CHAR(40) NOT NULL,
            banned BOOLEAN NOT NULL,
            PRIMARY KEY(name)
        ) CHARACTER SET=utf8");

        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS characters(
            name VARCHAR(16) NOT NULL,
            account VARCHAR(16) NOT NULL,

            class VARCHAR(16) NOT NULL,
            x FLOAT NOT NULL,
        	y FLOAT NOT NULL,
            z FLOAT NOT NULL,
        	level INT NOT NULL,
            health INT NOT NULL,
        	mana INT NOT NULL,
            strength INT NOT NULL,
        	intelligence INT NOT NULL,
            experience BIGINT NOT NULL,
        	skillExperience BIGINT NOT NULL,
            gold BIGINT NOT NULL,
        	coins BIGINT NOT NULL,
            online TIMESTAMP,

            deleted BOOLEAN NOT NULL,

        	PRIMARY KEY (name),
            INDEX(account),
        	FOREIGN KEY(account)
                REFERENCES accounts(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_inventory(
            `character` VARCHAR(16) NOT NULL,
            slot INT NOT NULL,
        	name VARCHAR(50) NOT NULL,
            valid BOOLEAN NOT NULL,
            amount INT NOT NULL,
        	petHealth INT NOT NULL,
            petLevel INT NOT NULL,
            petExperience INT NOT NULL,

            primary key(`character`, slot),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");

        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_equipment(
            `character` VARCHAR(16) NOT NULL,
            slot INT NOT NULL,
        	name VARCHAR(50) NOT NULL,
            valid BOOLEAN NOT NULL,
            amount INT NOT NULL,

            primary key(`character`, slot),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
         ) CHARACTER SET=utf8");

        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_skills(
            `character` VARCHAR(16) NOT NULL,
            name VARCHAR(50) NOT NULL,
            learned BOOLEAN NOT NULL ,
            level INT NOT NULL,
        	castTimeEnd FLOAT NOT NULL,
            cooldownEnd FLOAT NOT NULL,
        	buffTimeEnd FLOAT NOT NULL,

            INDEX(`character`),
            FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_quests(
            `character` VARCHAR(16) NOT NULL,
            name VARCHAR(50) NOT NULL,
            killed INT NOT NULL,
        	completed BOOLEAN NOT NULL,

            INDEX(`character`),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_orders(
            orderid BIGINT NOT NULL AUTO_INCREMENT,
            `character` VARCHAR(16) NOT NULL,
            coins BIGINT NOT NULL,
            processed BIGINT NOT NULL,

            PRIMARY KEY(orderid),
            INDEX(`character`),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS guild_info(
            name VARCHAR(16) NOT NULL,
            notice TEXT NOT NULL,
        	PRIMARY KEY(name)
        ) CHARACTER SET=utf8");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS guild_members(
            guild VARCHAR(16) NOT NULL,
            `character` VARCHAR(16) NOT NULL,
            rank INT NOT NULL,  
          	PRIMARY KEY(guild, `character`),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE,
            FOREIGN KEY(guild)
                REFERENCES guild_info(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8");

    }

    static Database()
    {
        Debug.Log("Initializing database");


        InitializeSchema();

        Utils.InvokeMany(typeof(Database), null, "Initialize_");

    }

    #region Helper Functions

    // run a query that doesn't return anything
    public static void ExecuteNonQuery(string sql, params SqlParameter[] args)
    {
        MySqlHelper.ExecuteNonQuery(ConnectionString, sql, args);
    }

    public static void ExecuteNonQuery(MySqlCommand command, string sql, params SqlParameter[] args)
    {
        command.CommandText = sql;
        command.Prepare();

        command.Parameters.Clear();

        foreach (var arg in args)
        {
            command.Parameters.Add(arg);
        }

        command.ExecuteNonQuery();
    }

    // run a query that returns a single value
    public static object ExecuteScalar(string sql, params SqlParameter[] args)
    {
        return MySqlHelper.ExecuteScalar(ConnectionString, sql, args);
    }

    // run a query that returns several values
    public static List<List<object>> ExecuteReader(string sql, params SqlParameter[] args)
    {
        var result = new List<List<object>>();

        using (var reader = MySqlHelper.ExecuteReader(ConnectionString, sql, args))
        {

            while (reader.Read())
            {
                var buf = new object[reader.FieldCount];
                reader.GetValues(buf);
                result.Add(buf.ToList());
            }
        }

        return result;
    }
    #endregion

        // account data ////////////////////////////////////////////////////////////
    public static bool IsValidAccount(string account, string password) {
        // this function can be used to verify account credentials in a database
        // or a content management system.
        //
        // for example, we could setup a content management system with a forum,
        // news, shop etc. and then use a simple HTTP-GET to check the account
        // info, for example:
        //
        //   var request = new WWW("example.com/verify.php?id="+id+"&amp;pw="+pw);
        //   while (!request.isDone)
        //       print("loading...");
        //   return request.error == null && request.text == "ok";
        //
        // where verify.php is a script like this one:
        //   <?php
        //   // id and pw set with HTTP-GET?
        //   if (isset($_GET['id']) && isset($_GET['pw'])) {
        //       // validate id and pw by using the CMS, for example in Drupal:
        //       if (user_authenticate($_GET['id'], $_GET['pw']))
        //           echo "ok";
        //       else
        //           echo "invalid id or pw";
        //   }
        //   ?>
        //
        // or we could check in a MYSQL database:
        //   var dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=notas;uid=root;password=" + dbpwd);
        //   var cmd = dbConn.CreateCommand();
        //   cmd.CommandText = "SELECT id FROM accounts WHERE id='" + account + "' AND pw='" + password + "'";
        //   dbConn.Open();
        //   var reader = cmd.ExecuteReader();
        //   if (reader.Read())
        //       return reader.ToString() == account;
        //   return false;
        //
        // as usual, we will use the simplest solution possible:
        // create account if not exists, compare password otherwise.
        // no CMS communication necessary and good enough for an Indie MMORPG.

        // not empty?
        if (!Utils.IsNullOrWhiteSpace(account) && !Utils.IsNullOrWhiteSpace(password)) {
            var table = ExecuteReader("SELECT password, banned FROM accounts WHERE name=@name", new SqlParameter("@name", account));
            if (table.Count == 1) {
                // account exists. check password and ban status.
                var row = table[0];
                return (string)row[0] == password && !(bool)row[1];
            } else {
                // account doesn't exist. create it.
                ExecuteNonQuery("INSERT INTO accounts VALUES (@name, @password, 0)", new SqlParameter("@name", account), new SqlParameter("@password", password));
                return true;
            }
        }
        return false;
    }

    // character data //////////////////////////////////////////////////////////
    public static bool CharacterExists(string characterName) {
        // checks deleted ones too so we don't end up with duplicates if we un-
        // delete one
        return ((long)ExecuteScalar("SELECT Count(*) FROM characters WHERE name=@name", new SqlParameter("@name", characterName))) == 1;
    }

    public static void CharacterDelete(string characterName) {
        // soft delete the character so it can always be restored later
        ExecuteNonQuery("UPDATE characters SET deleted=1 WHERE name=@character", new SqlParameter("@character", characterName));
    }

    // returns a dict of<character name, character class=prefab name>
    // we really need the prefab name too, so that client character selection
    // can read all kinds of properties like icons, stats, 3D models and not
    // just the character name
    public static Dictionary<string, string> CharactersForAccount(string account) {
        var result = new Dictionary<string, string>();

        var table = ExecuteReader("SELECT name, class FROM characters WHERE account=@account AND deleted=0", new SqlParameter("@account", account));
        foreach (var row in table)
            result[(string)row[0]] = (string)row[1];

        return result;
    }

    public static GameObject CharacterLoad(string characterName, List<Player> prefabs) {
        var table = ExecuteReader("SELECT * FROM characters WHERE name=@name AND deleted=0", new SqlParameter("@name", characterName));
        if (table.Count == 1) {
            var mainrow = table[0];

            // instantiate based on the class name
            string className = (string)mainrow[2];
            var prefab = prefabs.Find(p => p.name == className);
            if (prefab != null) {
                var go = GameObject.Instantiate(prefab.gameObject);
                var player = go.GetComponent<Player>();

                player.name               = (string)mainrow[0];
                player.account            = (string)mainrow[1];
                player.className          = (string)mainrow[2];
                float x                   = (float)mainrow[3];
                float y                   = (float)mainrow[4];
                float z                   = (float)mainrow[5];
                Vector3 position          = new Vector3(x, y, z);
                player.level              = (int)mainrow[6];
                player.health             = (int)mainrow[7];
                player.mana               = (int)mainrow[8];
                player.strength           = (int)mainrow[9];
                player.intelligence       = (int)mainrow[10];
                player.experience         = (long)mainrow[11];
                player.skillExperience    = (long)mainrow[12];
                player.gold               = (long)mainrow[13];
                player.coins              = (long)mainrow[14];

                // try to warp to loaded position.
                // => agent.warp is recommended over transform.position and
                //    avoids all kinds of weird bugs
                // => warping might fail if we changed the world since last save
                //    so we reset to start position if not on navmesh
                player.agent.Warp(position);
                if (!player.agent.isOnNavMesh) {
                    Transform start = NetworkManager.singleton.GetNearestStartPosition(position);
                    player.agent.Warp(start.position);
                    Debug.Log(player.name + " invalid position was reset");
                }

                // load inventory based on inventorySize (creates slots if none)
                for (int i = 0; i < player.inventorySize; ++i) {
                    // any saved data for that slot?
                    table = ExecuteReader("SELECT name, valid, amount, petHealth, petLevel, petExperience FROM character_inventory WHERE `character`=@character AND slot=@slot;", new SqlParameter("@character", player.name), new SqlParameter("@slot", i));
                    if (table.Count == 1) {
                        var row = table[0];
                        var item = new Item();
                        item.name = (string)row[0];
                        item.valid = (bool)row[1];
                        item.amount = (int)row[2];
                        item.petHealth = (int)row[3];
                        item.petLevel = (int)row[4];
                        item.petExperience = (int)row[5];

                        // add item if template still exists, otherwise empty
                        player.inventory.Add(item.valid && item.TemplateExists() ? item : new Item());
                    } else {
                        // add empty slot or default item if any
                        player.inventory.Add(i < player.defaultItems.Length ? new Item(player.defaultItems[i]) : new Item());
                    }
                }

                // load equipment based on equipmentInfo (creates slots if none)
                for (int i = 0; i < player.equipmentInfo.Length; ++i) {
                    // any saved data for that slot?
                    table = ExecuteReader("SELECT name, valid, amount FROM character_equipment WHERE `character`=@character AND slot=@slot", new SqlParameter("@character", player.name), new SqlParameter("@slot", i));
                    if (table.Count == 1) {
                        var row = table[0];
                        var item = new Item();
                        item.name = (string)row[0];
                        item.valid = (bool)row[1];
                        item.amount = (int)row[2];

                        // add item if template still exists, otherwise empty
                        player.equipment.Add(item.valid && item.TemplateExists() ? item : new Item());
                    } else {
                        // add empty slot or default item if any
                        EquipmentInfo info = player.equipmentInfo[i];
                        player.equipment.Add(info.defaultItem != null ? new Item(info.defaultItem) : new Item());
                    }
                }

                // load skills based on skill templates (the others don't matter)
                foreach (var template in player.skillTemplates) {
                    // create skill based on template
                    var skill = new Skill(template);

                    // load saved data if any
                    table = ExecuteReader("SELECT learned, level, castTimeEnd, cooldownEnd, buffTimeEnd FROM character_skills WHERE `character`=@character AND name=@name", new SqlParameter("@character", characterName), new SqlParameter("@name", template.name));
                    foreach (var row in table) {
                        skill.learned = (bool)row[0]; // sqlite has no bool
                        // make sure that 1 <= level <= maxlevel (in case we removed a skill
                        // level etc)
                        skill.level = Mathf.Clamp((int)row[1], 1, skill.maxLevel);
                        // castTimeEnd and cooldownEnd are based on Time.time, which
                        // will be different when restarting a server, hence why we
                        // saved them as just the remaining times. so let's convert them
                        // back again.
                        skill.castTimeEnd = (float)row[2] + Time.time;
                        skill.cooldownEnd = (float)row[3] + Time.time;
                        skill.buffTimeEnd = (float)row[4] + Time.time;
                    }

                    player.skills.Add(skill);
                }

                // load quests
                table = ExecuteReader("SELECT name, killed, completed FROM character_quests WHERE `character`=@character", new SqlParameter("@character", player.name));
                foreach (var row in table) {
                    var quest = new Quest();
                    quest.name = (string)row[0];
                    quest.killed = (int)row[1];
                    quest.completed = (bool)row[2];
                    player.quests.Add(quest.TemplateExists() ? quest : new Quest());
                }

                // in a guild?
                string guild = (string)ExecuteScalar("SELECT guild FROM guild_members WHERE `character`=@character", new SqlParameter("@character", player.name));
                if (guild != null) {
                    // load guild info
                    player.guildName = guild;
                    table = ExecuteReader("SELECT notice FROM guild_info WHERE name=@guild", new SqlParameter("@guild", guild));
                    if (table.Count == 1) {
                        var row = table[0];
                        player.guild.notice = (string)row[0];
                    }

                    // load members list
                    var members = new List<GuildMember>();
                    table = ExecuteReader("SELECT character, rank FROM guild_members WHERE guild=@guild", new SqlParameter("@guild", player.guildName));
                    foreach (var row in table) {
                        var member = new GuildMember();
                        member.name = (string)row[0];
                        member.rank = (GuildRank)((int)row[1]);
                        member.online = Player.onlinePlayers.ContainsKey(member.name);
                        if (member.name == player.name) {
                            member.level = player.level;
                        } else {
                            object scalar = ExecuteScalar("SELECT level FROM characters WHERE name=@character", new SqlParameter("@character", member.name));
                            member.level = scalar != null ? (int)scalar : 1;
                        }
                        members.Add(member);
                    }
                    player.guild.members = members.ToArray(); // guild.AddMember each time is too slow because array resizing
                }

                // addon system hooks
                Utils.InvokeMany(typeof(Database), null, "CharacterLoad_", player);

                return go;
            } else Debug.LogError("no prefab found for class: " + className);
        }
        return null;
    }

    // adds or overwrites character data in the database
    public static void CharacterSave(Player player, bool online, bool useTransaction = true) {
        // only use a transaction if not called within SaveMany transaction
        Transaction(command =>
        {

            // online status:
            //   '' if offline (if just logging out etc.)
            //   current time otherwise
            // -> this way it's fault tolerant because external applications can
            //    check if online != '' and if time difference < saveinterval
            // -> online time is useful for network zones (server<->server online
            //    checks), external websites which render dynamic maps, etc.
            // -> it uses the ISO 8601 standard format
            DateTime? onlineTimestamp = null;

            if (!online)
                onlineTimestamp = DateTime.Now;

            ExecuteNonQuery(command, "REPLACE INTO characters VALUES (@name, @account, @class, @x, @y, @z, @level, @health, @mana, @strength, @intelligence, @experience, @skillExperience, @gold, @coins, @online, 0)",
                            new SqlParameter("@name", player.name),
                            new SqlParameter("@account", player.account),
                            new SqlParameter("@class", player.className),
                            new SqlParameter("@x", player.transform.position.x),
                            new SqlParameter("@y", player.transform.position.y),
                            new SqlParameter("@z", player.transform.position.z),
                            new SqlParameter("@level", player.level),
                            new SqlParameter("@health", player.health),
                            new SqlParameter("@mana", player.mana),
                            new SqlParameter("@strength", player.strength),
                            new SqlParameter("@intelligence", player.intelligence),
                            new SqlParameter("@experience", player.experience),
                            new SqlParameter("@skillExperience", player.skillExperience),
                            new SqlParameter("@gold", player.gold),
                            new SqlParameter("@coins", player.coins),
                            new SqlParameter("@online", onlineTimestamp));

            // inventory: remove old entries first, then add all new ones
            // (we could use UPDATE where slot=... but deleting everything makes
            //  sure that there are never any ghosts)
            ExecuteNonQuery(command, "DELETE FROM character_inventory WHERE `character`=@character", new SqlParameter("@character", player.name));
            for (int i = 0; i < player.inventory.Count; ++i)
            {
                var item = player.inventory[i];
                ExecuteNonQuery(command, "INSERT INTO character_inventory VALUES (@character, @slot, @name, @valid, @amount, @petHealth, @petLevel, @petExperience)",
                                new SqlParameter("@character", player.name),
                                new SqlParameter("@slot", i),
                                new SqlParameter("@name", item.valid ? item.name : ""),
                                new SqlParameter("@valid", item.valid),
                                new SqlParameter("@amount", item.valid ? item.amount : 0),
                                new SqlParameter("@petHealth", item.valid ? item.petHealth : 0),
                                new SqlParameter("@petLevel", item.valid ? item.petLevel : 0),
                                new SqlParameter("@petExperience", item.valid ? item.petExperience : 0));
            }

            // equipment: remove old entries first, then add all new ones
            // (we could use UPDATE where slot=... but deleting everything makes
            //  sure that there are never any ghosts)
            ExecuteNonQuery(command, "DELETE FROM character_equipment WHERE `character`=@character", new SqlParameter("@character", player.name));
            for (int i = 0; i < player.equipment.Count; ++i)
            {
                var item = player.equipment[i];
                ExecuteNonQuery(command, "INSERT INTO character_equipment VALUES (@character, @slot, @name, @valid, @amount)",
                                new SqlParameter("@character", player.name),
                                new SqlParameter("@slot", i),
                                new SqlParameter("@name", item.valid ? item.name : ""),
                                new SqlParameter("@valid", item.valid),
                                new SqlParameter("@amount", item.valid ? item.amount : 0));
            }

            // skills: remove old entries first, then add all new ones
            ExecuteNonQuery(command, "DELETE FROM character_skills WHERE `character`=@character", new SqlParameter("@character", player.name));
            foreach (var skill in player.skills)
                if (skill.learned)
                    // castTimeEnd and cooldownEnd are based on Time.time, which
                    // will be different when restarting the server, so let's
                    // convert them to the remaining time for easier save & load
                    // note: this does NOT work when trying to save character data shortly
                    //       before closing the editor or game because Time.time is 0 then.
                    ExecuteNonQuery(command, "INSERT INTO character_skills VALUES (@character, @name, @learned, @level, @castTimeEnd, @cooldownEnd, @buffTimeEnd)",
                                    new SqlParameter("@character", player.name),
                                    new SqlParameter("@name", skill.name),
                                    new SqlParameter("@learned", skill.learned),
                                    new SqlParameter("@level", skill.level),
                                    new SqlParameter("@castTimeEnd", skill.CastTimeRemaining()),
                                    new SqlParameter("@cooldownEnd", skill.CooldownRemaining()),
                                    new SqlParameter("@buffTimeEnd", skill.BuffTimeRemaining()));

            // quests: remove old entries first, then add all new ones
            ExecuteNonQuery(command, "DELETE FROM character_quests WHERE `character`=@character", new SqlParameter("@character", player.name));
            foreach (var quest in player.quests)
                ExecuteNonQuery(command, "INSERT INTO character_quests VALUES (@character, @name, @killed, @completed)",
                                new SqlParameter("@character", player.name),
                                new SqlParameter("@name", quest.name),
                                new SqlParameter("@killed", quest.killed),
                                new SqlParameter("@completed", quest.completed));

            // addon system hooks
            Utils.InvokeMany(typeof(Database), null, "CharacterSave_", player);


        });

    }

    // save multiple characters at once (useful for ultra fast transactions)
    public static void CharacterSaveMany(List<Player> players, bool online = true) {

        foreach (var player in players) 
            CharacterSave(player, online, false);
    }

    // guilds //////////////////////////////////////////////////////////////////
    public static void SaveGuild(string guild, string notice, List<GuildMember> members) {

        Transaction(command =>
        {


            // guild info
            ExecuteNonQuery(command, "REPLACE INTO guild_info VALUES (@guild, @notice)",
                            new SqlParameter("@guild", guild),
                            new SqlParameter("@notice", notice));

            // members list
            ExecuteNonQuery(command, "DELETE FROM guild_members WHERE guild=@guild", new SqlParameter("@guild", guild));
            foreach (var member in members)
            {
                ExecuteNonQuery(command, "INSERT INTO guild_members VALUES(@guild, @character, @rank)",
                                new SqlParameter("@guild", guild),
                                new SqlParameter("@character", member.name),
                                new SqlParameter("@rank", member.rank));
            }

        });
    }

    public static bool GuildExists(string guild) {
        return ((long)ExecuteScalar("SELECT Count(*) FROM guild_info WHERE name=@name", new SqlParameter("@name", guild))) == 1;
    }

    public static void RemoveGuild(string guild) {
        Transaction(command =>
        {
            ExecuteNonQuery(command, "DELETE FROM guild_info WHERE name=@name", new SqlParameter("@name", guild));
            ExecuteNonQuery(command, "DELETE FROM guild_members WHERE guild=@guild", new SqlParameter("@guild", guild));

        });
    }

    // item mall ///////////////////////////////////////////////////////////////
    public static List<long> GrabCharacterOrders(string characterName) {
        // grab new orders from the database and delete them immediately
        //
        // note: this requires an orderid if we want someone else to write to
        // the database too. otherwise deleting would delete all the new ones or
        // updating would update all the new ones. especially in sqlite.
        //
        // note: we could just delete processed orders, but keeping them in the
        // database is easier for debugging / support.
        var result = new List<long>();
        var table = ExecuteReader("SELECT orderid, coins FROM character_orders WHERE `character`=@character AND processed=0", new SqlParameter("@character", characterName));
        foreach (var row in table) {
            result.Add((long)row[1]);
            ExecuteNonQuery("UPDATE character_orders SET processed=1 WHERE orderid=@orderid", new SqlParameter("@orderid", (long)row[0]));
        }
        return result;
    }
}
