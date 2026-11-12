using System;
// Aliases make sure we always use the MySqlConnector classes, even if the
// project contains its own class called MySqlConnection (this caused CS1729/CS1674).
using MySqlConn = MySqlConnector.MySqlConnection;
using MySqlCmd = MySqlConnector.MySqlCommand;

namespace CybersecurityAwarenessBot.Services
{
    /// <summary>
    /// Creates the MySQL database/table and hands out connections.
    /// </summary>
    public class DatabaseService
    {
        // TODO: replace YOUR_PASSWORD with your MySQL root password.
        private const string ServerConnectionString =
            "Server=localhost;User ID=root;Password=Habile@26!;";

        private const string DatabaseConnectionString =
            "Server=localhost;Database=cybersecuritybot;User ID=root;Password=Habile@26!;";

        public MySqlConn GetConnection()
        {
            return new MySqlConn(DatabaseConnectionString);
        }

        /// <summary>Creates the database and CyberTasks table if they do not exist.</summary>
        public void CreateDatabaseAndTable()
        {
            using (var connection = new MySqlConn(ServerConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCmd(
                    "CREATE DATABASE IF NOT EXISTS cybersecuritybot", connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            using (var connection = GetConnection())
            {
                connection.Open();

                const string tableQuery = @"
                    CREATE TABLE IF NOT EXISTS CyberTasks
                    (
                        TaskId INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(200) NOT NULL,
                        Description TEXT,
                        ReminderDate DATETIME NULL,
                        IsCompleted BOOLEAN DEFAULT FALSE,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";

                using (var command = new MySqlCmd(tableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}