using System;
using System.Data.SqlClient;
using System.Globalization;

namespace ConsoleADONET
{
    public static class DbInitializer
    {
        public static string Initialize(string connectionString)
        {
            //Заполнение случайными данными трех предварительно созданных таблиц Fuels, Tanks, Operations
            //Инициализация перменных                                                 
            int tanks_number = 75;
            int fuels_number = 75;
            int operations_number = 3000;
            string tankType;
            string tankMaterial;
            float tankWeight;
            float tankVolume;
            string fuelType;
            float fuelDensity;
            string result = "";
            Random randObj = new Random(1);
            string specifier = "G";
            CultureInfo culture = CultureInfo.InvariantCulture;
            DateTime today = DateTime.Now.Date;
            string strSql;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Открытие соединения
                connection.Open();
                // Проверка существуют ли записи в таблице Fuels
                SqlCommand check_Fuels = new SqlCommand("SELECT COUNT(*) FROM Fuels;", connection);
                int RecordsExist = (int)check_Fuels.ExecuteScalar();

                // Собственно заполнение базы данных записями
                if (RecordsExist == 0)
                {
                    // Открытие транзакции для выполнения команд вставки данных
                    SqlTransaction transaction = connection.BeginTransaction();

                    SqlCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    try
                    {
                        //Заполнение таблицы емкостей
                        //Словари для Tanks
                        string[] tank_voc = { "Цистерна_", "Ведро_", "Бак_", "Фляга_", "Цистерна_", "Стакан_" };//словарь названий емкостей
                        string[] material_voc = { "Сталь", "Платина", "Алюминий", "ПЭТ", "Чугун", "Алюминий", "Золото", "Дерево", "Керамика" };//словарь названий материалов емкостей
                        int count_tank_voc = tank_voc.GetLength(0);
                        int count_material_voc = material_voc.GetLength(0);
                        int tankID;
                        strSql = "INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) VALUES ";
                        for (tankID = 1; tankID <= tanks_number; tankID++)
                        {
                            tankType = "N'" + tank_voc[randObj.Next(count_tank_voc)] + tankID.ToString() + "'";
                            tankMaterial = "N'" + material_voc[randObj.Next(count_material_voc)] + "'";
                            tankWeight = 500 * (float)randObj.NextDouble();
                            tankVolume = 200 * (float)randObj.NextDouble();
                            strSql += "(" + tankType + ", " + tankWeight.ToString(specifier, culture) + ", " + tankVolume.ToString(specifier, culture) + ", " + tankMaterial + "), ";
                        }
                        command.CommandText = strSql.TrimEnd(new Char[] { ',', ' ' }) + ";";
                        //отправляет команду на вставку в базу данных
                        command.ExecuteNonQuery();

                        //Заполнение таблицы видов топлива
                        //Словарь для Fuels
                        string[] fuel_voc = { "Нефть_", "Бензин_", "Керосин_", "Мазут_", "Спирт_", "Водород_" };//словарь названий видов топлива
                        int count_fuel_voc = fuel_voc.GetLength(0);
                        int fuelID;
                        strSql = "INSERT INTO Fuels (FuelType, FuelDensity) VALUES";
                        for (fuelID = 1; fuelID <= fuels_number; fuelID++)
                        {
                            fuelType = "N'" + fuel_voc[randObj.Next(count_fuel_voc)] + fuelID.ToString() + "'";
                            fuelDensity = 2 * (float)randObj.NextDouble();
                            strSql += "(" + fuelType + ", " + fuelDensity.ToString(specifier, culture) + "), ";
                        }
                        command.CommandText = strSql.TrimEnd(new Char[] { ',', ' ' }) + ";";
                        //отправляет команду на вставку в базу данных
                        command.ExecuteNonQuery();

                        //Заполнение таблицы операций
                        DateTime operationdate;
                        int inc_exp;
                        strSql = "INSERT INTO Operations (TankID, FuelID, Inc_Exp, Date) VALUES";
                        string strSqlValues;
                        for (int operationID = 1; operationID <= operations_number; operationID++)
                        {
                            tankID = randObj.Next(1, tanks_number - 1);
                            fuelID = randObj.Next(1, fuels_number - 1);
                            inc_exp = randObj.Next(200) - 100;
                            operationdate = today.AddDays(-operationID);
                            strSqlValues = strSql+ "(" + tankID + ", " + fuelID + ", " + 
                                inc_exp.ToString(specifier, culture) + ", " +
                                "'" + operationdate.ToString(culture) + "');";
                            //отправляет команду на вставку в базу данных
                            command.ExecuteNonQuery();
                        }


                        // подтверждаем транзакцию
                        transaction.Commit();

                    }
                    // Обработка ошибок внутри транзакции
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        Console.WriteLine(result);
                        transaction.Rollback();
                    }

                }
            }
            return result;

        }
    }

}