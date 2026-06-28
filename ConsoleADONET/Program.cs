using System;
using System.Collections;
using Microsoft.Data.SqlClient;
using ConsoleADONET.Data;

namespace ConsoleADONET
{
    /// <summary>
    /// Точка входа. Отвечает только за последовательность демонстраций.
    /// Вся логика работы с БД вынесена в CommandExamples и DataAdapterExamples.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // На .NET 10 кодировка консоли Windows по умолчанию не UTF-8 — задаём явно,
            // иначе кириллица в выводе превратится в «?».
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* нет консоли */ }

            // Считывает строку подключения из App.config (ключ "toplivoConnectionString")
            string connectionString = System.Configuration.ConfigurationManager
                .ConnectionStrings["toplivoConnectionString"].ConnectionString;

            // Заполняет таблицы тестовыми данными, если они пусты
            DbInitializer.Initialize(connectionString);

            // Открывает подключение и выполняет демонстрации
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Подключение открыто\n");

                    // ====================================================
                    // ВЫБОРКА (SELECT)
                    // ====================================================

                    RunAndPrint(connection, CommandExamples.SelectViaCommand,
                        "SELECT | SqlCommand + SqlDataReader -> List<Tank>");

                    RunAndPrint(connection, CommandExamples.SelectViaStoredProcedure,
                        "SELECT | SqlCommand + хранимая процедура uspGetOperations");

                    RunAndPrint(connection, DataAdapterExamples.SelectViaDataAdapter,
                        "SELECT | SqlDataAdapter.Fill() -> DataTable");

                    RunAndPrint(connection, CommandExamples.SelectJoinViaCommand,
                        "SELECT | INNER JOIN трёх таблиц (Operations + Fuels + Tanks) через SqlCommand");

                    RunAndPrint(connection, DataAdapterExamples.SelectJoinViaDataAdapter,
                        "SELECT | LEFT JOIN двух таблиц (Fuels + Operations) через SqlDataAdapter");

                    // ────────────────────────────────────────────────────
                    Pause("SELECT | ExecuteScalar: COUNT / MAX / MIN / AVG");
                    CommandExamples.DemoExecuteScalar(connection);

                    // ====================================================
                    // ВСТАВКА (INSERT)
                    // ====================================================

                    Pause("INSERT | SqlCommand + параметры (Add против AddWithValue)");
                    Console.WriteLine(CommandExamples.InsertViaCommand(connection));

                    Pause("INSERT | SqlCommand + хранимая процедура uspInsertTanks");
                    Console.WriteLine(CommandExamples.InsertViaStoredProcedure(connection));

                    Pause("INSERT | SqlDataAdapter + InsertCommand");
                    Console.WriteLine(DataAdapterExamples.InsertViaDataAdapter(connection));

                    // ====================================================
                    // ОБНОВЛЕНИЕ (UPDATE)
                    // ====================================================

                    Pause("UPDATE | SqlCommand + параметры");
                    Console.WriteLine(CommandExamples.UpdateViaCommand(connection));

                    Pause("UPDATE | SqlDataAdapter + UpdateCommand + DataRowVersion.Original");
                    Console.WriteLine(DataAdapterExamples.UpdateViaDataAdapter(connection));

                    // ====================================================
                    // УДАЛЕНИЕ (DELETE)
                    // ====================================================

                    Pause("DELETE | SqlCommand + параметры");
                    Console.WriteLine(CommandExamples.DeleteViaCommand(connection));

                    Pause("DELETE | SqlDataAdapter + DeleteCommand");
                    Console.WriteLine(DataAdapterExamples.DeleteViaDataAdapter(connection));

                    // ====================================================
                    // ДОПОЛНИТЕЛЬНО
                    // ====================================================

                    Pause("SqlCommandBuilder | автогенерация INSERT / UPDATE / DELETE");
                    Console.WriteLine(DataAdapterExamples.DemoSqlCommandBuilder(connection));

                    Pause("DataSet + DataRelation | несколько таблиц, навигация без JOIN");
                    DataAdapterExamples.DemoDataRelation(connection);
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка SQL: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("\nПодключение закрыто");
                }
            }

            Console.WriteLine("\nГотово. Нажмите любую клавишу для выхода.");
            Console.ReadKey();
        }

        // ── Вспомогательные методы ────────────────────────────────────────

        static void Pause(string description)
        {
            Console.WriteLine($"\n====== {description} (нажмите любую клавишу) ======");
            Console.ReadKey();
        }

        static void RunAndPrint(SqlConnection connection,
            Func<SqlConnection, IList> method, string description)
        {
            Pause(description);
            IList items = method(connection);
            Console.WriteLine("Записи:");
            foreach (var item in items)
                Console.WriteLine(item);
            Console.WriteLine();
        }
    }
}
