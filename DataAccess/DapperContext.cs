using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BHMS_Portal.DataAccess
{
    public class DapperContext
    {
        private readonly string _connectionString;
        //public DapperContext()
        //{
        //    _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        //}
        public DapperContext()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (connectionStringSettings == null)
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            _connectionString = connectionStringSettings.ConnectionString;
        }
        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
