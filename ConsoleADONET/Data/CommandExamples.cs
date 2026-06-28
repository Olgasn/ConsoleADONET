using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using ConsoleADONET.Models;

namespace ConsoleADONET.Data
{
    /// <summary>
    /// Примеры CRUD-операций с прямым использованием SqlCommand.
    ///
    /// Три формы выполнения команды:
    ///   ExecuteReader  — построчное чтение результирующего набора (SELECT)
    ///   ExecuteNonQuery — команды без возвращаемых строк (INSERT/UPDATE/DELETE)
    ///   ExecuteScalar   — возврат одного значения (COUNT, MAX, MIN …)
    /// </summary>
    public static class CommandExamples
    {
        // ── SQL-запросы вынесены в константы ─────────────────────────────
        // Изменение схемы требует правки в одном месте, а не поиска по коду.
        private const string SqlSelectTanks =
            "SELECT TOP 5 TankId, TankType, TankVolume, TankWeight, TankMaterial FROM Tanks;";

        // INNER JOIN трёх таблиц с параметром фильтрации
        private const string SqlSelectInnerJoin =
            "SELECT TOP 7" +
            "    o.OperationId, f.FuelType, f.FuelDensity," +
            "    t.TankType, t.TankMaterial, o.Inc_Exp, o.Date " +
            "FROM Operations o " +
            "INNER JOIN Fuels f ON o.FuelId = f.FuelId " +
            "INNER JOIN Tanks t ON o.TankId = t.TankId " +
            "WHERE t.TankMaterial = @Material " +
            "ORDER BY o.Date DESC;";

        private const string SqlInsertTank =
            "INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) " +
            "VALUES (@Type, @Weight, @Volume, @Material);";

        private const string SqlUpdateTank =
            "UPDATE Tanks SET TankMaterial = @Material WHERE TankType = @Type;";

        private const string SqlDeleteTank =
            "DELETE FROM Tanks WHERE TankType = @Type;";

        // ================================================================
        // ВЫБОРКА (SELECT)
        // ================================================================

        /// <summary>
        /// SELECT через SqlCommand + SqlDataReader.
        /// SqlDataReader читает данные потоково (forward-only, read-only) —
        /// минимальное потребление памяти, максимальная скорость.
        /// Результат маппируется в объекты модели Tank вместо сырых строк.
        /// </summary>
        public static IList SelectViaCommand(SqlConnection connection)
        {
            var tanks = new List<Tank>();

            SqlCommand cmd = new SqlCommand(SqlSelectTanks, connection);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                Console.WriteLine(
                    $"{"TankId",-8}{"TankType",-20}{"Volume",-12}{"Weight",-12}{"Material"}");

                while (reader.Read())
                {
                    // Маппинг строки результата в объект модели.
                    // GetInt32 / GetString / GetFloat — типобезопасные методы,
                    // предпочтительнее GetValue(i) с последующим приведением типа.
                    tanks.Add(new Tank
                    {
                        TankId       = reader.GetInt32(0),
                        TankType     = reader.GetString(1),
                        TankVolume   = reader.GetFloat(2),
                        TankWeight   = reader.GetFloat(3),
                        TankMaterial = reader.GetString(4)
                    });
                }
            }
            return tanks;   // List<Tank> реализует IList; Print() вызовет Tank.ToString()
        }

