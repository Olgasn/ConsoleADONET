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
            //Открытие соединения для выполнения команд
            using (SqlConnection connection = new SqlConnection(сonnectionString))
            {
                connection.Open();

                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select1(connection));
                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select2(connection));
                Console.WriteLine("====== Будет выполнена выборка данных (нажмите любую клавишу) ========");
                Console.ReadKey();
                Print(Select3(connection));

            }
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

        static string Insert(SqlConnection connection)
        {
            string message = "";
            SqlTransaction transaction = connection.BeginTransaction();
            SqlCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            try
            {

                command.CommandText = "";
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

