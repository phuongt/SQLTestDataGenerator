using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using SqlTestDataGenerator.Core.Models;
using System.Collections.Generic;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class GoldSqlParserTests
    {
        private ISqlParser _parser;

        [TestInitialize]
        public void Setup()
        {
            // Giả lập parser GOLD cho MySQL (sau này thay bằng GoldSqlParserMySql thực tế)
            _parser = new SqlQueryParserV3(); // Tạm thời dùng parser cũ để kiểm tra pipeline
        }

        [TestMethod]
        public void Parse_SimpleSelectWithWhere_ShouldExtractWhereCondition()
        {
            string sql = "SELECT * FROM users WHERE age > 18";
            var result = _parser.ParseQuery(sql);
            Assert.AreEqual(1, result.WhereConditions.Count);
            Assert.AreEqual("age", result.WhereConditions[0].ColumnName);
            Assert.AreEqual(">", result.WhereConditions[0].Operator);
            Assert.AreEqual("18", result.WhereConditions[0].Value);
        }

        [TestMethod]
        public void Parse_JoinQuery_ShouldExtractJoinRequirement()
        {
            string sql = "SELECT u.id, r.name FROM users u JOIN roles r ON u.role_id = r.id WHERE r.name = 'admin'";
            var result = _parser.ParseQuery(sql);
            Assert.AreEqual(1, result.JoinRequirements.Count);
            Assert.AreEqual("u", result.JoinRequirements[0].LeftTableAlias);
            Assert.AreEqual("role_id", result.JoinRequirements[0].LeftColumn);
            Assert.AreEqual("r", result.JoinRequirements[0].RightTableAlias);
            Assert.AreEqual("id", result.JoinRequirements[0].RightColumn);
            Assert.AreEqual("r", result.JoinRequirements[0].RightTable);
        }

        [TestMethod]
        public void Parse_ComplexWhere_ShouldExtractAllConditions()
        {
            string sql = "SELECT * FROM orders WHERE status = 'paid' AND amount >= 100 AND created_at >= '2024-01-01'";
            var result = _parser.ParseQuery(sql);
            Assert.AreEqual(3, result.WhereConditions.Count);
            Assert.IsTrue(result.WhereConditions.Exists(w => w.ColumnName == "status" && w.Value == "paid"));
            Assert.IsTrue(result.WhereConditions.Exists(w => w.ColumnName == "amount" && w.Operator == ">=" && w.Value == "100"));
            Assert.IsTrue(result.WhereConditions.Exists(w => w.ColumnName == "created_at" && w.Operator == ">=" && w.Value == "2024-01-01"));
        }

        [TestMethod]
        public void Parse_InClause_ShouldExtractInCondition()
        {
            string sql = "SELECT * FROM products WHERE category_id IN (1,2,3)";
            var result = _parser.ParseQuery(sql);
            Assert.AreEqual(1, result.WhereConditions.Count);
            Assert.AreEqual("category_id", result.WhereConditions[0].ColumnName);
            Assert.AreEqual("IN", result.WhereConditions[0].Operator);
            Assert.AreEqual("1,2,3", result.WhereConditions[0].Value);
        }
    }
} 