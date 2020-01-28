﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NESTExportaDB
{
    public class Helper
    {
        /// <summary>
        /// Determines if an object is a list.
        /// </summary>
        /// <param name="o">An object.</param>
        /// <returns>Boolean indicating if the object is a list.</returns>
        public static bool IsList(object o)
        {
            if (o == null) return false;
            return o is IList &&
                   o.GetType().IsGenericType &&
                   o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// Converts an object to a List object.
        /// </summary>
        /// <param name="o">An object.</param>
        /// <returns>A List object.</returns>
        public static List<object> ObjectToList(object o)
        {
            if (o == null) return null;
            List<object> ret = new List<object>();
            var enumerator = ((IEnumerable)o).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ret.Add(enumerator.Current);
            }
            return ret;
        }

        /// <summary>
        /// Determines if a DataTable is null or empty.
        /// </summary>
        /// <param name="t">A DataTable.</param>
        /// <returns>Boolean indicating if the DataTable is null or empty.</returns>
        public static bool DataTableIsNullOrEmpty(DataTable t)
        {
            if (t == null) return true;
            if (t.Rows.Count < 1) return true;
            return false;
        }

        /// <summary>
        /// Converts a DataTable to an object of a given type.
        /// </summary>
        /// <typeparam name="T">The type of object to which the DataTable should be converted.</typeparam>
        /// <param name="t">A DataTable.</param>
        /// <returns>An object of type T containing values from the DataTable.</returns>
        public static T DataTableToObject<T>(DataTable t) where T : new()
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            if (t.Rows.Count < 1) throw new ArgumentException("No rows in DataTable");
            foreach (DataRow r in t.Rows)
            {
                return DataRowToObject<T>(r);
            }
            return default(T);
        }
         
        /// <summary>
        /// Convert a DataRow to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of object to which the DataRow should be converted.</typeparam>
        /// <param name="r">A DataRow.</param>
        /// <returns>An object of type T containing values from the DataRow.</returns>
        public static T DataRowToObject<T>(DataRow r) where T : new()
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            T item = new T();
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            foreach (var property in properties)
            {
                property.SetValue(item, r[property.Name], null);
            }
            return item;
        }

        /// <summary>
        /// Converts a DataTable to a List of dynamic objects.
        /// </summary>
        /// <param name="dt">DataTable.</param>
        /// <returns>A List of dynamic objects.</returns>
        public static List<dynamic> DataTableToListDynamic(DataTable dt)
        {
            List<dynamic> ret = new List<dynamic>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

        /// <summary>
        /// Converts a DataTable to a dynamic, assuming the DataTable has a single row.
        /// </summary>
        /// <param name="dt">DataTable.</param>
        /// <returns>A dynamic object.</returns>
        public static dynamic DataTableToDynamic(DataTable dt)
        {
            dynamic ret = new ExpandoObject();
            if (dt == null || dt.Rows.Count < 1) return ret;
            if (dt.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Converts a DataTable to a List of Dictionary objects with key of type string and value of type object.
        /// </summary>
        /// <param name="dt">DataTable.</param>
        /// <returns>List of Dictionary objects.</returns>
        public static List<Dictionary<string, object>> DataTableToListDictionary(DataTable dt)
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                Dictionary<string, object> currDict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    currDict.Add(col.ColumnName, curr[col]);
                }

                ret.Add(currDict);
            }

            return ret;
        }

        /// <summary>
        /// Converts a DataTable to a Dictionary with key of type string and value of type object, assuming the DataTable has a single row.
        /// </summary>
        /// <param name="dt">DataTable.</param>
        /// <returns>A Dictionary object.</returns>
        public static Dictionary<string, object> DataTableToDictionary(DataTable dt)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (dt == null || dt.Rows.Count < 1) return ret;
            if (dt.Rows.Count != 1) throw new ArgumentException("DataTable must contain only one row.");

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    ret.Add(col.ColumnName, curr[col]);
                }

                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Deserialize a JSON string to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of object to which the JSON should be deserialized.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>An object of type T built using data from the JSON string.</returns>
        public static T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Deserialize a byte array containing JSON to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of object to which the JSON should be deserialized.</typeparam>
        /// <param name="bytes">The byte array containing JSON data.</param>
        /// <returns>An object of type T built using data from the JSON byte data.</returns>
        public static T DeserializeJson<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 1) throw new ArgumentNullException(nameof(bytes));
            return DeserializeJson<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Serialize an object to a JSON string.
        /// </summary>
        /// <param name="obj">An object.</param>
        /// <param name="pretty">Enable pretty printing.</param>
        /// <returns>A string containing JSON built from the supplied object.</returns>
        public static string SerializeJson(object obj, bool pretty)
        {
            if (obj == null) return null;
            string json;

            if (pretty)
            {
                json = JsonConvert.SerializeObject(
                  obj,
                  Newtonsoft.Json.Formatting.Indented,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                  });
            }
            else
            {
                json = JsonConvert.SerializeObject(obj,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc
                  });
            }

            return json;
        }

        /// <summary>
        /// Check to see if extended characters are in use in a string.
        /// </summary>
        /// <param name="data">The string to evaluate.</param>
        /// <returns>A Boolean indicating whether or not extended characters were detected.</returns>
        public static bool IsExtendedCharacters(string data)
        {
            if (String.IsNullOrEmpty(data)) return false;
            foreach (char c in data)
            {
                if ((int)c > 128) return true;
            }
            return false;
        }
    }
}
