using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using CsharpTracer.Helpers;

namespace CSharpTracer.Logging
{
    public class Logger
    {
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private readonly string _connectionString;
        private static readonly string databasePath = "logs.db";

        internal class GraphRecord
        {
            public string Source { get; set; } = string.Empty;
            public string SourceType { get; set; } = string.Empty;
            public string EdgeType { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
            public string TargetType { get; set; } = string.Empty;
            public long Observations { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
        }

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

            var createLogTableCommand = connection.CreateCommand();
            createLogTableCommand.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    EventName TEXT NOT NULL,
                    Data JSON NOT NULL
                );
            ";
            createLogTableCommand.ExecuteNonQuery();

            var createNodeTableCommand = connection.CreateCommand();
            createNodeTableCommand.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Nodes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uuid TEXT UNIQUE,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Observations BigInt NOT NULL,
                    FirstSeen DATETIME NOT NULL,
                    LastSeen DATETIME NOT NULL
                );
            ";
            createNodeTableCommand.ExecuteNonQuery();

            var createEdgeTableCommand = connection.CreateCommand();
            createEdgeTableCommand.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS Edges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uuid TEXT UNIQUE,
                    SourceUuid TEXT NOT NULL,
                    EdgeType TEXT NOT NULL,
                    TargetUuid TEXT NOT NULL,
                    Observations BigInt NOT NULL,
                    FirstSeen DATETIME NOT NULL,
                    LastSeen DATETIME NOT NULL
                );
            ";
            createEdgeTableCommand.ExecuteNonQuery();
        }

        internal void LogGraph(GraphRecord record)

        {
            // Generate UUIDs (type + name)
            var sourceUuid = HashGenerator.GenerateUUIDv5(record.Source + record.SourceType);
            var targetUuid = HashGenerator.GenerateUUIDv5(record.Target + record.TargetType);

            var edgeUuid = HashGenerator.GenerateUUIDv5(sourceUuid.ToString() + record.EdgeType + targetUuid.ToString());

            // Log the edge
            LogEdge(edgeUuid.ToString(), sourceUuid.ToString(), record.EdgeType, targetUuid.ToString(), record.Observations, record.FirstSeen, record.LastSeen);

            //Log the source and target
            LogNode(sourceUuid.ToString(), record.SourceType, record.Source, record.Observations, record.FirstSeen, record.LastSeen);
            LogNode(targetUuid.ToString(), record.TargetType, record.Target, record.Observations, record.FirstSeen, record.LastSeen);


        }

        internal void LogEdge(string EdgeUuid, string SourceUuid, string edgeType, string TargetUuid, long Observations, DateTime FirstSeen, DateTime LastSeen)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            @"
                INSERT INTO Edges (Uuid, SourceUuid, EdgeType, TargetUuid, Observations, FirstSeen, LastSeen)
                VALUES ($Uuid, $SourceUuid, $EdgeType, $TargetUuid, $Observations, $FirstSeen, $LastSeen)
                ON CONFLICT(Uuid) DO UPDATE SET Observations = Observations + $Observations, LastSeen = $LastSeen;
            ";

            insertCommand.Parameters.AddWithValue("$Uuid", EdgeUuid);
            insertCommand.Parameters.AddWithValue("$SourceUuid", SourceUuid);
            insertCommand.Parameters.AddWithValue("$EdgeType", edgeType);
            insertCommand.Parameters.AddWithValue("$TargetUuid", TargetUuid);
            insertCommand.Parameters.AddWithValue("$Observations", Observations);
            insertCommand.Parameters.AddWithValue("$FirstSeen", FirstSeen);
            insertCommand.Parameters.AddWithValue("$LastSeen", LastSeen);

            insertCommand.ExecuteNonQuery();
        }

        internal void LogNode(string Uuid, string Type, string Name, long Observations, DateTime FirstSeen, DateTime LastSeen)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
            @"
                INSERT INTO Nodes (Uuid, Type, Name, Observations, FirstSeen, LastSeen)
                VALUES ($Uuid, $Type, $Name, $Observations, $FirstSeen, $LastSeen)
                ON CONFLICT(Uuid) DO UPDATE SET Observations = Observations + $Observations, LastSeen = $LastSeen;
            ";

            insertCommand.Parameters.AddWithValue("$Uuid", Uuid);
            insertCommand.Parameters.AddWithValue("$Type", Type);
            insertCommand.Parameters.AddWithValue("$Name", Name);
            insertCommand.Parameters.AddWithValue("$Observations", Observations);
            insertCommand.Parameters.AddWithValue("$FirstSeen", FirstSeen);
            insertCommand.Parameters.AddWithValue("$LastSeen", LastSeen);

            insertCommand.ExecuteNonQuery();
        }

        internal void LogEvent(string EventName, string timestamp, object data)
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

        internal DataTable GetLogs()
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
