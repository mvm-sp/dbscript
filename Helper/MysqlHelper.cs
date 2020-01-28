﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace NESTExportaDB
{
    internal static class MysqlHelper
    {
        public static string ConnectionString(string serverIp, int serverPort, string username, string password, string database)
        {
            string ret = "";

            //
            // http://www.connectionstrings.com/mysql/
            //
            // MySQL does not use 'Instance'
            ret += "Server=" + serverIp + "; ";
            if (serverPort > 0) ret += "Port=" + serverPort + "; ";
            ret += "Database=" + database + "; ";
            if (!String.IsNullOrEmpty(username)) ret += "Uid=" + username + "; ";
            if (!String.IsNullOrEmpty(password)) ret += "Pwd=" + password + "; ";

            return ret;
        }

        public static string LoadTableNamesQuery()
        {
            return "SHOW TABLES";
        }

        public static string LoadTableColumnsQuery(string database, string table)
        {
            return
                "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE " +
                "TABLE_NAME='" + table + "' " +
                "AND TABLE_SCHEMA='" + database + "'";
        }

        public static string SanitizeString(string val)
        {
            string ret = "";
            ret = MySqlHelper.EscapeString(val);
            return ret;
        }

        public static string SelectQuery(string tableName, int? indexStart, int? maxResults, List<string> returnFields, Expression filter, string orderByClause)
        { 
            string outerQuery = "";
            string whereClause = "";

            //
            // SELECT
            //
            outerQuery += "SELECT ";

            //
            // fields
            //
            if (returnFields == null || returnFields.Count < 1) outerQuery += "* ";
            else
            {
                int fieldsAdded = 0;
                foreach (string curr in returnFields)
                {
                    if (fieldsAdded == 0)
                    {
                        outerQuery += SanitizeString(curr);
                        fieldsAdded++;
                    }
                    else
                    {
                        outerQuery += "," + SanitizeString(curr);
                        fieldsAdded++;
                    }
                }
            }
            outerQuery += " ";

            //
            // table
            //
            outerQuery += "FROM " + tableName + " ";

            //
            // expressions
            //
            if (filter != null) whereClause = filter.ToWhereClause(DbTypes.MySql);
            if (!String.IsNullOrEmpty(whereClause))
            {
                outerQuery += "WHERE " + whereClause + " ";
            }

            // 
            // order clause
            //
            if (!String.IsNullOrEmpty(orderByClause)) outerQuery += SanitizeString(orderByClause) + " ";

            //
            // limit
            //
            if (maxResults > 0)
            {
                if (indexStart != null && indexStart >= 0)
                {
                    outerQuery += "LIMIT " + indexStart + "," + maxResults;
                }
                else
                {
                    outerQuery += "LIMIT " + maxResults;
                }
            }

            return outerQuery;
        }

        public static string InsertQuery(string tableName, string keys, string values)
        {
            string ret =
                "START TRANSACTION; " +
                "INSERT INTO " + tableName + " " +
                "(" + keys + ") " + 
                "VALUES " + 
                "(" + values + "); " + 
                "SELECT LAST_INSERT_ID() AS id; " + 
                "COMMIT; ";

            return ret;
        }

        public static string UpdateQuery(string tableName, string keyValueClause, Expression filter)
        {
            string ret =
                "UPDATE " + tableName + " SET " +
                keyValueClause + " ";

            if (filter != null) ret += "WHERE " + filter.ToWhereClause(DbTypes.MySql) + " ";
            
            return ret;
        }

        public static string DeleteQuery(string tableName, Expression filter)
        {
            string ret =
                "DELETE FROM " + tableName + " ";

            if (filter != null) ret += "WHERE " + filter.ToWhereClause(DbTypes.MySql) + " ";

            return ret;
        }
    }
}