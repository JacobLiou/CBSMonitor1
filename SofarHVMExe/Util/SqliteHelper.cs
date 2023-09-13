using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SofarHVMExe.Util
{
    public static class SqliteHelper
    {
        //连接字符串
        //private static readonly string str = ConfigurationManager.ConnectionStrings["sqliteCon"].ConnectionString;
        private static readonly string str = "Data Source=Data.db";

        /// <summary>
        /// 增删改
        /// 20180723
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">sql参数</param>
        /// <returns>受影响的行数</returns>
        public static int ExecuteNonQuery(string sql, params SqliteParameter[] param)
        {
            //try
            //{
            using (SqliteConnection con = new SqliteConnection(str))
            {
                using (SqliteCommand cmd = new SqliteCommand(sql, con))
                {
                    con.Open();
                    if (param != null)
                    {
                        cmd.Parameters.AddRange(param);
                    }

                    string sql2 = cmd.CommandText;
                    //con.Close();
                    return cmd.ExecuteNonQuery();
                }
            }
            //}
            //catch (SQLiteException se)
            //{
            //    return 0;
            //}
        }
        /// <summary>
        /// 查询
        /// 20180723
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">sql参数</param>
        /// <returns>首行首列</returns>
        public static object? ExecuteScalar(string sql, params SqliteParameter[] param)
        {
            using (SqliteConnection con = new SqliteConnection(str))
            {
                using (SqliteCommand cmd = new SqliteCommand(sql, con))
                {
                    con.Open();
                    if (param != null)
                    {
                        cmd.Parameters.AddRange(param);
                    }

                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 查询多行数据 20220719
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">sql参数</param>
        /// <returns>一个表</returns>
        public static DataTable ExecuteTable(string sql, params SqliteParameter[] param)
        {
            using (SqliteConnection con = new SqliteConnection(str))
            {
                using (SqliteCommand cmd = new SqliteCommand(sql, con))
                {
                    if (param != null)
                    {
                        cmd.Parameters.AddRange(param);
                    }
                    try
                    {
                        con.Open();
                        var dataReader = cmd.ExecuteReader();

                        return ConvertToDataTable(dataReader);
                    }
                    catch (Exception ex)
                    {
                        con.Close();
                        con.Dispose();
                        throw ex;
                    }
                }
            }
        }

        private static DataTable ConvertToDataTable(SqliteDataReader dataReader)
        {
            DataTable dt = new DataTable();
            DataTable schemaTable = dataReader.GetSchemaTable();
            try
            {
                //动态构建表，添加列
                foreach (DataRow dr in schemaTable.Rows)
                {
                    DataColumn dc = new DataColumn();
                    //设置列的数据类型
                    dc.DataType = dr[0].GetType();
                    //设置列的名称
                    dc.ColumnName = dr[0].ToString();
                    //将该列添加进构造的表中
                    dt.Columns.Add(dc);
                }
                //读取数据添加进表中
                while (dataReader.Read())
                {
                    DataRow row = dt.NewRow();
                    //填充一行数据
                    for (int i = 0; i < schemaTable.Rows.Count; i++)
                    {
                        row[i] = dataReader[i].ToString();

                    }
                    dt.Rows.Add(row);
                    row = null;
                }
                dataReader.Close();
                schemaTable = null;
                return dt;
            }
            catch (Exception ex)
            {
                //抛出异常
                throw new Exception(ex.Message);
            }

        }
        /// <summary>
        /// 数据插入
        /// 20180725
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="insertData">需要插入的数据字典</param>
        /// <returns>受影响行数</returns>
        public static int ExecuteInsert(string tbName, Dictionary<String, String> insertData)
        {
            string point = "";//分隔符号(,)
            string keyStr = "";//字段名拼接字符串
            string valueStr = "";//值的拼接字符串

            List<SqliteParameter> param = new List<SqliteParameter>();
            foreach (string key in insertData.Keys)
            {
                keyStr += string.Format("{0} `{1}`", point, key);
                valueStr += string.Format("{0} @{1}", point, key);
                param.Add(new SqliteParameter("@" + key, insertData[key]));
                point = ",";
            }
            string sql = string.Format("INSERT INTO `{0}`({1}) VALUES({2})", tbName, keyStr, valueStr);

            //return sql;
            return ExecuteNonQuery(sql, param.ToArray());

        }

        /// <summary>
        /// 执行Update语句
        /// 20180725
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <param name="where">更新条件：id=1</param>
        /// <param name="insertData">需要更新的数据</param>
        /// <returns>受影响行数</returns>
        public static int ExecuteUpdate(string tbName, string where, Dictionary<String, String> insertData)
        {
            string point = "";//分隔符号(,)
            string kvStr = "";//键值对拼接字符串(Id=@Id)

            List<SqliteParameter> param = new List<SqliteParameter>();
            foreach (string key in insertData.Keys)
            {
                kvStr += string.Format("{0} {1}=@{2}", point, key, key);
                param.Add(new SqliteParameter("@" + key, insertData[key]));
                point = ",";
            }
            string sql = string.Format("UPDATE `{0}` SET {1} WHERE {2}", tbName, kvStr, where);

            return ExecuteNonQuery(sql, param.ToArray());

        }
    }
}
