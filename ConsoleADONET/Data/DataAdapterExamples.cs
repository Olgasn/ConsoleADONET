using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ConsoleADONET.Data
{
    /// <summary>
    /// Примеры CRUD-операций через SqlDataAdapter (отсоединённая модель данных).
    ///
    /// Принцип работы DataAdapter:
    ///   1. Fill()   — загружает данные из БД в DataSet/DataTable (в памяти).
    ///   2. Изменения вносятся в DataRow; каждая строка отслеживает свой RowState
    ///      (Unchanged / Added / Modified / Deleted).
    ///   3. Update() — DataAdapter сам определяет нужную команду по RowState
    ///      и отправляет только изменённые строки в БД.
    ///
    /// Соединение с БД нужно только на шагах Fill() и Update().
    /// Между ними приложение работает с данными автономно.
    /// </summary>
    public static class DataAdapterExamples
    {
        // ── SQL-запросы вынесены в константы ─────────────────────────────
        private const string SqlSelectOperations =
            "SELECT TOP 5 OperationId, FuelType, TankType, Inc_Exp, Date FROM View_AllOperations;";

        // LEFT JOIN двух таблиц: показывает топлива, даже если у них нет операций
        private const string SqlSelectLeftJoin =
            "SELECT TOP 7" +
            "    f.FuelId, f.FuelType, f.FuelDensity," +
            "    o.OperationId, o.Inc_Exp, o.Date " +
            "FROM Fuels f " +
            "LEFT JOIN Operations o ON f.FuelId = o.FuelId " +
            "WHERE f.FuelId <= 5 " +
            "ORDER BY f.FuelId, o.Date DESC;";

        private const string SqlInsertTank =
            "INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) " +
            "VALUES (@Type, @Weight, @Volume, @Material);";

        private const string SqlUpdateTank =
            "UPDATE Tanks SET TankMaterial = @Material, TankWeight = @Weight WHERE TankId = @Id;";

        private const string SqlDeleteTank =
            "DELETE FROM Tanks WHERE TankId = @Id;";

        // ================================================================
        // ВЫБОРКА (SELECT)
        // ================================================================

        /// <summary>
        /// SELECT через SqlDataAdapter.Fill() → DataTable.
        /// DataAdapter открывает и закрывает соединение самостоятельно.
        /// Все данные загружаются в память — удобно для небольших наборов.
        /// </summary>
        public static IList SelectViaDataAdapter(SqlConnection connection)
        {
            var results = new List<string>();

            SqlDataAdapter adapter = new SqlDataAdapter(SqlSelectOperations, connection);

            DataTable table = new DataTable();
            adapter.Fill(table); // соединение открывается → запрос → соединение закрывается

            Console.WriteLine(
                $"{"OperationId",-14}{"FuelType",-20}{"TankType",-18}{"Inc_Exp",-10}{"Date"}");

            foreach (DataRow row in table.Rows)
                results.Add(
                    $"{row["OperationId"],-14}{row["FuelType"],-20}" +
                    $"{row["TankType"],-18}{row["Inc_Exp"],-10}{Convert.ToDateTime(row["Date"]):d}");

            return results;
        }

        /// <summary>
        /// SELECT с LEFT JOIN двух таблиц через SqlDataAdapter → DataTable.
        ///
        /// LEFT JOIN возвращает все строки левой таблицы (Fuels), даже если в правой
        /// (Operations) нет совпадения. Для таких строк столбцы правой таблицы = NULL.
        /// Это принципиальное отличие от INNER JOIN, где строки без пары не попадают в результат.
        ///
        /// NULL в DataRow проверяется через DBNull.Value или DataRow.IsNull().
        /// </summary>
        public static IList SelectJoinViaDataAdapter(SqlConnection connection)
        {
            var results = new List<string>();

            SqlDataAdapter adapter = new SqlDataAdapter(SqlSelectLeftJoin, connection);
            DataTable table = new DataTable();
            adapter.Fill(table);

            Console.WriteLine(
                $"{"FuelId",-8}{"FuelType",-18}{"Density",-10}" +
                $"{"OperationId",-14}{"Inc_Exp",-10}{"Date"}");

            foreach (DataRow row in table.Rows)
            {
                // LEFT JOIN: OperationId может быть NULL, если у топлива нет операций
                string opId    = row.IsNull("OperationId") ? "—" : row["OperationId"].ToString();
                string incExp  = row.IsNull("Inc_Exp")     ? "—" : $"{row["Inc_Exp"]:F1}";
                string date    = row.IsNull("Date")        ? "—" : $"{Convert.ToDateTime(row["Date"]):d}";

                results.Add(
                    $"{row["FuelId"],-8}{row["FuelType"],-18}{row["FuelDensity"],-10:F3}" +
                    $"{opId,-14}{incExp,-10}{date}");
            }
            return results;
        }

        // ================================================================
        // SqlCommandBuilder — автогенерация команд
        // ================================================================

        /// <summary>
        /// SqlCommandBuilder автоматически строит INSERT/UPDATE/DELETE
        /// по SELECT-запросу SelectCommand адаптера.
        ///
        /// Требования:
        ///   • SELECT должен содержать первичный ключ таблицы.
        ///   • Работает только с одной таблицей (без JOIN).
        ///
        /// Плюсы:  не нужно вручную писать InsertCommand/UpdateCommand/DeleteCommand.
        /// Минусы: генерирует избыточный SQL, не поддерживает сложные сценарии.
        ///         В продакшн-коде предпочтительны явные команды (см. Insert/Update/Delete ниже).
        /// </summary>
        public static string DemoSqlCommandBuilder(SqlConnection connection)
        {
            // SELECT содержит FuelId (PK) — обязательное условие для CommandBuilder
            // N-префикс обязателен: без него строковый литерал — VARCHAR, и кириллица
            // на не-кириллической кодовой странице БД (напр. SQL_Latin1_General_CP1) превратится
            // в «?», условие не совпадёт, а UPDATE/DELETE молча затронут 0 строк.
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT FuelId, FuelType, FuelDensity FROM Fuels WHERE FuelType = N'ТестCB';",
                connection);

            // CommandBuilder привязывается к адаптеру и генерирует команды лениво (при первом вызове)
            SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

            // Можно посмотреть сгенерированный SQL ещё до выполнения
            Console.WriteLine("  Сгенерированные команды:");
            Console.WriteLine($"    INSERT: {builder.GetInsertCommand().CommandText}");
            Console.WriteLine($"    UPDATE: {builder.GetUpdateCommand().CommandText}");
            Console.WriteLine($"    DELETE: {builder.GetDeleteCommand().CommandText}");

            DataSet ds = new DataSet();

            // ── INSERT ───────────────────────────────────────────────────
            adapter.Fill(ds, "Fuels");
            DataRow newRow = ds.Tables["Fuels"].NewRow(); // RowState = Detached
            newRow["FuelType"]    = "ТестCB";
            newRow["FuelDensity"] = 0.75f;
            ds.Tables["Fuels"].Rows.Add(newRow);          // RowState = Added
            int inserted = adapter.Update(ds, "Fuels");   // отправляет InsertCommand

            // ── UPDATE ───────────────────────────────────────────────────
            ds.Clear();
            adapter.Fill(ds, "Fuels");
            foreach (DataRow r in ds.Tables["Fuels"].Rows)
                r["FuelDensity"] = 0.99f;                 // RowState = Modified
            int updated = adapter.Update(ds, "Fuels");    // отправляет UpdateCommand

            // ── DELETE ───────────────────────────────────────────────────
            ds.Clear();
            adapter.Fill(ds, "Fuels");
            foreach (DataRow r in ds.Tables["Fuels"].Rows)
                r.Delete();                               // RowState = Deleted
            int deleted = adapter.Update(ds, "Fuels");    // отправляет DeleteCommand

            return $"[SqlCommandBuilder] INSERT={inserted} | UPDATE={updated} | DELETE={deleted}. " +
                   "Все команды сгенерированы автоматически.";
        }

        // ================================================================
        // DataSet с несколькими таблицами и DataRelation
        // ================================================================

        /// <summary>
        /// DataSet может хранить несколько DataTable и связи между ними (DataRelation).
        /// После Fill() соединение с БД больше не нужно — навигация по связям
        /// выполняется полностью в памяти через DataRow.GetChildRows().
        ///
        /// Это аналог JOIN, но «на стороне клиента».
        /// </summary>
        public static void DemoDataRelation(SqlConnection connection)
        {
            DataSet ds = new DataSet("ToplivoSet");

            // Загружаем две таблицы в один DataSet двумя отдельными запросами
            new SqlDataAdapter(
                "SELECT TOP 4 FuelId, FuelType FROM Fuels ORDER BY FuelId;",
                connection).Fill(ds, "Fuels");

            new SqlDataAdapter(
                "SELECT TOP 40 OperationId, FuelId, Inc_Exp, [Date] FROM Operations ORDER BY FuelId;",
                connection).Fill(ds, "Operations");

            // Создаём связь между таблицами в памяти.
            // parentColumn / childColumn — должны быть одного типа (Int32).
            ds.Relations.Add(new DataRelation(
                "FuelOperations",                             // имя связи
                ds.Tables["Fuels"].Columns["FuelId"],         // родительский столбец
                ds.Tables["Operations"].Columns["FuelId"]));  // дочерний столбец

            Console.WriteLine("  Навигация Fuels → Operations через DataRelation:");
            foreach (DataRow fuel in ds.Tables["Fuels"].Rows)
            {
                // GetChildRows возвращает строки Operations, у которых FuelId совпадает
                DataRow[] ops = fuel.GetChildRows("FuelOperations");
                Console.WriteLine(
                    $"    {fuel["FuelType"]} (FuelId={fuel["FuelId"]}): {ops.Length} операций");

                // Показываем первые 2 дочерних строки
                int shown = 0;
                foreach (DataRow op in ops)
                {
                    if (shown++ >= 2) break;
                    Console.WriteLine(
                        $"      OperationId={op["OperationId"],-8}" +
                        $"Inc_Exp={op["Inc_Exp"],-10:F1}Date={op["Date"]:d}");
                }
            }
        }

        // ================================================================
        // ВСТАВКА (INSERT)
        // ================================================================

        /// <summary>
        /// INSERT через SqlDataAdapter.
        /// InsertCommand задаётся явно: параметры привязаны к столбцам DataRow
        /// через четвёртый аргумент Parameters.Add() — имя исходного столбца.
        /// Update() вызывает InsertCommand только для строк с RowState = Added.
        /// </summary>
        public static string InsertViaDataAdapter(SqlConnection connection)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT * FROM Tanks WHERE 1 = 0;", // загружаем только схему, без строк
                connection);

            adapter.InsertCommand = new SqlCommand(SqlInsertTank, connection);
            // Последний аргумент Add() — имя столбца DataRow, из которого берётся значение
            adapter.InsertCommand.Parameters.Add("@Type",     SqlDbType.NVarChar, 20, "TankType");
            adapter.InsertCommand.Parameters.Add("@Weight",   SqlDbType.Real,      0, "TankWeight");
            adapter.InsertCommand.Parameters.Add("@Volume",   SqlDbType.Real,      0, "TankVolume");
            adapter.InsertCommand.Parameters.Add("@Material", SqlDbType.NVarChar, 20, "TankMaterial");

            DataSet ds = new DataSet();
            adapter.Fill(ds, "Tanks");

            DataRow newRow = ds.Tables["Tanks"].NewRow(); // RowState = Detached
            newRow["TankType"]     = "Фляга_DA";
            newRow["TankWeight"]   = 15.0f;
            newRow["TankVolume"]   = 10.0f;
            newRow["TankMaterial"] = "ПЭТ";
            ds.Tables["Tanks"].Rows.Add(newRow);          // RowState = Added

            // Update() проходит по всем строкам DataTable и для каждой Added вызывает InsertCommand
            int rows = adapter.Update(ds, "Tanks");
            return $"[InsertViaDataAdapter] Вставлено: {rows}";
        }

        // ================================================================
        // ОБНОВЛЕНИЕ (UPDATE)
        // ================================================================

        /// <summary>
        /// UPDATE через SqlDataAdapter.
        /// Загружаем строки в DataSet, меняем значения в памяти — RowState становится Modified.
        /// Update() вызывает UpdateCommand только для изменённых строк.
        ///
        /// SourceVersion = Original в параметре WHERE-условия: гарантирует,
        /// что в предикат попадёт значение ключа ДО изменения (оптимистичная блокировка).
        /// </summary>
        public static string UpdateViaDataAdapter(SqlConnection connection)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT * FROM Tanks WHERE TankType = N'Цистерна_СП';",
                connection);

            adapter.UpdateCommand = new SqlCommand(SqlUpdateTank, connection);
            adapter.UpdateCommand.Parameters.Add("@Material", SqlDbType.NVarChar, 20, "TankMaterial");
            adapter.UpdateCommand.Parameters.Add("@Weight",   SqlDbType.Real,      0, "TankWeight");

            // SourceVersion = Original: в WHERE используется значение ключа ДО изменения строки
            SqlParameter pk = adapter.UpdateCommand.Parameters.Add("@Id", SqlDbType.Int, 0, "TankId");
            pk.SourceVersion = DataRowVersion.Original;

            DataSet ds = new DataSet();
            adapter.Fill(ds, "Tanks");

            foreach (DataRow row in ds.Tables["Tanks"].Rows)
            {
                row["TankMaterial"] = "Золото"; // RowState становится Modified
                row["TankWeight"]   = 999.0f;
            }

            // Update() отправляет UpdateCommand только для строк RowState = Modified
            int rows = adapter.Update(ds, "Tanks");
            return $"[UpdateViaDataAdapter] Обновлено: {rows}";
        }

        // ================================================================
        // УДАЛЕНИЕ (DELETE)
        // ================================================================

        /// <summary>
        /// DELETE через SqlDataAdapter.
        /// row.Delete() устанавливает RowState = Deleted (строка не удаляется из DataTable).
        /// Update() вызывает DeleteCommand только для строк с RowState = Deleted.
        /// </summary>
        public static string DeleteViaDataAdapter(SqlConnection connection)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT * FROM Tanks WHERE TankType IN (N'Цистерна_СП', N'Фляга_DA');",
                connection);

            adapter.DeleteCommand = new SqlCommand(SqlDeleteTank, connection);
            SqlParameter pk = adapter.DeleteCommand.Parameters.Add("@Id", SqlDbType.Int, 0, "TankId");
            pk.SourceVersion = DataRowVersion.Original;

            DataSet ds = new DataSet();
            adapter.Fill(ds, "Tanks");

            foreach (DataRow row in ds.Tables["Tanks"].Rows)
                row.Delete(); // RowState = Deleted; строка ещё в DataTable, но помечена

            // Update() отправляет DeleteCommand только для строк RowState = Deleted
            int rows = adapter.Update(ds, "Tanks");
            return $"[DeleteViaDataAdapter] Удалено: {rows}";
        }
    }
}
