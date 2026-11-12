using System;
using System.Collections.Generic;
using CybersecurityAwarenessBot.Models;
// Aliases make sure we always use the MySqlConnector classes
using MySqlConn = MySqlConnector.MySqlConnection;
using MySqlCmd = MySqlConnector.MySqlCommand;

namespace CybersecurityAwarenessBot.Services
{
    /// <summary>
    /// Task assistant data access (CRUD) for the CyberTasks table.
    /// Methods throw on database errors; TaskWindow catches them and shows a message.
    /// </summary>
    public class TaskService
    {
        private readonly DatabaseService _database;

        public TaskService(DatabaseService database)
        {
            _database = database;
        }

        /// <summary>READ: every task, newest last.</summary>
        public List<CyberTask> GetAllTasks()
        {
            var tasks = new List<CyberTask>();

            using (MySqlConn connection = _database.GetConnection())
            {
                connection.Open();

                const string sql =
                    "SELECT TaskId, Title, Description, ReminderDate, IsCompleted, CreatedAt " +
                    "FROM CyberTasks ORDER BY TaskId";

                using (var command = new MySqlCmd(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    int idCol = reader.GetOrdinal("TaskId");
                    int titleCol = reader.GetOrdinal("Title");
                    int descCol = reader.GetOrdinal("Description");
                    int reminderCol = reader.GetOrdinal("ReminderDate");
                    int doneCol = reader.GetOrdinal("IsCompleted");
                    int createdCol = reader.GetOrdinal("CreatedAt");

                    while (reader.Read())
                    {
                        tasks.Add(new CyberTask
                        {
                            TaskId = reader.GetInt32(idCol),
                            Title = reader.GetString(titleCol),
                            Description = reader.IsDBNull(descCol) ? string.Empty : reader.GetString(descCol),
                            ReminderDate = reader.IsDBNull(reminderCol) ? null : reader.GetDateTime(reminderCol),
                            IsCompleted = reader.GetBoolean(doneCol),
                            CreatedAt = reader.IsDBNull(createdCol) ? DateTime.Now : reader.GetDateTime(createdCol)
                        });
                    }
                }
            }

            return tasks;
        }

        /// <summary>CREATE: adds a task and returns its new TaskId.</summary>
        public int AddTask(string title, string description, DateTime? reminderDate)
        {
            using (MySqlConn connection = _database.GetConnection())
            {
                connection.Open();

                const string sql =
                    "INSERT INTO CyberTasks (Title, Description, ReminderDate, IsCompleted) " +
                    "VALUES (@title, @description, @reminder, FALSE)";

                using (var command = new MySqlCmd(sql, connection))
                {
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@description", description);
                    command.Parameters.AddWithValue("@reminder", (object?)reminderDate ?? DBNull.Value);
                    command.ExecuteNonQuery();
                    return (int)command.LastInsertedId;
                }
            }
        }

        /// <summary>UPDATE: sets (or clears) the reminder of a task.</summary>
        public void SetReminder(int taskId, DateTime? reminderDate)
        {
            using (MySqlConn connection = _database.GetConnection())
            {
                connection.Open();

                using (var command = new MySqlCmd(
                    "UPDATE CyberTasks SET ReminderDate = @reminder WHERE TaskId = @id", connection))
                {
                    command.Parameters.AddWithValue("@reminder", (object?)reminderDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>UPDATE: marks a task as completed.</summary>
        public void MarkCompleted(int taskId)
        {
            using (MySqlConn connection = _database.GetConnection())
            {
                connection.Open();

                using (var command = new MySqlCmd(
                    "UPDATE CyberTasks SET IsCompleted = TRUE WHERE TaskId = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>DELETE: removes a task.</summary>
        public void DeleteTask(int taskId)
        {
            using (MySqlConn connection = _database.GetConnection())
            {
                connection.Open();

                using (var command = new MySqlCmd(
                    "DELETE FROM CyberTasks WHERE TaskId = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}