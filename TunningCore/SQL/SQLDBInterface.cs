using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace TunningCore.SQL
{
    public class SQLDBInterface
    {
        private SqlConnection cnn;
        public SqlCommand cmm;
        private string _connectionString;

        public SQLDBInterface(DataBaseProperties database)
        {
            SqlConnectionStringBuilder _sqlCB = new SqlConnectionStringBuilder();
            _sqlCB.DataSource = database.ServerName;
            _sqlCB.InitialCatalog = database.DatabaseName;
            _sqlCB.UserID = database.UserName;
            _sqlCB.Password = database.Password;
            _sqlCB.IntegratedSecurity = false;
            _sqlCB.ApplicationName = database.ApplicationName;
            _connectionString = _sqlCB.ConnectionString;
            cnn = new SqlConnection(_connectionString);
            cmm = new SqlCommand();
            cmm.Connection = cnn;
            cmm.CommandType = CommandType.Text;
        }


        public void SetConnectionString(DataBaseProperties database)
        {
            SqlConnectionStringBuilder _sqlCB = new SqlConnectionStringBuilder();
            _sqlCB.DataSource = database.ServerName;
            _sqlCB.InitialCatalog = database.DatabaseName;
            _sqlCB.UserID = database.UserName;
            _sqlCB.Password = database.Password;
            _sqlCB.IntegratedSecurity = false;
            _sqlCB.ApplicationName = database.ApplicationName;
            _connectionString = _sqlCB.ConnectionString;
            cmm.Connection = cnn;
        }


        public void Open()
        {
            try
            {
                cmm.Connection.Open();
            }
            catch (Exception ex)
            {
                SqlConnectionStringBuilder sql = new SqlConnectionStringBuilder(_connectionString);
                throw new Exception(string.Format("Erro de acesso ao banco de dados. Servidor:{0} DataBase:{1} Message:{2}", sql.DataSource, sql.InitialCatalog, ex.Message));
            }
        }


        public void Close()
        {
            try
            {
                cmm.Connection.Close();
            }
            catch (Exception ex)
            {
            }
        }

    }
}
