using System;
using System.Collections;
using Microsoft.Data.SqlClient;
using ConsoleADONET.Data;
using ConsoleADONET.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleADONET.Tests
{
    /// <summary>
    /// Интеграционные тесты CRUD-примеров на базе SqlCommand (<see cref="CommandExamples"/>).
    /// Перед классом база приводится к известному заполнению.
    /// </summary>
    [TestClass]
    public class CommandExamplesTests : IntegrationTestBase
    {
        [ClassInitialize]
        public static void SeedDatabase(TestContext _)
        {
            // ClassInitialize выполняется до проверки доступности в TestInitialize —
            // поэтому при недоступной базе просто выходим, тесты пометятся Inconclusive.
            if (!TestDatabase.IsAvailable)
                return;

            TestDatabase.Seed(tanksCount: 60, fuelsCount: 40, operationsCount: 400);
        }

        [TestCleanup]
        public void RemoveTestRows()
        {
            if (Connection == null)
                return;

            // Примеры Insert/Update создают строки с известными маркерами — убираем их,
            // чтобы тесты не зависели от порядка выполнения.
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM Tanks WHERE TankType IN (N'Бак_Тест', N'Цистерна_СП', N'Фляга_DA');";
                cmd.ExecuteNonQuery();
            }
        }

        private int CountTanks(string tankType)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Tanks WHERE TankType = @t;";
                cmd.Parameters.AddWithValue("@t", tankType);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // ── SELECT ────────────────────────────────────────────────────────

        [TestMethod]
        public void SelectViaCommand_ReturnsUpToFiveTanks()
        {
            IList tanks = CommandExamples.SelectViaCommand(Connection);

            Assert.IsNotNull(tanks);
            Assert.IsTrue(tanks.Count <= 5, "Запрос ограничен TOP 5.");
            Assert.IsTrue(tanks.Count > 0, "На заполненной базе должны быть ёмкости.");
            CollectionAssert.AllItemsAreInstancesOfType((ICollection)tanks, typeof(Tank));
        }

        [TestMethod]
        public void SelectViaStoredProcedure_DoesNotThrowAndReturnsList()
        {
            IList rows = CommandExamples.SelectViaStoredProcedure(Connection);

            Assert.IsNotNull(rows);
            Assert.IsTrue(rows.Count <= 5);
        }

        [TestMethod]
        public void SelectJoinViaCommand_ReturnsList()
        {
            IList rows = CommandExamples.SelectJoinViaCommand(Connection);

            Assert.IsNotNull(rows);
            Assert.IsTrue(rows.Count <= 7, "Запрос ограничен TOP 7.");
        }

        [TestMethod]
        public void DemoExecuteScalar_RunsWithoutError()
        {
            // Метод печатает агрегаты в консоль; проверяем, что выполняется без исключений.
            CommandExamples.DemoExecuteScalar(Connection);
        }

        // ── INSERT / UPDATE / DELETE ──────────────────────────────────────

        [TestMethod]
        public void InsertViaCommand_InsertsOneRow()
        {
            string result = CommandExamples.InsertViaCommand(Connection);

            StringAssert.Contains(result, "Вставлено записей: 1");
            Assert.AreEqual(1, CountTanks("Бак_Тест"));
        }

        [TestMethod]
        public void InsertViaStoredProcedure_InsertsRow()
        {
            string result = CommandExamples.InsertViaStoredProcedure(Connection);

            StringAssert.Contains(result, "добавлена");
            Assert.AreEqual(1, CountTanks("Цистерна_СП"));
        }

        [TestMethod]
        public void UpdateViaCommand_UpdatesInsertedRow()
        {
            CommandExamples.InsertViaCommand(Connection);

            string result = CommandExamples.UpdateViaCommand(Connection);

            StringAssert.Contains(result, "Обновлено записей: 1");
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT TankMaterial FROM Tanks WHERE TankType = N'Бак_Тест';";
                Assert.AreEqual("Платина", (string)cmd.ExecuteScalar());
            }
        }

        [TestMethod]
        public void DeleteViaCommand_RemovesInsertedRow()
        {
            CommandExamples.InsertViaCommand(Connection);
            Assert.AreEqual(1, CountTanks("Бак_Тест"));

            string result = CommandExamples.DeleteViaCommand(Connection);

            StringAssert.Contains(result, "Удалено записей: 1");
            Assert.AreEqual(0, CountTanks("Бак_Тест"));
        }
    }
}
