using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleADONET
{
    class Program
    {
        public static object ConfigurationManager { get; private set; }

        static void Main(string[] args)
        {
            //Считывание строки подключения из конфигурационного файла
            string сonnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["toplivoConnectionString"].ConnectionString;
            //Инициализация базы данных
            string InitializeResult = DbInitializer.Initialize(сonnectionString);
            // Создание подключения
            SqlConnection connection = new SqlConnection(сonnectionString);
            try
            {
                // Открываем подключение для выполенения команд
                connection.Open();
                Console.WriteLine("Подключение открыто");

                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select1(connection));
                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select2(connection));
                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select3(connection));
                //Console.WriteLine("====== Будет выполнена вставка данных (нажмите любую клавишу) ========");
                //Console.ReadKey();
                //Print(Insert(connection,"procedureName"));
            }
            // Обработка ошибок соединения
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            // Закрытие соединения с базой данных
            finally
            {
                // закрываем подключение
                connection.Close();
                Console.WriteLine("Подключение закрыто...");
            }

            Console.Read();



        }



        static void Print(IList items)
        {
            Console.WriteLine("Записи: ");
            foreach (var item in items)
            {
                Console.WriteLine(item.ToString());
            }
            Console.WriteLine();
            Console.ReadKey();
        }

        static string Insert(SqlConnection connection, String procedureName)
        {
            string message = "";
            SqlTransaction transaction = connection.BeginTransaction();
            SqlCommand command = new SqlCommand(procedureName, connection);
            command.Transaction = transaction;
            try
            {

                //command.CommandType = System.Data.CommandType.StoredProcedure;
                //SqlParameter nameParam = new SqlParameter
                //{
                //    ParameterName = "@name",
                //    Value = name
                //};
                ////добавляем параметр
                //command.Parameters.Add(nameParam);

                //отправляет команду в базу данных
                command.ExecuteNonQuery();

                // подтверждаем транзакцию
                transaction.Commit();
                message = "";


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
                message = "";

            }
            return message;
 
        }

        static string Delete(SqlConnection connection)
        {
            string message = "";


            //todo
            return message;

        }
        static IList Select1(SqlConnection connection)
        {
            List<string> results= new List<string>();
            //todo
            SqlCommand sqlCommand = new SqlCommand("SELECT TOP 5 * FROM Fuels;",connection);
            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows) // если есть данные
            {
                // выводим названия столбцов
                Console.WriteLine("{0}\t{1}\t{2}", reader.GetName(0), reader.GetName(1), reader.GetName(2));

                while (reader.Read()) // построчно считываем данные
                {
                    object fld1 = reader.GetValue(0);
                    object fld2 = reader.GetValue(1);
                    object fld3 = reader.GetValue(2);

                    Console.WriteLine("{0} \t{1} \t{2}", fld1, fld2, fld3);
                }
            }

            reader.Close();
            return results;
        }
        static IList Select2(SqlConnection connection)
        {
            List<string> results = new List<string>();
            //todo

            return results;
        }
        static IList Select3(SqlConnection connection)
        {
            List<string> results = new List<string>();
            //todo

            return results;
        }

        static string Update(SqlConnection connection)
        {
            string message = "";

            //todo

            return message;

        }

    }
    }

