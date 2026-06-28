using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleADONET.Tests
{
    /// <summary>
    /// Инфраструктура интеграционных тестов: разрешение строки подключения,
    /// создание схемы по CreateDB.sql и «мягкая» деградация, если SQL Server недоступен.
    ///
    /// Строка подключения берётся из переменной окружения TEST_CONNECTION_STRING,
    /// иначе используется LocalDB с отдельной базой toplivo_citest (чтобы не затронуть
    /// рабочую базу toplivo_test из App.config приложения).
    ///
    /// Если базу поднять не удалось, тесты не падают, а помечаются Inconclusive —
    /// сборка остаётся «зелёной» там, где SQL Server физически нет.
    /// </summary>
    internal static class TestDatabase
    {
        private const string DefaultConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=toplivo_citest;Integrated Security=True;Encrypt=False;Connect Timeout=15";

        private static readonly object Gate = new object();
        private static bool _checked;
        private static string _connectionString;
        private static string _skipReason;

        /// <summary>Строка подключения к тестовой базе.</summary>
        public static string ConnectionString =>
            Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? DefaultConnectionString;

        /// <summary>Доступна ли база (с уже созданной схемой). Проверка выполняется один раз.</summary>
        public static bool IsAvailable
        {
            get
            {
                EnsureChecked();
                return _skipReason == null;
            }
        }

        /// <summary>Причина пропуска интеграционных тестов (null, если база доступна).</summary>
        public static string SkipReason
        {
            get
            {
                EnsureChecked();
                return _skipReason;
            }
        }

        /// <summary>Открытое подключение к тестовой базе.</summary>
        public static SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Приводит базу к известному состоянию: очищает три таблицы и заполняет заново
        /// детерминированным набором (DbInitializer использует Random(1)).
        /// Используется в ClassInitialize тестов CRUD-примеров, которым нужны данные.
        /// </summary>
        public static void Seed(int tanksCount, int fuelsCount, int operationsCount)
        {
            using (var connection = OpenConnection())
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Operations; DELETE FROM Fuels; DELETE FROM Tanks;";
                cmd.ExecuteNonQuery();
            }

            DbInitializer.Initialize(ConnectionString, tanksCount, fuelsCount, operationsCount);
        }

        private static void EnsureChecked()
        {
            lock (Gate)
            {
                if (_checked)
                    return;

                _checked = true;
                _connectionString = ConnectionString;
                try
                {
                    EnsureSchema(_connectionString);
                }
                catch (Exception ex)
                {
                    _skipReason =
                        "Тестовая база данных недоступна, интеграционные тесты пропущены. " +
                        $"Строка подключения: {Mask(_connectionString)}. Причина: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Создаёт схему (таблицы, представление, процедуры) по CreateDB.sql, если её ещё нет.
        /// Имя базы из CreateDB.sql (toplivo_test) заменяется на имя из строки подключения,
        /// чтобы тесты использовали свою отдельную базу.
        ///
        /// Все проверки выполняются через подключение к master: открывать соединение
        /// с ещё не существующей целевой базой нельзя — неудача «отравит» пул подключений
        /// SqlClient (период блокировки), и следующий Open() вернёт кэшированную ошибку логина.
        /// </summary>
        private static void EnsureSchema(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string targetDb = string.IsNullOrEmpty(builder.InitialCatalog) ? "toplivo_citest" : builder.InitialCatalog;

            var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            using (var master = new SqlConnection(masterBuilder.ConnectionString))
            {
                master.Open();

                if (DatabaseHasSchema(master, targetDb))
                    return;

                // CreateDB.sql использует литерал toplivo_test только как имя базы — безопасно заменить.
                // Скрипт сам выполняет USE master / CREATE DATABASE / USE <db>, поэтому гоним его в master.
                string script = File.ReadAllText(ScriptPath()).Replace("toplivo_test", targetDb);
                foreach (string batch in SplitOnGo(script))
                {
                    using (var cmd = master.CreateCommand())
                    {
                        cmd.CommandText = batch;
                        cmd.CommandTimeout = 60;
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // База только что создана — сбрасываем пул на случай неудачных подключений,
            // закэшированных до её появления.
            SqlConnection.ClearAllPools();
        }

        /// <summary>Существует ли целевая база и есть ли в ней все три таблицы (проверка через master).</summary>
        private static bool DatabaseHasSchema(SqlConnection master, string targetDb)
        {
            object dbId;
            using (var cmd = master.CreateCommand())
            {
                cmd.CommandText = "SELECT DB_ID(@db);";
                cmd.Parameters.AddWithValue("@db", targetDb);
                dbId = cmd.ExecuteScalar();
            }
            if (dbId == null || dbId == DBNull.Value)
                return false;

            using (var cmd = master.CreateCommand())
            {
                cmd.CommandText =
                    $"SELECT COUNT(*) FROM [{targetDb}].sys.tables WHERE name IN ('Fuels', 'Tanks', 'Operations');";
                return Convert.ToInt32(cmd.ExecuteScalar()) == 3;
            }
        }

        private static string ScriptPath()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CreateDB.sql");
            if (!File.Exists(path))
                throw new FileNotFoundException("Не найден CreateDB.sql рядом с тестовой сборкой.", path);
            return path;
        }

        /// <summary>Разбивает T-SQL скрипт на батчи по строкам-разделителям GO.</summary>
        private static IEnumerable<string> SplitOnGo(string script)
        {
            string[] parts = Regex.Split(script, @"(?im)^\s*GO\s*$");
            foreach (string part in parts)
            {
                string batch = part.Trim();
                if (batch.Length > 0)
                    yield return batch;
            }
        }

        private static string Mask(string connectionString)
        {
            try
            {
                var b = new SqlConnectionStringBuilder(connectionString);
                return $"{b.DataSource}/{b.InitialCatalog}";
            }
            catch
            {
                return "(нечитаемая строка подключения)";
            }
        }
    }

    /// <summary>
    /// Базовый класс интеграционных тестов: гарантирует, что тест выполняется только
    /// при доступной базе; иначе помечает тест Inconclusive (не Failed).
    /// </summary>
    public abstract class IntegrationTestBase
    {
        protected SqlConnection Connection { get; private set; }

        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void SkipIfNoDatabase()
        {
            if (!TestDatabase.IsAvailable)
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Inconclusive(TestDatabase.SkipReason);

            Connection = TestDatabase.OpenConnection();
        }

        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup]
        public void CloseConnection()
        {
            Connection?.Dispose();
            Connection = null;
        }
    }
}
