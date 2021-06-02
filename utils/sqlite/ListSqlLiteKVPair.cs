using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;
using PocFwIpApp.constant;


namespace PocFwIpApp.utils.sqlite
{
    class ListSqlLiteKVPair
    {
        [Flags]
        public enum AddOptions : int
        {
            DateTimeToStrDateAndTime = 1,
            DateTimeToStrDate = 2,
            TimeSpanToStrTime = 4,
            TimeSpanToStrSec = 8,
        };

        private readonly List<SqlLiteKVPair> _inList = new List<SqlLiteKVPair>();

        public static string UpdateClauseStrForOneParam(string keyA, object valA)
        {
            ListSqlLiteKVPair lst = new ListSqlLiteKVPair();
            lst.Add(keyA, valA);

            return lst.UpdateClauseStr();
        }

        public static string UpdateClauseStrForOneParam(string keyA, object valA, string keyB, object valB)
        {
            ListSqlLiteKVPair lst = new ListSqlLiteKVPair();
            lst.Add(keyA, valA);
            lst.Add(keyB, valB);

            return lst.UpdateClauseStr();
        }

        public static string UpdateClauseStrForOneParam(string keyA, object valA, string keyB, object valB, string keyC, object valC)
        {
            ListSqlLiteKVPair lst = new ListSqlLiteKVPair();
            lst.Add(keyA, valA);
            lst.Add(keyB, valB);
            lst.Add(keyC, valC);

            return lst.UpdateClauseStr();
        }





        public SqlLiteKVPair Add(string key, object value,  AddOptions options = AddOptions.DateTimeToStrDate | AddOptions.TimeSpanToStrTime)
        {
            if (options == AddOptions.DateTimeToStrDate && options == AddOptions.DateTimeToStrDateAndTime)
            {
                throw new Exception("DateTimeToStrDate et DateTimeToStrDateAndTime ne peuvent pas être précisées ensemble");
            }

            if (options == AddOptions.TimeSpanToStrSec && options == AddOptions.TimeSpanToStrTime)
            {
                throw new Exception("TimeSpanToStrSec et TimeSpanToStrTime ne peuvent pas être précisées ensemble");
            }


            SqlLiteKVPair kv = new SqlLiteKVPair();
            kv.Key = key;

            if (value == null)
            {
                kv.Value = null;
            }
            else if (value is string)
            {
                kv.Value = value;
            }
            else if (value is int)
            {
                kv.Value = value;
            }
            else if (value is DateTime)
            {
                if (options.HasFlag(AddOptions.DateTimeToStrDate))
                {
                    kv.Value = ((DateTime)value).ToString("yyyy-MM-dd");
                }
                else if (options.HasFlag(AddOptions.DateTimeToStrDateAndTime))
                {
                    kv.Value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm");

                }
            }
            else if (value is TimeSpan)
            {
                if (options.HasFlag(AddOptions.TimeSpanToStrSec))
                {
                    kv.Value = (int)((TimeSpan)value).TotalSeconds;
                }
                else if (options.HasFlag(AddOptions.TimeSpanToStrTime))
                {
                    kv.Value = ((TimeSpan)value).ToString("c");
                }
            }
            else if (value is bool)
            {
                kv.Value = (bool)value ? 1 : 0;
            }
            else
            {
                kv.Value = value.ToString();
            }

            if (Contains(key))
            {
                Remove(key);
            }



            _inList.Add(kv);
            return kv;
        }

        public bool Contains(string key)
        {
            return _inList.Any(r => r.Key.Equals(key));

        }

        public void Remove(string key)
        {
            _inList.RemoveAll(r => r.Key.Equals(key));
        }

        public string UpdateClauseStr(bool withSetPrefix = false)
        {
            StringBuilder strB = new StringBuilder(withSetPrefix ? "SET " : "");

            foreach (SqlLiteKVPair kvPair in _inList.Where(r=>r.IsUpdateField))
            {
                strB.Append(String.Format("{0}=@{0},", kvPair.Key));
            }

            return strB.ToString().TrimEnd(',');

        }

        public string InserOrderStr(string tableName)
        {
            StringBuilder strCols = new StringBuilder();
            StringBuilder strVals = new StringBuilder();

            string tpl = "{0},";

            foreach (SqlLiteKVPair kvPair in _inList)
            {
                strCols.Append(String.Format(tpl, kvPair.Key));
                strVals.Append(String.Format("@" + tpl, kvPair.Key));

            }

            return String.Format(SqlConstants.INSERT_INTO, tableName, strCols.ToString().TrimEnd(','), strVals.ToString().TrimEnd(','));

        }

        public string DeleteWhereStr(string tableName)
        {
            List<String> whereClauseList = new List<string>();
            foreach (SqlLiteKVPair kvPair in _inList)
            {
                String str = String.Format("{0}=@{0}", kvPair.Key);
                whereClauseList.Add(str);
            }


            return String.Format(SqlConstants.DELETE_WHERE, tableName,
                whereClauseList.Any() ? String.Join(" AND ", whereClauseList) : "");
        }

        public void AddSqlParams(SQLiteCommand command)
        {
            foreach (SqlLiteKVPair kvPair in _inList)
            {
                command.Parameters.Add(new SQLiteParameter("@" + kvPair.Key, kvPair.Value));

            }
        }


        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (SqlLiteKVPair kvPair in _inList)
            {
                stringBuilder.Append("[" + kvPair.Key + ":" + (kvPair.Value ?? "null") + "]");
            }

            return stringBuilder.ToString();
        }


    }
}
