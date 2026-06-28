using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace ConsoleADONET
{
    public static class DbInitializer
    {
        public static void Initialize(string connectionString,
            int tanksCount = 100, int fuelsCount = 100, int operationsCount = 10000)
        {
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            // Если хотя бы одна таблица пуста — очищаем все три и заполняем заново,
            // чтобы FK-ссылки оставались консистентными.
            if (AllTablesPopulated(connection))
                return;

            using SqlTransaction transaction = connection.BeginTransaction();
            using SqlCommand cmd = connection.CreateCommand();
            cmd.Transaction = transaction;

            try
            {
                // Очищаем таблицы через DELETE в порядке «дочерняя → родительские».
                //
                // Затем сбрасываем IDENTITY, чтобы ID снова начинались с 1 (на это рассчитаны
                // демо-запросы вида WHERE FuelId <= 5). RESEED делаем только если в таблицу уже
                // вставляли (last_value IS NOT NULL) — тогда следующая запись получит 0 + 1 = 1.
                // На новой таблице last_value = NULL: DBCC RESEED, 0 дал бы первую запись с ID = 0,
                // поэтому его пропускаем — новый IDENTITY и так стартует с 1.
                cmd.CommandText =
                    "DELETE FROM Operations; DELETE FROM Fuels; DELETE FROM Tanks; " +
                    "IF (SELECT last_value FROM sys.identity_columns WHERE object_id = OBJECT_ID('Operations')) IS NOT NULL DBCC CHECKIDENT('Operations', RESEED, 0); " +
                    "IF (SELECT last_value FROM sys.identity_columns WHERE object_id = OBJECT_ID('Fuels'))      IS NOT NULL DBCC CHECKIDENT('Fuels', RESEED, 0); " +
                    "IF (SELECT last_value FROM sys.identity_columns WHERE object_id = OBJECT_ID('Tanks'))      IS NOT NULL DBCC CHECKIDENT('Tanks', RESEED, 0);";
                cmd.ExecuteNonQuery();

                var rng     = new Random(1);
                var culture = CultureInfo.InvariantCulture;
                var today   = DateTime.Now.Date;

                // ── Tanks ─────────────────────────────────────────────────────
                string[] tankVoc = [
                    "Цистерна_", "Ведро_", "Бак_", "Фляга_", "Стакан_",
                    "Резервуар_", "Канистра_", "Бочка_", "Бидон_", "Баллон_",
                    "Контейнер_", "Колба_", "Танкер_", "Отстойник_", "Накопитель_"
                ];
                string[] materialVoc = [
                    "Сталь", "Платина", "Алюминий", "ПЭТ", "Чугун",
                    "Золото", "Дерево", "Керамика", "Стекло", "Медь",
                    "Титан", "Нержавейка", "Бронза", "Полипропилен", "Фибергласс"
                ];

                var sb = new StringBuilder("INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) VALUES ");
                for (int i = 1; i <= tanksCount; i++)
                {
                    string type     = "N'" + tankVoc[rng.Next(tankVoc.Length)] + i + "'";
                    string material = "N'" + materialVoc[rng.Next(materialVoc.Length)] + "'";
                    float  weight   = 500 * (float)rng.NextDouble();
                    float  volume   = 200 * (float)rng.NextDouble();
                    sb.Append($"({type}, {weight.ToString("G", culture)}, {volume.ToString("G", culture)}, {material}), ");
                }
                cmd.CommandText = sb.ToString().TrimEnd(',', ' ') + ";";
                cmd.ExecuteNonQuery();

                // ── Fuels ─────────────────────────────────────────────────────
                string[] fuelVoc = [
                    "Нефть_", "Бензин_", "Керосин_", "Мазут_", "Спирт_", "Водород_",
                    "Дизель_", "Пропан_", "Бутан_", "Метан_", "Этанол_",
                    "Авиатопливо_", "Лигроин_", "Биодизель_", "Синтетическое_"
                ];

                sb.Clear();
                sb.Append("INSERT INTO Fuels (FuelType, FuelDensity) VALUES ");
                for (int i = 1; i <= fuelsCount; i++)
                {
                    string type    = "N'" + fuelVoc[rng.Next(fuelVoc.Length)] + i + "'";
                    float  density = 2 * (float)rng.NextDouble();
                    sb.Append($"({type}, {density.ToString("G", culture)}), ");
                }
                cmd.CommandText = sb.ToString().TrimEnd(',', ' ') + ";";
                cmd.ExecuteNonQuery();

                // ── Реальные диапазоны ID родительских таблиц ─────────────────
                // IDENTITY мог начаться не с 1 (повторное заполнение после DELETE), поэтому
                // берём фактические границы, а не предполагаем 1..count. Иначе Operations
                // сослались бы на несуществующие Tank/Fuel и нарушили внешний ключ.
                cmd.CommandText =
                    "SELECT MIN(TankId), MAX(TankId) FROM Tanks; SELECT MIN(FuelId), MAX(FuelId) FROM Fuels;";
                int minTank, maxTank, minFuel, maxFuel;
                using (SqlDataReader idReader = cmd.ExecuteReader())
                {
                    idReader.Read();
                    minTank = idReader.GetInt32(0);
                    maxTank = idReader.GetInt32(1);
                    idReader.NextResult();
                    idReader.Read();
                    minFuel = idReader.GetInt32(0);
                    maxFuel = idReader.GetInt32(1);
                }

                // ── Operations (batch INSERT по 1000 строк) ───────────────────
                // SQL Server допускает не более 1000 строк в одном VALUES.
                const int batchSize = 1000;
                var rows = new List<string>(batchSize);
                for (int i = 1; i <= operationsCount; i++)
                {
                    // Next(min, max+1) — диапазон ровно по реальным ID родителей (max включительно).
                    int      tankId = rng.Next(minTank, maxTank + 1);
                    int      fuelId = rng.Next(minFuel, maxFuel + 1);
                    int      incExp = rng.Next(200) - 100;
                    DateTime opDate = today.AddDays(-i);
                    rows.Add($"({tankId}, {fuelId}, {incExp.ToString("G", culture)}, '{opDate.ToString(culture)}')");

                    if (rows.Count == batchSize || i == operationsCount)
                    {
                        cmd.CommandText =
                            "INSERT INTO Operations (TankId, FuelId, Inc_Exp, Date) VALUES " +
                            string.Join(", ", rows) + ";";
                        cmd.ExecuteNonQuery();
                        rows.Clear();
                    }
                }

                transaction.Commit();
                Console.WriteLine($"База данных инициализирована: {tanksCount} ёмкостей, {fuelsCount} видов топлива, {operationsCount} операций.");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static bool AllTablesPopulated(SqlConnection connection)
        {
            using SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT (SELECT COUNT(*) FROM Fuels), " +
                       "(SELECT COUNT(*) FROM Tanks), " +
                       "(SELECT COUNT(*) FROM Operations);";
            using SqlDataReader r = cmd.ExecuteReader();
            r.Read();
            return (int)r[0] > 0 && (int)r[1] > 0 && (int)r[2] > 0;
        }
    }
}
