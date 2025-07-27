using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using SqlTestDataGenerator.Core.Models;
using System;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class OracleConnectionTest
    {
        private readonly string _connectionString = new AppSettings().DefaultOracleConnectionString;

        [TestMethod]
        public void OracleConnection_ShouldOpenAndCloseSuccessfully()
        {
            try
            {
                using var connection = new OracleConnection(_connectionString);
                connection.Open();
                Assert.AreEqual(System.Data.ConnectionState.Open, connection.State, "Connection should be open.");
                connection.Close();
                Assert.AreEqual(System.Data.ConnectionState.Closed, connection.State, "Connection should be closed.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Oracle connection failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
} 