
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
/// Port of the sqlite database class from ummorpg
/// </summary>
public partial class Database
{

    private static string connectionString = null;

    /// <summary>
    /// produces the connection string based on environment variables
    /// </summary>
    /// <value>The connection string</value>
    private static string ConnectionString
    {
        get
        {

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
        using (var connection = new MySqlConnection(ConnectionString))
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
        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS guild_info(
            name VARCHAR(16) NOT NULL,
            notice TEXT NOT NULL,
            PRIMARY KEY(name)
        ) CHARACTER SET=utf8mb4");


        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS accounts (
            name VARCHAR(16) NOT NULL,
            password CHAR(40) NOT NULL,
            banned BOOLEAN NOT NULL DEFAULT 0,
            PRIMARY KEY(name)
        ) CHARACTER SET=utf8mb4");

        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS characters(
            name VARCHAR(16) NOT NULL,
            account VARCHAR(16) NOT NULL,

            class VARCHAR(16) NOT NULL,
            x FLOAT NOT NULL,
        	y FLOAT NOT NULL,
            z FLOAT NOT NULL,
        	level INT NOT NULL DEFAULT 1,
            health INT NOT NULL,
        	mana INT NOT NULL,
            strength INT NOT NULL DEFAULT 0,
        	intelligence INT NOT NULL DEFAULT 0,
            experience BIGINT NOT NULL DEFAULT 0,
        	skillExperience BIGINT NOT NULL DEFAULT 0,
            gold BIGINT NOT NULL DEFAULT 0,
        	coins BIGINT NOT NULL DEFAULT 0,
            online TIMESTAMP,

            deleted BOOLEAN NOT NULL,

            guild VARCHAR(16),
            rank INT,

        	PRIMARY KEY (name),
            INDEX(account),
            INDEX(guild),
        	FOREIGN KEY(account)
                REFERENCES accounts(name)
                ON DELETE CASCADE ON UPDATE CASCADE,
            FOREIGN KEY(guild)
                REFERENCES guild_info(name)
                ON DELETE SET NULL ON UPDATE CASCADE
        ) CHARACTER SET=utf8mb4");


        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS character_inventory(
            `character` VARCHAR(16) NOT NULL,
            slot INT NOT NULL,
        	name VARCHAR(50) NOT NULL,
            amount INT NOT NULL,
        	petHealth INT NOT NULL,
            petLevel INT NOT NULL,
            petExperience BIGINT NOT NULL,

            primary key(`character`, slot),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8mb4");

        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS character_equipment(
            `character` VARCHAR(16) NOT NULL,
            slot INT NOT NULL,
        	name VARCHAR(50) NOT NULL,
            amount INT NOT NULL,

            primary key(`character`, slot),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
         ) CHARACTER SET=utf8mb4");

        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS character_skills(
            `character` VARCHAR(16) NOT NULL,
            name VARCHAR(50) NOT NULL,
            level INT NOT NULL,
        	castTimeEnd FLOAT NOT NULL,
            cooldownEnd FLOAT NOT NULL,

            PRIMARY KEY (`character`, name),
            FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8mb4");


        ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS character_buffs (
            `character` VARCHAR(16) NOT NULL,
            name VARCHAR(50) NOT NULL,
            level INT NOT NULL,
            buffTimeEnd FLOAT NOT NULL,

            PRIMARY KEY (`character`, name),
            FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE 
        ) CHARACTER SET=utf8mb4");


        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS character_quests(
            `character` VARCHAR(16) NOT NULL,
            name VARCHAR(50) NOT NULL,
            killed INT NOT NULL,
        	completed BOOLEAN NOT NULL,

            PRIMARY KEY(`character`, name),
        	FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
        ) CHARACTER SET=utf8mb4");


        ExecuteNonQueryMySql(@"
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
        ) CHARACTER SET=utf8mb4");





    }

    static Database()
    {
        Debug.Log("Initializing database");


        InitializeSchema();

        Utils.InvokeMany(typeof(Database), null, "Initialize_");

    }

    #region Helper Functions

    // run a query that doesn't return anything
    private static void ExecuteNonQueryMySql(string sql, params SqlParameter[] args)
    {
        MySqlHelper.ExecuteNonQuery(ConnectionString, sql, args);
    }


    private static void ExecuteNonQueryMySql(MySqlCommand command, string sql, params SqlParameter[] args)
    {
        command.CommandText = sql;
        command.Parameters.Clear();

        foreach (var arg in args)
        {
            command.Parameters.Add(arg);
        }

        command.ExecuteNonQuery();
    }

    // run a query that returns a single value
    private static object ExecuteScalarMySql(string sql, params SqlParameter[] args)
    {
        return MySqlHelper.ExecuteScalar(ConnectionString, sql, args);
    }

    private static DataRow ExecuteDataRowMySql(string sql, params SqlParameter[] args)
    {
        return MySqlHelper.ExecuteDataRow(ConnectionString, sql, args);
    }

    private static DataSet ExecuteDataSetMySql(string sql, params SqlParameter[] args)
    {
        return MySqlHelper.ExecuteDataset(ConnectionString, sql, args);
    }

    // run a query that returns several values
    private static List<List<object>> ExecuteReaderMySql(string sql, params SqlParameter[] args)
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

    // run a query that returns several values
    private static MySqlDataReader GetReader(string sql, params SqlParameter[] args)
    {
        return MySqlHelper.ExecuteReader(ConnectionString, sql, args);
    }

    #endregion


    // account data ////////////////////////////////////////////////////////////
    public static bool IsValidAccount(string account, string password)
    {
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
        if (!Utils.IsNullOrWhiteSpace(account) && !Utils.IsNullOrWhiteSpace(password))
        {

            var row = ExecuteDataRowMySql("SELECT password, banned FROM accounts WHERE name=@name", new SqlParameter("@name", account));
            if (row != null)
            {
                return password == (string)row["password"] && !(bool)row["banned"];
            }
            else
            {
                // account doesn't exist. create it.
                ExecuteNonQueryMySql("INSERT INTO accounts VALUES (@name, @password, 0)", new SqlParameter("@name", account), new SqlParameter("@password", password));
                return true;

            }
        }
        return false;
    }

    // character data //////////////////////////////////////////////////////////
    public static bool CharacterExists(string characterName)
    {
        // checks deleted ones too so we don't end up with duplicates if we un-
        // delete one
        return ((long)ExecuteScalarMySql("SELECT Count(*) FROM characters WHERE name=@name", new SqlParameter("@name", characterName))) == 1;
    }

    public static void CharacterDelete(string characterName)
    {
        // soft delete the character so it can always be restored later
        ExecuteNonQueryMySql("UPDATE characters SET deleted=1 WHERE name=@character", new SqlParameter("@character", characterName));
    }

    // returns a dict of<character name, character class=prefab name>
    // we really need the prefab name too, so that client character selection
    // can read all kinds of properties like icons, stats, 3D models and not
    // just the character name
    public static List<string> CharactersForAccount(string account)
    {
        var result = new List<String>();

        var table = ExecuteReaderMySql("SELECT name FROM characters WHERE account=@account AND deleted=0", new SqlParameter("@account", account));
        foreach (var row in table)
            result.Add((string)row[0]);
        return result;
    }

    private static void LoadInventory(Player player)
    {
        // fill all slots first
        for (int i = 0; i < player.inventorySize; ++i)
            player.inventory.Add(new Item());

        // override with the inventory stored in database
        using (var reader = GetReader(@"SELECT * FROM character_inventory WHERE `character`=@character;",
                                           new SqlParameter("@character", player.name)))
        {

            while (reader.Read())
            {
                string itemName = (string)reader["name"];
                int slot = (int)reader["slot"];

                ItemTemplate template;
                if (slot < player.inventorySize && ItemTemplate.dict.TryGetValue(itemName.GetStableHashCode(), out template))
                {
                    Item item = new Item(template);
                    item.valid = true; // only valid items were saved
                    item.amount = (int)reader["amount"];
                    item.petHealth = (int)reader["petHealth"];
                    item.petLevel = (int)reader["petLevel"];
                    item.petExperience = (long)reader["petExperience"];
                    player.inventory[slot] = item;
                }
            }
        }
    }

    private static void LoadEquipment(Player player)
    {
        // fill all slots first
        for (int i = 0; i < player.equipmentInfo.Length; ++i)
            player.equipment.Add(new Item());

        using (var reader = GetReader(@"SELECT * FROM character_equipment WHERE `character`=@character;",
                                           new SqlParameter("@character", player.name)))
        {
            
            while (reader.Read())
            {
                string itemName = (string)reader["name"];
                int slot = (int)reader["slot"];

                ItemTemplate template;
                if (slot < player.equipmentInfo.Length && ItemTemplate.dict.TryGetValue(itemName.GetStableHashCode(), out template))
                {
                    Item item = new Item(template);
                    item.valid = true; // only valid items were saved
                    item.amount = (int)reader["amount"];
                    player.equipment[slot] = item;
                }
            }
        }
    }

    private static void LoadSkills(Player player)
    {
        // load skills based on skill templates (the others don't matter)
        // -> this way any template changes in a prefab will be applied
        //    to all existing players every time (unlike item templates
        //    which are only for newly created characters)

        // fill all slots first
        foreach (var template in player.skillTemplates)
            player.skills.Add(new Skill(template));

        using (var reader = GetReader(
            "SELECT name, level, castTimeEnd, cooldownEnd FROM character_skills WHERE `character`=@character ",
            new SqlParameter("@character", player.name)))
        {

            while (reader.Read())
            {

                var skillName = (string)reader["name"];

                int index = player.skills.FindIndex(skill => skill.name == skillName);
                if (index != -1)
                {
                    Skill skill = player.skills[index];
                    skill.learned = true; // only learned skills were saved
                    // make sure that 1 <= level <= maxlevel (in case we removed a skill
                    // level etc)
                    skill.level = Mathf.Clamp((int)reader["level"], 1, skill.maxLevel);
                    // make sure that 1 <= level <= maxlevel (in case we removed a skill
                    // level etc)
                    // castTimeEnd and cooldownEnd are based on Time.time, which
                    // will be different when restarting a server, hence why we
                    // saved them as just the remaining times. so let's convert them
                    // back again.
                    skill.castTimeEnd = (float)reader["castTimeEnd"] + Time.time;
                    skill.cooldownEnd = (float)reader["cooldownEnd"] + Time.time;

                    player.skills[index] = skill;
                }
            }
        }
    }

    private static void LoadBuffs(Player player)
    {

        using (var reader = GetReader(
            "SELECT name, level, buffTimeEnd FROM character_buffs WHERE `character` = @character ",
            new SqlParameter("@character", player.name)))
        {
            while (reader.Read())
            {
                Buff buff = new Buff();
                buff.name = (string)reader["name"];
                // make sure that 1 <= level <= maxlevel (in case we removed a skill
                // level etc)
                buff.level = Mathf.Clamp((int)reader["level"], 1, buff.maxLevel);
                // buffTimeEnd is based on Time.time, which will be
                // different when restarting a server, hence why we saved
                // them as just the remaining times. so let's convert them
                // back again.
                buff.buffTimeEnd = (float)reader["buffTimeEnd"] + NetworkTime.time;
                if (buff.TemplateExists()) player.buffs.Add(buff);

            }

        }
    }

    private static void LoadQuests(Player player)
    {
        // load quests

        using (var reader = GetReader("SELECT name, killed, completed FROM character_quests WHERE `character`=@character",
                                           new SqlParameter("@character", player.name)))
        {

            while (reader.Read())
            {
                string questName = (string)reader["name"];
                QuestTemplate template;
                if (QuestTemplate.dict.TryGetValue(questName.GetStableHashCode(), out template))
                {
                    Quest quest = new Quest(template);
                    quest.killed = (int)reader["killed"];
                    quest.completed = (bool)reader["completed"];
                    player.quests.Add(quest);
                }
            }
        }
    }

    private static void LoadGuild(Player player)
    {
        // in a guild?
        if (player.guildName != "")
        {
            // load guild info
            var row = ExecuteDataRowMySql("SELECT notice FROM guild_info WHERE name=@guild", new SqlParameter("@guild", player.guildName));
            if (row != null)
            {
                player.guild.notice = (string)row["notice"];
            }

            // load members list
            var members = new List<GuildMember>();

            using (var reader = GetReader(
                "SELECT name, level, rank FROM characters WHERE guild=@guild AND deleted=0",
                new SqlParameter("@guild", player.guildName)))
            {

                while (reader.Read())
                {
                    var member = new GuildMember();
                    member.name = (string)reader["name"];
                    member.rank = (GuildRank)((int)reader["rank"]);
                    member.online = Player.onlinePlayers.ContainsKey(member.name);
                    member.level = (int)reader["level"];

                    members.Add(member);
                };
            }
            player.guild.members = members.ToArray(); // guild.AddMember each time is too slow because array resizing
        }
    }

    public static GameObject CharacterLoad(string characterName, List<Player> prefabs)
    {
        var row = ExecuteDataRowMySql("SELECT * FROM characters WHERE name=@name AND deleted=0", new SqlParameter("@name", characterName));
        if (row != null)
        {
            // instantiate based on the class name
            string className = (string)row["class"];
            var prefab = prefabs.Find(p => p.name == className);
            if (prefab != null)
            {
                var go = GameObject.Instantiate(prefab.gameObject);
                var player = go.GetComponent<Player>();

                player.name = (string)row["name"];
                player.account = (string)row["account"];
                player.className = (string)row["class"];
                float x = (float)row["x"];
                float y = (float)row["y"];
                float z = (float)row["z"];
                Vector3 position = new Vector3(x, y, z);
                player.level = (int)row["level"];
                int health = (int)row["health"];
                int mana = (int)row["mana"];
                player.strength = (int)row["strength"];
                player.intelligence = (int)row["intelligence"];
                player.experience = (long)row["experience"];
                player.skillExperience = (long)row["skillExperience"];
                player.gold = (long)row["gold"];
                player.coins = (long)row["coins"];

                if (row.IsNull("guild"))
                    player.guildName = "";
                else
                    player.guildName = (string)row["guild"];

                // try to warp to loaded position.
                // => agent.warp is recommended over transform.position and
                //    avoids all kinds of weird bugs
                // => warping might fail if we changed the world since last save
                //    so we reset to start position if not on navmesh
                player.agent.Warp(position);
                if (!player.agent.isOnNavMesh)
                {
                    Transform start = NetworkManager.singleton.GetNearestStartPosition(position);
                    player.agent.Warp(start.position);
                    Debug.Log(player.name + " invalid position was reset");
                }

                LoadInventory(player);
                LoadEquipment(player);
                LoadSkills(player);
                LoadBuffs(player);
                LoadQuests(player);
                LoadGuild(player);

                // assign health / mana after max values were fully loaded
                // (they depend on equipment, buffs, etc.)
                player.health = health;
                player.mana = mana;

                // addon system hooks
                Utils.InvokeMany(typeof(Database), null, "CharacterLoad_", player);

                return go;
            }
            else Debug.LogError("no prefab found for class: " + className);
        }
        return null;
    }

    private static void SaveInventory(Player player, MySqlCommand command)
    {
        // inventory: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQueryMySql(command, "DELETE FROM character_inventory WHERE `character`=@character", new SqlParameter("@character", player.name));
        for (int i = 0; i < player.inventory.Count; ++i)
        {
            var item = player.inventory[i];
            if (item.valid) // only relevant items to save queries/storage/time
                ExecuteNonQueryMySql(command, "INSERT INTO character_inventory VALUES (@character, @slot, @name, @amount, @petHealth, @petLevel, @petExperience)",
                        new SqlParameter("@character", player.name),
                        new SqlParameter("@slot", i),
                        new SqlParameter("@name",  item.name),
                        new SqlParameter("@amount",  item.amount),
                        new SqlParameter("@petHealth", item.petHealth),
                        new SqlParameter("@petLevel",  item.petLevel),
                        new SqlParameter("@petExperience", item.petExperience));
        }
    }

    private static void SaveEquipment(Player player, MySqlCommand command)
    {
        // equipment: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQueryMySql(command, "DELETE FROM character_equipment WHERE `character`=@character", new SqlParameter("@character", player.name));
        for (int i = 0; i < player.equipment.Count; ++i)
        {
            var item = player.equipment[i];
            if (item.valid) // only relevant equip to save queries/storage/time
                ExecuteNonQueryMySql(command, "INSERT INTO character_equipment VALUES (@character, @slot, @name, @amount)",
                            new SqlParameter("@character", player.name),
                            new SqlParameter("@slot", i),
                            new SqlParameter("@name", item.name),
                            new SqlParameter("@amount", item.amount));
        }
    }

    private static void SaveSkills(Player player, MySqlCommand command)
    {
        // skills: remove old entries first, then add all new ones
        ExecuteNonQueryMySql(command, "DELETE FROM character_skills WHERE `character`=@character", new SqlParameter("@character", player.name));
        foreach (var skill in player.skills)
        {
            // only save relevant skills to save a lot of queries and storage
            // (considering thousands of players)
            // => interesting only if learned or if buff/status (murderer etc.)
            if (skill.learned) // only relevant skills to save queries/storage/time
            {
                // castTimeEnd and cooldownEnd are based on Time.time, which
                // will be different when restarting the server, so let's
                // convert them to the remaining time for easier save & load
                // note: this does NOT work when trying to save character data shortly
                //       before closing the editor or game because Time.time is 0 then.
                ExecuteNonQueryMySql(command, @"
                    INSERT INTO character_skills 
                    SET
                        `character` = @character,
                        name = @name,
                        level = @level,
                        castTimeEnd = @castTimeEnd,
                        cooldownEnd = @cooldownEnd",
                                    new SqlParameter("@character", player.name),
                                    new SqlParameter("@name", skill.name),
                                     new SqlParameter("@level", skill.level),
                                    new SqlParameter("@castTimeEnd", skill.CastTimeRemaining()),
                                    new SqlParameter("@cooldownEnd", skill.CooldownRemaining()));
            }
        }
    }

    private static void SaveBuffs(Player player, MySqlCommand command)
    {
        ExecuteNonQueryMySql(command, "DELETE FROM character_buffs WHERE `character`=@character", new SqlParameter("@character", player.name));
        foreach (var buff in player.buffs)
        {
            // buffTimeEnd is based on Time.time, which will be different when
            // restarting the server, so let's convert them to the remaining
            // time for easier save & load
            // note: this does NOT work when trying to save character data shortly
            //       before closing the editor or game because Time.time is 0 then.
            ExecuteNonQueryMySql(command, "INSERT INTO character_buffs VALUES (@character, @name, @level, @buffTimeEnd)",
                            new SqlParameter("@character", player.name),
                            new SqlParameter("@name", buff.name),
                                 new SqlParameter("@level", buff.level),
                            new SqlParameter("@buffTimeEnd", (float)buff.BuffTimeRemaining()));
        }
    }

    private static void SaveQuests(Player player, MySqlCommand command)
    {
        // quests: remove old entries first, then add all new ones
        ExecuteNonQueryMySql(command, "DELETE FROM character_quests WHERE `character`=@character", new SqlParameter("@character", player.name));
        foreach (var quest in player.quests)
        {
            ExecuteNonQueryMySql(command, "INSERT INTO character_quests VALUES (@character, @name, @killed, @completed)",
                            new SqlParameter("@character", player.name),
                            new SqlParameter("@name", quest.name),
                            new SqlParameter("@killed", quest.killed),
                            new SqlParameter("@completed", quest.completed));
        }
    }

    // adds or overwrites character data in the database
    public static void CharacterSave(Player player, bool online, bool useTransaction = true)
    {
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

            var query = @"
            INSERT INTO characters 
            SET
                name=@name,
                account=@account,
                class = @class,
                x = @x,
                y = @y,
                z = @z,
                level = @level,
                health = @health,
                mana = @mana,
                strength = @strength,
                intelligence = @intelligence,
                experience = @experience,
                skillExperience = @skillExperience,
                gold = @gold,
                coins = @coins,
                online = @online,
                deleted = 0,
                guild = @guild
            ON DUPLICATE KEY UPDATE 
                account=@account,
                class = @class,
                x = @x,
                y = @y,
                z = @z,
                level = @level,
                health = @health,
                mana = @mana,
                strength = @strength,
                intelligence = @intelligence,
                experience = @experience,
                skillExperience = @skillExperience,
                gold = @gold,
                coins = @coins,
                online = @online,
                deleted = 0,
                guild = @guild
            ";

            ExecuteNonQueryMySql(command, query,
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
                        new SqlParameter("@online", onlineTimestamp),
                        new SqlParameter("@guild", player.guildName == "" ? null : player.guildName)
                           );

            SaveInventory(player, command);
            SaveEquipment(player, command);
            SaveSkills(player, command);
            SaveBuffs(player, command);
            SaveQuests(player, command);


            // addon system hooks
            Utils.InvokeMany(typeof(Database), null, "CharacterSave_", player);


        });

    }






    // save multiple characters at once (useful for ultra fast transactions)
    public static void CharacterSaveMany(List<Player> players, bool online = true)
    {

        foreach (var player in players)
            CharacterSave(player, online, false);
    }

    // guilds //////////////////////////////////////////////////////////////////
    public static void SaveGuild(string guild, string notice, List<GuildMember> members)
    {

        Transaction(command =>
        {

            var query = @"
            INSERT INTO guild_info
            SET
                name = @guild,
                notice = @notice
            ON DUPLICATE KEY UPDATE
                notice = @notice";

                // guild info
                ExecuteNonQueryMySql(command, query,
                                    new SqlParameter("@guild", guild),
                                    new SqlParameter("@notice", notice));

            ExecuteNonQueryMySql(command, "UPDATE characters set guild = NULL where guild=@guild", new SqlParameter("@guild", guild));


            foreach (var member in members)
            {

                Debug.Log("Saving guild " + guild + " member " + member.name);
                ExecuteNonQueryMySql(command, "UPDATE characters set guild = @guild, rank=@rank where name=@character",
                                new SqlParameter("@guild", guild),
                                new SqlParameter("@character", member.name),
                                new SqlParameter("@rank", member.rank));
            }

        });
    }

    public static bool GuildExists(string guild)
    {
        return ((long)ExecuteScalarMySql("SELECT Count(*) FROM guild_info WHERE name=@name", new SqlParameter("@name", guild))) == 1;
    }

    public static void RemoveGuild(string guild)
    {
        ExecuteNonQueryMySql("DELETE FROM guild_info WHERE name=@name", new SqlParameter("@name", guild));
    }

    // item mall ///////////////////////////////////////////////////////////////
    public static List<long> GrabCharacterOrders(string characterName)
    {
        // grab new orders from the database and delete them immediately
        //
        // note: this requires an orderid if we want someone else to write to
        // the database too. otherwise deleting would delete all the new ones or
        // updating would update all the new ones. especially in sqlite.
        //
        // note: we could just delete processed orders, but keeping them in the
        // database is easier for debugging / support.
        var result = new List<long>();
        var table = ExecuteReaderMySql("SELECT orderid, coins FROM character_orders WHERE `character`=@character AND processed=0", new SqlParameter("@character", characterName));
        foreach (var row in table)
        {
            result.Add((long)row[1]);
            ExecuteNonQueryMySql("UPDATE character_orders SET processed=1 WHERE orderid=@orderid", new SqlParameter("@orderid", (long)row[0]));
        }
        return result;
    }
}
