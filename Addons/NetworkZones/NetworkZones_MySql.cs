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

    static void Initialize_Zone()
    {
        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS character_scene (
            `character` VARCHAR(16) NOT NULL, 
            scenepath VARCHAR(255) NOT NULL,
            PRIMARY KEY(`character`),
            FOREIGN KEY(`character`)
                REFERENCES characters(name)
                ON DELETE CASCADE ON UPDATE CASCADE
            ) CHARACTER SET=utf8mb4");


        ExecuteNonQueryMySql(@"
        CREATE TABLE IF NOT EXISTS zones_online (
            id INT NOT NULL AUTO_INCREMENT,
            PRIMARY KEY(id),
            online TIMESTAMP NOT NULL
        ) CHARACTER SET=utf8mb4");
    }


    public static bool IsCharacterOnlineAnywhere(string characterName)
    {
        var obj = ExecuteScalarMySql("SELECT online FROM characters WHERE name=@name", new SqlParameter("@name", characterName));
        if (obj != null)
        {
            var time = (DateTime)obj;
            double elapsedSeconds = (DateTime.Now - time).TotalSeconds;
            float saveInterval = ((NetworkManagerMMO)NetworkManager.singleton).saveInterval;
            //UnityEngine.Debug.Log("datetime=" + time + " elapsed=" + elapsedSeconds + " saveinterval=" + saveInterval);
            return elapsedSeconds < saveInterval * 2;
        }
        return false;
    }

    public static bool AnyAccountCharacterOnline(string account)
    {
        List<string> characters = CharactersForAccount(account);
        return characters.Any(IsCharacterOnlineAnywhere);
    }

    public static string GetCharacterScenePath(string characterName)
    {
        object obj = ExecuteScalarMySql("SELECT scenepath FROM character_scene WHERE `character`=@character", new SqlParameter("@character", characterName));
        return obj != null ? (string)obj : "";
    }

    public static void SaveCharacterScenePath(string characterName, string scenePath)
    {
        ExecuteNonQueryMySql("REPLACE INTO character_scene VALUE (@character,@scenepath)", new SqlParameter("@character", characterName), new SqlParameter("@scenepath", scenePath));
    }

    // a zone is online if the online string is not empty and if the time
    // difference is less than the write interval * multiplier
    // (* multiplier to have some tolerance)
    public static double TimeElapsedSinceMainZoneOnline()
    {
        var obj = ExecuteScalarMySql("SELECT online FROM zones_online");
        if (obj != null)
        {
            var time = (DateTime)obj;
            return (DateTime.Now - time).TotalSeconds;
        }
        return Mathf.Infinity;
    }

    // should only be called by main zone
    public static void SaveMainZoneOnlineTime()
    {
        // online status:
        //   '' if offline (if just logging out etc.)
        //   current time otherwise
        // -> it uses the ISO 8601 standard format
        var online = DateTime.Now;
        ExecuteNonQueryMySql("REPLACE INTO zones_online VALUE (@id, @online)", new SqlParameter("@id", 1), new SqlParameter("@online", online));
    }
}
