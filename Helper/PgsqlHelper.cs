using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESTExportaDB
{
	internal static class PgsqlHelper
	{
		public static string ConnectionString(string serverIp, int serverPort, string username, string password, string database)
		{
			string ret = "";

			//
			// http://www.connectionstrings.com/postgresql/
			//
			// PgSQL does not use 'Instance'
			ret += "Server=" + serverIp + "; ";
			if (serverPort > 0) ret += "Port=" + serverPort + "; ";
			ret += "Database=" + database + "; ";
			if (!String.IsNullOrEmpty(username)) ret += "User ID=" + username + "; ";
			if (!String.IsNullOrEmpty(password)) ret += "Password=" + password + "; ";
			ret += "CommandTimeout=240;";
			return ret;
		}

		public static string LoadTableNamesQuery()
		{
			return "SELECT * FROM pg_catalog.pg_tables WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema'  order by schemaname, tablename";
		}

		public static string DisableConstraintsQuery(string tableName)
		{
			return "ALTER TABLE " + tableName + " DISABLE TRIGGER ALL;";
		}

		public static string EnableConstraintsQuery(string tableName)
		{
			return "ALTER TABLE " + tableName + " ENABLE TRIGGER ALL;";
		}

		public static string LoadTableColumnsQuery(string database, string table)
		{
			return "select case c.relkind when 'r' then 'table' when 'v' then 'view' end::varchar " +
					", table_name::varchar " +
					", column_name::varchar " +
					", IS_NULLABLE as is_nullable " +
					", 	column_default" +
					", udt_name::varchar " +
					",format_type(a.atttypid, a.atttypmod)::varchar as data_type" +
					", case when pk.indisprimary then 'YES' else 'NO' end as is_primary_key" +
					", case when lower(data_type) = 'array' then information_schema._pg_char_max_length(arraytype.oid, a.atttypmod) else character_maximum_length end::int as max_len " +
					", ordinal_position::int " +
					" from \"" + database + "\".information_schema.columns " +
					" inner join pg_catalog.pg_attribute a on a.attname = column_name inner " +
					" join pg_catalog.pg_class c on c.oid = a.attrelid and c.relname = table_name " +
					" left join ( select pa.attname, i.indrelid, indisprimary " +
					"             from pg_index i " +
					"             inner join pg_attribute pa on pa.attrelid = i.indrelid " +
					"             and pa.attnum = any(i.indkey) " +
					"             where i.indisprimary) " +
					" pk on pk.indrelid = a.attrelid and pk.attname = a.attname " +
					" left join pg_catalog.pg_type arraytype " +
					" on arraytype.typname = right(udt_name, -1) " +
					" inner join pg_type t on a.atttypid = t.oid " +
					" where table_schema = 'public' " +
					" and c.relkind = 'r' " +
					" and table_name = '" + table + "'" +
					" order by table_schema, table_name, ordinal_position;";
			/*
			return
					"SELECT  " +
					"  cols.COLUMN_NAME AS column_name, " +
					"  cols.IS_NULLABLE AS is_nullable, " +
					"  cols.DATA_TYPE AS data_type, " +
					"  cols.CHARACTER_MAXIMUM_LENGTH AS max_len, " +
					"  CASE " +
					"    WHEN cons.COLUMN_NAME IS NULL THEN 'NO' ELSE 'YES' " +
					"  END AS is_primary_key " +
					"FROM \"" + database + "\".INFORMATION_SCHEMA.COLUMNS cols " +
					"LEFT JOIN \"" + database + "\".INFORMATION_SCHEMA.KEY_COLUMN_USAGE cons " +
					"ON cols.COLUMN_NAME = cons.COLUMN_NAME and cons.TABLE_NAME = cols.TABLE_NAME" +
					"WHERE cols.TABLE_NAME = '" + table + "' order by is_primary_key desc;";
			*/
		}

		public static string LoadConstraintQuery(string database, string table)
		{
			return "SELECT DISTINCT tc.table_schema " +
					" , tc.constraint_name " +
					" , tc.table_name " +
					" , kcu.column_name " +
					" , case when tc.constraint_type::VARCHAR = 'PRIMARY KEY' THEN  '' ELSE ccu.table_schema  END AS foreign_table_schema " +
					" , case when tc.constraint_type::VARCHAR = 'PRIMARY KEY' THEN  '' ELSE ccu.table_name END AS  foreign_table_name" +
					" , case when tc.constraint_type::VARCHAR = 'PRIMARY KEY' THEN  '' ELSE ccu.column_name END AS  foreign_column_name  " +
					" , tc.constraint_type  " +
					" FROM \"" + database + "\".information_schema.table_constraints AS tc  " +
					" JOIN \"" + database + "\".information_schema.key_column_usage AS kcu  " +
					" ON tc.constraint_name = kcu.constraint_name  " +
					" AND tc.table_schema = kcu.table_schema  " +
					" JOIN \"" + database + "\".information_schema.constraint_column_usage AS ccu  " +
					" ON ccu.constraint_name = tc.constraint_name  " +
					" AND ccu.table_schema = tc.table_schema  " +
					" WHERE tc.table_name='" + table + "'  " +
					" order by constraint_type desc,  tc.table_schema ,	tc.constraint_name ,kcu.column_name ;";
		}
		
	  public static string LoadFunctionsDefinitionQuery()
		{
			return "SELECT n.nspname::varchar AS schema_name " +
					",proname::varchar AS fname " +
					",p.oid::bigint as Id " +
					",pg_get_function_arguments(p.oid)::varchar AS IN_args " +
					",p.proargnames::varchar as All_args " +
					",t.typname::varchar AS return_type " +
					",CASE WHEN p.pronargs = 0 " +
					"		THEN CAST('*' AS pg_catalog.text) " +
					"	 ELSE(pg_catalog.array_to_string(ARRAY( " +
					"SELECT  " +
					"	pg_catalog.format_type(p.proallargtypes[s.i], NULL) " +
					"FROM " +
					"	pg_catalog.generate_series(0, pg_catalog.array_upper(p.proallargtypes, 1)) AS s(i) " +
					"), ', ') " +
				 ") " +
				 "end::varchar AS ALL_Args_Types " +
				 " ,format('%I.%I(%s)', n.nspname, p.proname, oidvectortypes(p.proargtypes))::varchar as call_type " +
				 " ,d.description::varchar " +
				 " ,p.prosrc::varchar as body " +
				 " ,pg_get_functiondef(p.oid)::varchar as definition " +
			"FROM pg_proc p " +
			"JOIN pg_type t " +
			"	ON p.prorettype = t.oid " +
			"LEFT OUTER " +
			"JOIN pg_description d " +
			"	ON p.oid = d.objoid " +
			"LEFT OUTER " +
			"JOIN pg_namespace n " +
			"	ON n.oid = p.pronamespace " +
		 "where NOT p.proisagg " +
		 "	 AND n.nspname = 'public' " +
		 " order by proname;";
		}

		public static string LoadTempTableDependency()
		{
			return "CREATE TEMPORARY TABLE TT_AllTables AS  " +
			"SELECT " +
			"	 ist.table_schema AS OnTableSchema " +
			"	,ist.table_name AS OnTableName " +
			"	,tForeignKeyInformation.FkNullable " +
			"	,CAST(DENSE_RANK() OVER(ORDER BY ist.table_schema, ist.table_name) AS varchar(20)) AS OnTableId " +
			" , tForeignKeyInformation.AgainstTableSchema AS AgainstTableSchema " +
			"	,tForeignKeyInformation.AgainstTableName AS AgainstTableName " +
			"FROM INFORMATION_SCHEMA.TABLES AS ist " +
			"LEFT JOIN " +
			"(" +
			"		SELECT " +
			" 	 KCU1.table_schema AS OnTableSchema " +
			"	, KCU1.table_name AS OnTableName " +
			"	, MIN(isc.IS_NULLABLE) AS FkNullable " +
			"	, KCU2.table_schema AS AgainstTableSchema " +
			"	, KCU2.table_name AS AgainstTableName " +
			"		FROM information_schema.referential_constraints AS RC " +
			"		INNER JOIN information_schema.key_column_usage AS KCU1 " +
			"				ON KCU1.constraint_catalog = RC.constraint_catalog " +
			"				AND KCU1.constraint_schema = RC.constraint_schema " +
			"				AND KCU1.constraint_name = RC.constraint_name " +
			"		INNER JOIN information_schema.key_column_usage AS KCU2 " +
			"			ON KCU2.constraint_catalog = RC.constraint_catalog " +
			"				AND KCU2.constraint_schema = RC.unique_constraint_schema " +
			"				AND KCU2.constraint_name = RC.unique_constraint_name " +
			"				AND KCU2.ordinal_position = KCU1.ordinal_position " +
			"		INNER JOIN INFORMATION_SCHEMA.COLUMNS AS isc " +
			"				ON isc.table_name = KCU1.table_name " +
			"				AND isc.table_schema = KCU1.table_schema " +
			"				AND isc.table_catalog = KCU1.table_catalog " +
			"				AND isc.column_name = KCU1.column_name " +
			"	WHERE KCU1.table_name <> KCU2.table_name " +
			"	GROUP BY " +
			"		 KCU1.table_schema " +
			"		, KCU1.table_name " +
			"		, KCU2.table_schema " +
			"		, KCU2.table_name " +
			") AS tForeignKeyInformation " +
			"	ON tForeignKeyInformation.OnTableName = ist.table_name " +
			"	AND tForeignKeyInformation.OnTableSchema = ist.table_schema " +
			"WHERE(1 = 1) " +
			"AND ist.table_type = 'BASE TABLE' " +
			"AND ist.table_schema NOT IN('pg_catalog', 'information_schema') " +
			"ORDER BY OnTableSchema, OnTableName " +
			"; ";
		}

		public static string LoadTableDependencies()
		{
			return "; WITH RECURSIVE CTE_RecursiveDependencyResolution AS  " +
					"(" +
					"	SELECT " +
					"		 OnTableSchema " +
					"		, OnTableName " +
					"		, FkNullable " +
					"		, AgainstTableSchema " +
					"		, AgainstTableName " +
					"		, CONCAT(N';', OnTableSchema, N'.', OnTableName, N';') AS PathName " +
					"		, CONCAT(';', OnTableId, ';') AS Path " +
					"		, 0 AS lvl " +
					"	FROM TT_AllTables " +
					"	WHERE(1 = 1) " +
					"	AND AgainstTableName IS NULL " +
					"	UNION ALL " +
					"	SELECT " +
					"		 TT_AllTables.OnTableSchema " +
					"		, TT_AllTables.OnTableName " +
					"		, TT_AllTables.FkNullable " +
					"		, TT_AllTables.AgainstTableSchema " +
					"		, TT_AllTables.AgainstTableName " +
					"		, CONCAT(CTE_RecursiveDependencyResolution.PathName, TT_AllTables.OnTableSchema, N'.', TT_AllTables.OnTableName, N';') AS PathName " +
					"		, CONCAT(CTE_RecursiveDependencyResolution.Path, TT_AllTables.OnTableId, N';') AS Path " +
					"		, CTE_RecursiveDependencyResolution.Lvl + 1 AS Lvl " +
					"	FROM CTE_RecursiveDependencyResolution " +
					"	INNER JOIN TT_AllTables " +
					"		ON TT_AllTables.AgainstTableName = CTE_RecursiveDependencyResolution.OnTableName " +
					"		AND TT_AllTables.AgainstTableSchema = CTE_RecursiveDependencyResolution.OnTableSchema " +
					"		AND CTE_RecursiveDependencyResolution.Path NOT LIKE '%;' || TT_AllTables.OnTableId || ';%' " +
					") " +
					"SELECT " +
					"	 MAX(lvl) AS Level " +
					"	, OnTableSchema " +
					"	, OnTableName " +
					"	, MIN(FkNullable) AS FkNullable " +
					"FROM CTE_RecursiveDependencyResolution " +
					"GROUP BY OnTableSchema, OnTableName " +
					"ORDER BY " +
					"	 Level, " +
					"	 OnTableSchema " +
					"	, OnTableName " +
					"	, FkNullable " +
					";  ";
		}

		public static string SanitizeString(string val)
		{
			string tag = "$" + EscapeString(val, 2) + "$";
			return tag + val + tag;
		}

		private static string EscapeString(string val, int numChar)
		{
			string ret = "";
			Random random = new Random();
			if (numChar < 1) return ret;

			while (true)
			{
				ret = "";
				random = new Random();

				int valid = 0;
				int num = 0;

				for (int i = 0; i < numChar; i++)
				{
					num = 0;
					valid = 0;
					while (valid == 0)
					{
						num = random.Next(126);
						if (((num > 64) && (num < 91)) ||
								((num > 96) && (num < 123)))
						{
							valid = 1;
						}
					}
					ret += (char)num;
				}

				if (!val.Contains("$" + ret + "$")) break;
			}

			return ret;
		}

		private static string SanitizeFieldname(string val)
		{
			string ret = "";

			//
			// null, below ASCII range, above ASCII range
			//
			for (int i = 0; i < val.Length; i++)
			{
				if (((int)(val[i]) == 10) ||      // Preserve carriage return
						((int)(val[i]) == 13))        // and line feed
				{
					ret += val[i];
				}
				else if ((int)(val[i]) < 32)
				{
					continue;
				}
				else
				{
					ret += val[i];
				}
			}

			//
			// double dash
			//
			int doubleDash = 0;
			while (true)
			{
				doubleDash = ret.IndexOf("--");
				if (doubleDash < 0)
				{
					break;
				}
				else
				{
					ret = ret.Remove(doubleDash, 2);
				}
			}

			//
			// open comment
			// 
			int openComment = 0;
			while (true)
			{
				openComment = ret.IndexOf("/*");
				if (openComment < 0) break;
				else
				{
					ret = ret.Remove(openComment, 2);
				}
			}

			//
			// close comment
			//
			int closeComment = 0;
			while (true)
			{
				closeComment = ret.IndexOf("*/");
				if (closeComment < 0) break;
				else
				{
					ret = ret.Remove(closeComment, 2);
				}
			}

			//
			// in-string replacement
			//
			ret = ret.Replace("'", "''");
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
						outerQuery += "\"" + SanitizeFieldname(curr) + "\"";
						fieldsAdded++;
					}
					else
					{
						outerQuery += ",\"" + SanitizeFieldname(curr) + "\"";
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
			if (filter != null) whereClause = filter.ToWhereClause(DbTypes.PgSql);
			if (!String.IsNullOrEmpty(whereClause))
			{
				outerQuery += "WHERE " + whereClause + " ";
			}

			// 
			// order clause
			//
			if (!String.IsNullOrEmpty(orderByClause)) outerQuery += PreparedOrderByClause(orderByClause) + " ";

			//
			// limit
			//
			if (maxResults > 0)
			{
				if (indexStart != null && indexStart >= 0)
				{
					outerQuery += "OFFSET " + indexStart + " LIMIT " + maxResults;
				}
				else
				{
					outerQuery += "LIMIT " + maxResults;
				}
			}

			return outerQuery;
		}

		private static string PreparedOrderByClause(string val)
		{
			string ret = "";

			//
			// null, below ASCII range, above ASCII range
			//
			for (int i = 0; i < val.Length; i++)
			{
				if (((int)(val[i]) == 10) ||      // Preserve carriage return
						((int)(val[i]) == 13))        // and line feed
				{
					ret += val[i];
				}
				else if ((int)(val[i]) < 32)
				{
					continue;
				}
				else
				{
					ret += val[i];
				}
			}

			//
			// double dash
			//
			int doubleDash = 0;
			while (true)
			{
				doubleDash = ret.IndexOf("--");
				if (doubleDash < 0)
				{
					break;
				}
				else
				{
					ret = ret.Remove(doubleDash, 2);
				}
			}

			//
			// open comment
			// 
			int openComment = 0;
			while (true)
			{
				openComment = ret.IndexOf("/*");
				if (openComment < 0) break;
				else
				{
					ret = ret.Remove(openComment, 2);
				}
			}

			//
			// close comment
			//
			int closeComment = 0;
			while (true)
			{
				closeComment = ret.IndexOf("*/");
				if (closeComment < 0) break;
				else
				{
					ret = ret.Remove(closeComment, 2);
				}
			}

			//
			// in-string replacement
			//
			ret = ret.Replace("'", "''");
			return ret;
		}

		public static string InsertQuery(string tableName, string keys, string values)
		{
			string ret =
					"INSERT INTO " + tableName + " " +
					"(" + keys + ") " +
					"VALUES " +
					"(" + values + ") " +
					"RETURNING *;";
			return ret;
		}

		public static string UpdateQuery(string tableName, string keyValueClause, Expression filter)
		{
			string ret =
					"UPDATE " + tableName + " SET " +
					keyValueClause + " ";

			if (filter != null) ret += "WHERE " + filter.ToWhereClause(DbTypes.PgSql) + " ";
			ret += "RETURNING *";

			return ret;
		}

		public static string DeleteQuery(string tableName, Expression filter)
		{
			string ret =
					"DELETE FROM " + tableName + " ";

			if (filter != null) ret += "WHERE " + filter.ToWhereClause(DbTypes.PgSql) + " ";

			return ret;
		}
	}
}
