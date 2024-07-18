using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace CSharpTracer.Logging
{
    public class Logger
    {
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private readonly string _connectionString;
        private static readonly string databasePath = "logs.db";

        private Logger(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            InitializeDatabase();
        }

        public static Logger GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger(databasePath);
                    }
                }
            }
            return _instance;
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
            @"
                        CREATE TABLE IF NOT EXISTS Logs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Timestamp TEXT NOT NULL,
                            EventName TEXT NOT NULL,
                            Data TEXT NOT NULL
                        );
                    ";
            createTableCommand.ExecuteNonQuery();
        }

        public void LogEvent(string EventName, string timestamp, object data)
        {
            //var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            var jsonData = JsonSerializer.Serialize(data);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            @"
                        INSERT INTO Logs (Timestamp, EventName, Data)
                        VALUES ($timestamp, $EventName, $data);
                    ";

            insertCommand.Parameters.AddWithValue("$timestamp", timestamp);
            insertCommand.Parameters.AddWithValue("$EventName", EventName);
            insertCommand.Parameters.AddWithValue("$data", jsonData);

            insertCommand.ExecuteNonQuery();
        }

        public DataTable GetLogs()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT * FROM Logs;";

            using var reader = selectCommand.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            return dataTable;
        }
    }
}
