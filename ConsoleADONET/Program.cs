using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ConsoleADONET
{
    class Program
    {
        static void Main(string[] args)
        {
            // Считывание строки подключения из конфигурационного файла
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["toplivoConnectionString"].ConnectionString;

            // Инициализация базы данных
            string initializeResult = DbInitializer.Initialize(connectionString);

            // Выполнение операций
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Подключение открыто");

                    ExecuteAndPrint(connection, Select1, "Выборка данных 1");
                    ExecuteAndPrint(connection, Select2, "Выборка данных 2");
                    ExecuteAndPrint(connection, Select3, "Выборка данных 3");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка подключения: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Подключение закрыто...");
                }
            }

            Console.Read();
        }

        static void ExecuteAndPrint(SqlConnection connection, Func<SqlConnection, IList> selectMethod, string operationDescription)
        {
            Console.WriteLine($"====== {operationDescription} (нажмите любую клавишу) ========");
            Console.ReadKey();
            IList results = selectMethod(connection);
            Print(results);
        }

        static void Print(IList items)
        {
            Console.WriteLine("Записи:");
            foreach (var item in items)
            {
                Console.WriteLine(item.ToString());
            }
            Console.WriteLine();
        }

        static IList Select1(SqlConnection connection)
        {
            List<string> results = new List<string>();
            SqlCommand sqlCommand = new SqlCommand("SELECT TOP 7 * FROM Fuels;", connection);
            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    Console.WriteLine("{0}\t{1}\t{2}", reader.GetName(0), reader.GetName(1), reader.GetName(2));
                    while (reader.Read())
                    {
                        object fld1 = reader.GetValue(0);
                        object fld2 = reader.GetValue(1);
                        object fld3 = reader.GetValue(2);
                        results.Add($"{fld1}\t{fld2}\t{fld3}");
                    }
                }
            }
            return results;
        }

        static IList Select2(SqlConnection connection)
        {
            // Реализация Select2
            return new List<string>();
        }

        static IList Select3(SqlConnection connection)
        {
            // Реализация Select3
            return new List<string>();
        }

        static string Insert(SqlConnection connection, string procedureName)
        {
            string message = "";
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                SqlCommand command = new SqlCommand(procedureName, connection)
                {
                    Transaction = transaction
                };
                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    transaction.Commit();
                    message = $"Данные успешно вставлены. Количество добавленных записей: {rowsAffected}.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка вставки: {ex.Message}");
                    transaction.Rollback();
                    message = "Ошибка вставки данных.";
                }
            }
            return message;
        }

        static string Update(SqlConnection connection)
        {
            // Реализация Update
            return "Обновление завершено.";
        }

        static string Delete(SqlConnection connection)
        {
            // Реализация Delete
            return "Удаление завершено.";
        }
    }
}

