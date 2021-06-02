using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AryxDevLibrary.utils;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.utils.sqlite;

namespace PocFwIpApp.business
{
    public class SQLiteConfManager
    {
        public string DbFile { get; }

        private readonly String TableRulesFw = "RulesFw";


        public SQLiteConfManager(string dbFile)
        {
            DbFile = dbFile;

            if (!File.Exists(DbFile))
            {
                SQLiteUtils.CreateDb(DbFile);
            }

            SQLiteUtils.InitAndGetConnection(DbFile);
        }


        public bool IsExistProcessFileFwRule(ProcessFileFwRule p)
        {
            return IsExistProcessFileFwRule(p.RuleName, p.DirectionProtocol.Direction, p.DirectionProtocol.Protocol);
        }

        public bool IsExistProcessFileFwRule(String name, DirectionsEnum direction, ProtocoleEnum protocole)
        {
 
            ListSqlLiteKVPair lstUpd = new ListSqlLiteKVPair();

            lstUpd.Add("RuleName", name);
            lstUpd.Add("Direction", (int)direction);
            lstUpd.Add("Protocole", (int)protocole);

            string sql = String.Format(SqlConstants.SELECT_COUNT_ALL_WHERE, TableRulesFw, "RuleName = @RuleName AND Direction = @Direction AND Protocole = @Protocole");

            SQLiteCommand command = new SQLiteCommand(sql, SQLiteUtils.GetConnection());
            lstUpd.AddSqlParams(command);

            return (long)command.ExecuteScalar() >= 1;

        }


        internal bool AddProcessFileFwRule(ProcessFileFwRule p)
        {
            ListSqlLiteKVPair lstUpd = new ListSqlLiteKVPair();
            lstUpd.Add("RuleName", p.RuleName);
            lstUpd.Add("Direction", (int)p.DirectionProtocol.Direction);
            lstUpd.Add("Protocole", (int)p.DirectionProtocol.Protocol);
            lstUpd.Add("IsModeManuel", p.IsModeManuel);
            lstUpd.Add("IsEnableOnlyFileName", p.IsEnableOnlyFileName);
            lstUpd.Add("FilePath", p.FilePath.FullName);
            lstUpd.Add("DateCreation", DateTime.Now, ListSqlLiteKVPair.AddOptions.DateTimeToStrDateAndTime);
            lstUpd.Add("DateLastUpdate", DateTime.Now, ListSqlLiteKVPair.AddOptions.DateTimeToStrDateAndTime);

            string sql = lstUpd.InserOrderStr(TableRulesFw);

            SQLiteCommand command = new SQLiteCommand(sql, SQLiteUtils.GetConnection());

            lstUpd.AddSqlParams(command);

            return command.ExecuteNonQuery() == 1;
        }

        public bool UpdProcessFileFwRule(ProcessFileFwRule p)
        {
            SQLiteCommand command = null;

            ListSqlLiteKVPair lstUpd = new ListSqlLiteKVPair();
            lstUpd.Add("RuleName", p.RuleName);
            lstUpd.Add("Direction", (int)p.DirectionProtocol.Direction);
            lstUpd.Add("Protocole", (int)p.DirectionProtocol.Protocol);
            
            SqlLiteKVPair kv = lstUpd.Add("IsModeManuel", p.IsModeManuel);
            kv.IsUpdateField = true;
            
            kv = lstUpd.Add("IsEnableOnlyFileName", p.IsEnableOnlyFileName);
            kv.IsUpdateField = true;

            kv = lstUpd.Add("FilePath", p.FilePath.FullName);
            kv.IsUpdateField = true;

            kv = lstUpd.Add("DateLastUpdate", DateTime.Now, ListSqlLiteKVPair.AddOptions.DateTimeToStrDateAndTime);
            kv.IsUpdateField = true;


            string sql = String.Format(SqlConstants.UPDATE_WHERE, TableRulesFw, lstUpd.UpdateClauseStr(), "RuleName = @RuleName AND Direction = @Direction AND Protocole = @Protocole");

            command = new SQLiteCommand(sql, SQLiteUtils.GetConnection());

            lstUpd.AddSqlParams(command);

            return command.ExecuteNonQuery() == 1;
        }


        public List<ProcessFileFwRule> ReadRules()
        {
            SQLiteCommand command = null;

            List<ProcessFileFwRule> retList = new List<ProcessFileFwRule>();

            string sql = String.Format(SqlConstants.SELECT_ALL, TableRulesFw);

            command = new SQLiteCommand(sql, SQLiteUtils.GetConnection());
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    ProcessFileFwRule p = new ProcessFileFwRule();
                    p.RuleName = reader.GetStringByColName("RuleName");
                    p.DirectionProtocol = new DirectionProtocolDto();
                    p.DirectionProtocol.Direction = (DirectionsEnum) reader.GetInt32ByColName("Direction");
                    p.DirectionProtocol.Protocol = (ProtocoleEnum) reader.GetInt32ByColName("Protocole");
                    p.IsModeManuel = reader.GetBooleanByColName("IsModeManuel");
                    p.IsEnableOnlyFileName = reader.GetBooleanByColName("IsEnableOnlyFileName");
                    p.FilePath = new FileInfo(reader.GetStringByColName("FilePath"));
                    p.DateCreation = reader.GetDatetimeByColName("DateCreation");
                    p.DateLastUpdate = reader.GetDatetimeByColName("DateLastUpdate");

                    retList.Add(p);
                }
            }
            return retList;
        }


        public void Save()
        {
            //throw new NotImplementedException();
        }
    }
}
