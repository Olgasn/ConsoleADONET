using System;
using System.Collections;
using ConsoleADONET.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleADONET.Tests
{
    /// <summary>
    /// Интеграционные тесты CRUD-примеров на базе SqlDataAdapter (<see cref="DataAdapterExamples"/>).
    /// </summary>
    [TestClass]
    public class DataAdapterExamplesTests : IntegrationTestBase
    {
        [ClassInitialize]
        public static void SeedDatabase(TestContext _)
        {
            if (!TestDatabase.IsAvailable)
                return;

            // Достаточно операций, чтобы DemoDataRelation (TOP 40 операций / TOP 4 топлива)
            // гарантированно укладывался в загруженные родительские строки.
            TestDatabase.Seed(tanksCount: 75, fuelsCount: 75, operationsCount: 2000);
        }

        private void InsertTank(string tankType, string material)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) " +
                    "VALUES (@t, 100, 50, @m);";
                cmd.Parameters.AddWithValue("@t", tankType);
                cmd.Parameters.AddWithValue("@m", material);
                cmd.ExecuteNonQuery();
            }
        }

        [TestCleanup]
        public void RemoveTestRows()
        {
            if (Connection == null)
                return;

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM Tanks WHERE TankType IN (N'Фляга_DA', N'Цистерна_СП'); " +
                    "DELETE FROM Fuels WHERE FuelType = N'ТестCB';";
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
        public void SelectViaDataAdapter_ReturnsUpToFiveRows()
        {
            IList rows = DataAdapterExamples.SelectViaDataAdapter(Connection);

            Assert.IsNotNull(rows);
            Assert.IsTrue(rows.Count <= 5, "Запрос ограничен TOP 5.");
            Assert.IsTrue(rows.Count > 0, "На заполненной базе должны быть операции.");
        }

        [TestMethod]
        public void SelectJoinViaDataAdapter_ReturnsRows()
        {
            IList rows = DataAdapterExamples.SelectJoinViaDataAdapter(Connection);

            Assert.IsNotNull(rows);
            // LEFT JOIN по FuelId <= 5: количество зависит от данных, но строки быть должны.
            Assert.IsTrue(rows.Count > 0);
        }

        // ── INSERT через адаптер ──────────────────────────────────────────

        [TestMethod]
        public void InsertViaDataAdapter_InsertsOneRow()
        {
            string result = DataAdapterExamples.InsertViaDataAdapter(Connection);

            StringAssert.Contains(result, "Вставлено: 1");
            Assert.AreEqual(1, CountTanks("Фляга_DA"));
        }

        // ── UPDATE / DELETE через адаптер ─────────────────────────────────

        [TestMethod]
        public void UpdateViaDataAdapter_UpdatesMatchingRow()
        {
            InsertTank("Цистерна_СП", "Алюминий");

            string result = DataAdapterExamples.UpdateViaDataAdapter(Connection);

            StringAssert.Contains(result, "Обновлено: 1");
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT TankMaterial FROM Tanks WHERE TankType = N'Цистерна_СП';";
                Assert.AreEqual("Золото", (string)cmd.ExecuteScalar());
            }
        }

        [TestMethod]
        public void DeleteViaDataAdapter_DeletesMatchingRows()
        {
            InsertTank("Цистерна_СП", "Алюминий");
            InsertTank("Фляга_DA", "ПЭТ");

            string result = DataAdapterExamples.DeleteViaDataAdapter(Connection);

            StringAssert.Contains(result, "Удалено: 2");
            Assert.AreEqual(0, CountTanks("Цистерна_СП"));
            Assert.AreEqual(0, CountTanks("Фляга_DA"));
        }

        // ── SqlCommandBuilder и DataRelation ──────────────────────────────

        [TestMethod]
        public void DemoSqlCommandBuilder_PerformsInsertUpdateDelete()
        {
            string result = DataAdapterExamples.DemoSqlCommandBuilder(Connection);

            // Автогенерация команд: вставка/обновление/удаление одной тестовой строки топлива.
            StringAssert.Contains(result, "INSERT=1");
            StringAssert.Contains(result, "DELETE=1");
            // После демонстрации тестовая строка топлива удалена.
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Fuels WHERE FuelType = N'ТестCB';";
                Assert.AreEqual(0, Convert.ToInt32(cmd.ExecuteScalar()));
            }
        }

        [TestMethod]
        public void DemoDataRelation_RunsWithoutError()
        {
            // Метод печатает навигацию по DataRelation в консоль; проверяем отсутствие исключений.
            DataAdapterExamples.DemoDataRelation(Connection);
        }
    }
}