        /// <summary>
        /// SELECT через SqlCommand с CommandType.StoredProcedure.
        /// Демонстрирует передачу именованных параметров в хранимую процедуру.
        /// </summary>
        public static IList SelectViaStoredProcedure(SqlConnection connection)
        {
            var results = new List<string>();

            SqlCommand cmd = new SqlCommand("uspGetOperations", connection)
            {
                CommandType = CommandType.StoredProcedure  // без этого SQL Server ищет таблицу с таким именем
            };

            // Значение < 0 означает «без фильтра» (см. WHERE @FuelId < 0 OR ... в процедуре)
            cmd.Parameters.AddWithValue("@FuelId",   -1);
            cmd.Parameters.AddWithValue("@FuelType", "Бензин_"); // LIKE 'Бензин_%'
            cmd.Parameters.AddWithValue("@TankId",   -1);
            cmd.Parameters.AddWithValue("@TankType", "");         // пустая строка = без фильтра

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                Console.WriteLine(
                    $"{"OperationId",-14}{"FuelType",-20}{"TankType",-18}{"Inc_Exp",-10}{"Date"}");

                int count = 0;
                while (reader.Read() && count++ < 5)
                    results.Add(
                        $"{reader["OperationId"],-14}{reader["FuelType"],-20}" +
                        $"{reader["TankType"],-18}{reader["Inc_Exp"],-10}{reader["Date"]:d}");
            }
            return results;
        }

        /// <summary>
        /// SELECT с INNER JOIN трёх таблиц через SqlCommand + SqlDataReader.
        ///
        /// INNER JOIN возвращает только строки, у которых есть совпадение во всех трёх таблицах.
        /// Строки без пары (нет связанного топлива или ёмкости) в результат не попадают.
        ///
        /// Параметр @Material фильтрует по материалу ёмкости — демонстрирует
        /// передачу параметров в запрос с JOIN так же, как и в обычный SELECT.
        /// </summary>
        public static IList SelectJoinViaCommand(SqlConnection connection)
        {
            var results = new List<string>();

            SqlCommand cmd = new SqlCommand(SqlSelectInnerJoin, connection);
            cmd.Parameters.Add("@Material", SqlDbType.NVarChar, 20).Value = "Сталь";

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                // Имена столбцов берём из метаданных ридера — нет риска опечатки
                Console.WriteLine(
                    $"{"OperationId",-14}{"FuelType",-16}{"Density",-10}" +
                    $"{"TankType",-18}{"Material",-12}{"Inc_Exp",-10}{"Date"}");

                while (reader.Read())
                {
                    // Обращение по имени столбца (reader["FuelType"]) безопаснее обращения
                    // по индексу (reader.GetValue(1)) при изменении порядка столбцов в SELECT
                    results.Add(
                        $"{reader["OperationId"],-14}{reader["FuelType"],-16}" +
                        $"{reader["FuelDensity"],-10:F3}{reader["TankType"],-18}" +
                        $"{reader["TankMaterial"],-12}{reader["Inc_Exp"],-10}{reader["Date"]:d}");
                }
            }
            return results;
        }

        /// <summary>
        /// ExecuteScalar возвращает первый столбец первой строки результата.
        /// Идеален для агрегатных запросов: COUNT, MAX, MIN, SUM.
        /// Один объект SqlCommand допускает повторное использование —
        /// достаточно сменить CommandText.
        /// </summary>
        public static void DemoExecuteScalar(SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand(string.Empty, connection);

            cmd.CommandText = "SELECT COUNT(*) FROM Operations;";
            int totalOps = (int)cmd.ExecuteScalar();
            Console.WriteLine($"  Всего операций:          {totalOps}");

            cmd.CommandText = "SELECT MAX(Inc_Exp) FROM Operations;";
            float maxVal = (float)cmd.ExecuteScalar();
            Console.WriteLine($"  Максимальный Inc_Exp:    {maxVal:F2}");

            cmd.CommandText = "SELECT MIN([Date]) FROM Operations;";
            DateTime minDate = (DateTime)cmd.ExecuteScalar();
            Console.WriteLine($"  Дата первой операции:    {minDate:d}");

            cmd.CommandText = "SELECT AVG(FuelDensity) FROM Fuels;";
            double avgDensity = Convert.ToDouble(cmd.ExecuteScalar());
            Console.WriteLine($"  Средняя плотность топлива: {avgDensity:F4}");
        }

        // ================================================================
        // ВСТАВКА (INSERT)
        // ================================================================

        /// <summary>
        /// INSERT через параметризованный SqlCommand с явной транзакцией.
        ///
        /// Параметризация обязательна: конкатенация строк открывает SQL-инъекцию.
        ///
        /// Два стиля добавления параметров:
        ///   Parameters.Add()         — явный SqlDbType, рекомендуется в продакшн-коде.
        ///   Parameters.AddWithValue() — тип выводится из значения C#, удобно для прототипов.
        /// </summary>
        public static string InsertViaCommand(SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand(SqlInsertTank, connection);

            // ── Явное указание типа (предпочтительный стиль) ─────────────────
            // SQL Server получает точный тип данных → оптимальный план выполнения,
            // нет неявного приведения типов, нет риска усечения строки.
            cmd.Parameters.Add("@Type",   SqlDbType.NVarChar, 20).Value = "Бак_Тест";
            cmd.Parameters.Add("@Weight", SqlDbType.Real).Value          = 120.5f;
            cmd.Parameters.Add("@Volume", SqlDbType.Real).Value          = 50.0f;

            // ── AddWithValue (удобно, но с оговоркой) ────────────────────────
            // Тип передаётся как object — SQL Server вынужден выводить его неявно.
            // Для строк .NET передаёт NVarChar(4000), что может не совпасть со схемой.
            cmd.Parameters.AddWithValue("@Material", "Сталь");

            using (SqlTransaction tx = connection.BeginTransaction())
            {
                cmd.Transaction = tx;
                try
                {
                    int rows = cmd.ExecuteNonQuery(); // возвращает число затронутых строк
                    tx.Commit();
                    return $"[InsertViaCommand] Вставлено записей: {rows}";
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return $"[InsertViaCommand] Ошибка (откат транзакции): {ex.Message}";
                }
            }
        }

        /// <summary>
        /// INSERT через хранимую процедуру (CommandType.StoredProcedure).
        /// Логика вставки инкапсулирована в БД; C#-код не знает деталей SQL.
        /// </summary>
        public static string InsertViaStoredProcedure(SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand("uspInsertTanks", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TankType",     "Цистерна_СП");
            cmd.Parameters.AddWithValue("@TankWeight",   300.0f);
            cmd.Parameters.AddWithValue("@TankVolume",   150.0f);
            cmd.Parameters.AddWithValue("@TankMaterial", "Алюминий");

            using (SqlTransaction tx = connection.BeginTransaction())
            {
                cmd.Transaction = tx;
                try
                {
                    cmd.ExecuteNonQuery();
                    tx.Commit();
                    return "[InsertViaStoredProcedure] Запись добавлена через хранимую процедуру.";
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return $"[InsertViaStoredProcedure] Ошибка (откат): {ex.Message}";
                }
            }
        }

        // ================================================================
        // ОБНОВЛЕНИЕ (UPDATE)
        // ================================================================

        /// <summary>
        /// UPDATE через параметризованный SqlCommand с явной транзакцией.
        /// ExecuteNonQuery возвращает число обновлённых строк.
        /// </summary>
        public static string UpdateViaCommand(SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand(SqlUpdateTank, connection);
            cmd.Parameters.Add("@Material", SqlDbType.NVarChar, 20).Value = "Платина";
            cmd.Parameters.Add("@Type",     SqlDbType.NVarChar, 20).Value = "Бак_Тест";

            using (SqlTransaction tx = connection.BeginTransaction())
            {
                cmd.Transaction = tx;
                try
                {
                    int rows = cmd.ExecuteNonQuery();
                    tx.Commit();
                    return $"[UpdateViaCommand] Обновлено записей: {rows}";
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return $"[UpdateViaCommand] Ошибка (откат): {ex.Message}";
                }
            }
        }

        // ================================================================
        // УДАЛЕНИЕ (DELETE)
        // ================================================================

        /// <summary>
        /// DELETE через параметризованный SqlCommand с явной транзакцией.
        /// </summary>
        public static string DeleteViaCommand(SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand(SqlDeleteTank, connection);
            cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 20).Value = "Бак_Тест";

            using (SqlTransaction tx = connection.BeginTransaction())
            {
                cmd.Transaction = tx;
                try
                {
                    int rows = cmd.ExecuteNonQuery();
                    tx.Commit();
                    return $"[DeleteViaCommand] Удалено записей: {rows}";
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return $"[DeleteViaCommand] Ошибка (откат): {ex.Message}";
                }
            }
        }
    }
}
