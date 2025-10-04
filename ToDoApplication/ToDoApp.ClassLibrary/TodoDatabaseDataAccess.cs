using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace ToDoApp.ClassLibrary
{
    public class TodoDatabaseDataAccess : ITodoDataAccess
    {
        private readonly string _connectionString;

        // Constructor for dependency injection
        public TodoDatabaseDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<TodoModel> LoadTodoItems()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT id, task_description as TaskDescription, datetime_created as DateTimeCreated, is_complete as IsComplete FROM todo_items";
            return connection.Query<TodoModel>(sql).ToList();
        }

        public void SaveTodoItems(List<TodoModel> todoItems)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get all existing IDs in the database
                var existingIds = connection.Query<int>(
                    "SELECT id FROM todo_items",
                    transaction: transaction
                ).ToHashSet();

                var todoIds = todoItems.Select(t => t.Id).ToHashSet();

                // UPDATE existing todos
                foreach (var item in todoItems.Where(t => existingIds.Contains(t.Id)))
                {
                    var sql = @"UPDATE todo_items 
                       SET task_description = @TaskDescription, 
                           is_complete = @IsComplete,
                           datetime_created = @DateTimeCreated
                       WHERE id = @Id";
                    connection.Execute(sql, item, transaction);
                }

                // INSERT new todos (only if they don't exist)
                foreach (var item in todoItems.Where(t => !existingIds.Contains(t.Id)))
                {
                    var sql = @"INSERT INTO todo_items (id, task_description, datetime_created, is_complete) 
                       VALUES (@Id, @TaskDescription, @DateTimeCreated, @IsComplete)";
                    connection.Execute(sql, item, transaction);
                }

                // DELETE todos not in the list
                var idsToDelete = existingIds.Except(todoIds).ToList();
                if (idsToDelete.Any())
                {
                    connection.Execute(
                        "DELETE FROM todo_items WHERE id = ANY(@Ids)",
                        new { Ids = idsToDelete.ToArray() },
                        transaction
                    );
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public TodoModel CreateTodoItem(string taskDescription)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO todo_items (task_description, datetime_created, is_complete) 
                VALUES (@TaskDescription, @DateTimeCreated, @IsComplete) 
                RETURNING id, task_description as TaskDescription, datetime_created as DateTimeCreated, is_complete as IsComplete";
            var newTodo = connection.QuerySingle<TodoModel>(sql, new
            {
                TaskDescription = taskDescription,
                DateTimeCreated = DateTime.UtcNow,
                IsComplete = false
            });
            return newTodo;
        }
    }
}