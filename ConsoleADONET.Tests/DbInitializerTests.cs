using System;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleADONET.Tests
{
    /// <summary>
    /// Интеграционные тесты заполнения базы данных <see cref="DbInitializer"/>.
    /// Эти тесты ловят регрессию a8acfbe: TRUNCATE по таблицам Fuels/Tanks,
    /// на которые ссылается внешний ключ из Operations, приводил к ошибке SQL 4712.
    /// </summary>
    [TestClass]
    public class DbInitializerTests : IntegrationTestBase
    {
        private static int Count(SqlConnection connection, string table)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void ClearAll()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Operations; DELETE FROM Fuels; DELETE FROM Tanks;";
                cmd.ExecuteNonQuery();
            }
        }

        [TestMethod]
        public void Initialize_OnEmptyDatabase_SeedsRequestedCounts()
        {
            ClearAll();

            // Ключевой сценарий: пустая база → путь с очисткой и повторным заполнением.
            // До исправления здесь вылетала ошибка 4712 (TRUNCATE по FK-таблице).
            DbInitializer.Initialize(TestDatabase.ConnectionString,
                tanksCount: 10, fuelsCount: 8, operationsCount: 40);

            Assert.AreEqual(10, Count(Connection, "Tanks"), "Ожидалось 10 ёмкостей.");
            Assert.AreEqual(8, Count(Connection, "Fuels"), "Ожидалось 8 видов топлива.");
            Assert.AreEqual(40, Count(Connection, "Operations"), "Ожидалось 40 операций.");
        }

        [TestMethod]
        public void Initialize_ParentIdsAreContiguous()
        {
            ClearAll();
            DbInitializer.Initialize(TestDatabase.ConnectionString,
                tanksCount: 5, fuelsCount: 5, operationsCount: 10);

            // ID не обязаны начинаться с 1 (повторное заполнение продолжает IDENTITY),
            // но должны быть непрерывным диапазоном из ровно count значений.
            AssertContiguous("Tanks", "TankId", 5);
            AssertContiguous("Fuels", "FuelId", 5);
        }

        private void AssertContiguous(string table, string idColumn, int expectedCount)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText =
                    $"SELECT COUNT(*), MIN({idColumn}), MAX({idColumn}) FROM {table};";
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    int count = r.GetInt32(0);
                    int min = r.GetInt32(1);
                    int max = r.GetInt32(2);
                    Assert.AreEqual(expectedCount, count, $"{table}: неверное число строк.");
                    Assert.AreEqual(expectedCount, max - min + 1, $"{table}: ID должны быть непрерывными.");
                }
            }
        }

        [TestMethod]
        public void Initialize_ForeignKeysAreConsistent()
        {
            ClearAll();
            DbInitializer.Initialize(TestDatabase.ConnectionString,
                tanksCount: 6, fuelsCount: 6, operationsCount: 30);

            // Ни одна операция не должна ссылаться на несуществующие Tank/Fuel.
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT COUNT(*) FROM Operations o " +
                    "WHERE NOT EXISTS (SELECT 1 FROM Tanks t WHERE t.TankId = o.TankId) " +
                    "   OR NOT EXISTS (SELECT 1 FROM Fuels f WHERE f.FuelId = o.FuelId);";
                Assert.AreEqual(0, Convert.ToInt32(cmd.ExecuteScalar()),
                    "Все FK-ссылки операций должны быть валидны.");
            }
        }

        [TestMethod]
        public void Initialize_WhenAlreadyPopulated_IsIdempotent()
        {
            ClearAll();
            DbInitializer.Initialize(TestDatabase.ConnectionString,
                tanksCount: 7, fuelsCount: 7, operationsCount: 20);

            int tanksBefore = Count(Connection, "Tanks");
            int opsBefore = Count(Connection, "Operations");

            // Повторный вызов с другими количествами не должен ничего менять:
            // если все таблицы заполнены, Initialize выходит сразу (AllTablesPopulated).
            DbInitializer.Initialize(TestDatabase.ConnectionString,
                tanksCount: 999, fuelsCount: 999, operationsCount: 999);

            Assert.AreEqual(tanksBefore, Count(Connection, "Tanks"));
            Assert.AreEqual(opsBefore, Count(Connection, "Operations"));
        }

        [TestMethod]
        public void Initialize_IsDeterministic_SameSeedSameData()
        {
            ClearAll();
            DbInitializer.Initialize(TestDatabase.ConnectionString, 5, 5, 10);
            string firstFuel = FirstFuelType();

            ClearAll();
            DbInitializer.Initialize(TestDatabase.ConnectionString, 5, 5, 10);
            string secondFuel = FirstFuelType();

            // DbInitializer использует Random(1) → данные воспроизводимы.
            Assert.AreEqual(firstFuel, secondFuel);
        }

        private string FirstFuelType()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 FuelType FROM Fuels ORDER BY FuelId;";
                return (string)cmd.ExecuteScalar();
            }
        }
    }
}
