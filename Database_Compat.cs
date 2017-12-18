using UnityEngine;
using System.Collections;
using System.Linq;

using MySql.Data.MySqlClient;

using Mono.Data.Sqlite;
using System.Collections.Generic;


// These methods are for having backwards compatibility with sqlite
// you can delete this class if you don't have plugins that 
// use database methods
public partial class Database
{
    private static MySqlParameter[] ToMysqlParameters(SqliteParameter[] args)
    {
        return args.Select(x => new MySqlParameter(x.ParameterName, x.Value)).ToArray();
    }

    /// <summary>
    /// Backwards compatible method.
    /// Execute a statement that is not a query,  it translates sqliteparameters
    /// into mysqlparameters
    /// </summary>
    /// <param name="sql">Sql.</param>
    /// <param name="args">Arguments.</param>
    public static void ExecuteNonQuery(string sql, params SqliteParameter[] args)
    {
        ExecuteNonQueryMySql(sql, ToMysqlParameters(args));
    }

    /// <summary>
    /// Backwards compatible method.
    /// Execute a scalar query,  it translates sqliteparameters
    /// into mysqlparameters
    /// </summary>
    /// <param name="sql">Sql.</param>
    /// <param name="args">Arguments.</param>
    public static object ExecuteScalar(string sql, params SqliteParameter[] args)
    {
        return ExecuteScalarMySql(sql, ToMysqlParameters(args));
    }

    /// <summary>
    /// Backwards compatible method.
    /// Execute a query,  it translates sqliteparameters
    /// into mysqlparameters
    /// </summary>
    /// <param name="sql">Sql.</param>
    /// <param name="args">Arguments.</param>
    public static List<List<object>> ExecuteReader(string sql, params SqliteParameter[] args)
    {
        return ExecuteReaderMySql(sql, ToMysqlParameters(args));
    }

}
