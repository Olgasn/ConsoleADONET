using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleADONET
{
    public static class DbInitializer
    {
        public static string Initialize(string connectionString)
        {
                                                  
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
            string specifier="G";
            CultureInfo culture= CultureInfo.InvariantCulture;


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand check_Fuels = new SqlCommand("SELECT COUNT(*) FROM [Fuels]", connection);
                int RecordsExist = (int)check_Fuels.ExecuteScalar();
                if (RecordsExist > 0)
                {
                    SqlTransaction transaction = connection.BeginTransaction();
                    SqlCommand command = connection.CreateCommand();
                    command.Transaction = transaction;
                    try
                    {
                        //Заполнение таблицы емкостей
                        string[] tank_voc = { "Цистерна_", "Ведро_", "Бак_", "Фляга_", "Цистерна_", "Стакан_" };//словарь названий емкостей
                        string[] material_voc = { "Сталь", "Платина", "Алюминий", "ПЭТ", "Чугун", "Алюминий", "Золото", "Дерево", "Керамика" };//словарь названий видов топлива
                        int count_tank_voc = tank_voc.GetLength(0);
                        int count_material_voc = material_voc.GetLength(0);
                        string strSql= "INSERT INTO Tanks (TankType, TankWeight, TankVolume, TankMaterial) VALUES ";
                        for (int tankID = 1; tankID <= tanks_number; tankID++)
                        {
                            tankType = "'"+tank_voc[randObj.Next(count_tank_voc)] + tankID.ToString()+"'";
                            tankMaterial = "'"+material_voc[randObj.Next(count_material_voc)]+ "'";
                            tankWeight = 500 * (float)randObj.NextDouble();
                            tankVolume = 200 * (float)randObj.NextDouble();
                            strSql = strSql + "(" + tankType + ", " + tankWeight.ToString(specifier, culture) + ", " + tankVolume.ToString(specifier, culture) + ", " + tankMaterial + "), ";
                        }
                        command.CommandText = strSql.TrimEnd(new Char[] { ',', ' ' }) + ";";
                        //отправляет команду на вставку в базу данных
                        command.ExecuteNonQuery();

                        //Заполнение таблицы видов топлива
                        string[] fuel_voc = { "Нефть_", "Бензин_", "Керосин_", "Мазут_", "Спирт_", "Водород_" };
                        int count_fuel_voc = fuel_voc.GetLength(0);
                        strSql = "INSERT INTO Fuels (FuelType, FuelDensity) VALUES";
                        for (int fuelID = 1; fuelID <= fuels_number; fuelID++)
                        {
                            fuelType = "'"+ fuel_voc[randObj.Next(count_fuel_voc)] + fuelID.ToString()+ "'";
                            fuelDensity = 2 * (float)randObj.NextDouble();
                            strSql = strSql + "(" + fuelType + ", " + fuelDensity.ToString(specifier, culture) + "), ";
                        }
                        command.CommandText = strSql.TrimEnd(new Char[] { ',', ' ' }) + ";";
                        //отправляет команду на вставку в базу данных
                        command.ExecuteNonQuery();

                        //Заполнение таблицы операций
                        strSql = "INSERT INTO Fuels (TankID, FuelID, Inc_Exp, Date) VALUES";
                        for (int operationID = 1; operationID <= operations_number; operationID++)
                        {
                            int tankID = randObj.Next(1, tanks_number - 1);
                            int fuelID = randObj.Next(1, fuels_number - 1);
                            int inc_exp = randObj.Next(200) - 100;
                            DateTime today = DateTime.Now.Date;
                            DateTime operationdate = today.AddDays(-operationID);
                            strSql = strSql + "(" + tankID + ", " + fuelID + ", " + inc_exp.ToString(specifier, culture) + ", " + operationdate + "), ";
                        }
                        command.CommandText = strSql.TrimEnd(new Char[] { ',', ' ' }) + ";";
                        //отправляет команду на вставку в базу данных
                        command.ExecuteNonQuery();

                        // подтверждаем транзакцию
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        transaction.Rollback();
                    }

                }
            }
            return result;

        }
    }

}