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
            // Simple approach: clear table and re-insert all items
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Execute("DELETE FROM todo_items");
            foreach (var item in todoItems)
            {
                // NOTE: Still has the ID bug - not including id field
                var sql = "INSERT INTO todo_items (task_description, datetime_created, is_complete) VALUES (@TaskDescription, @DateTimeCreated, @IsComplete)";
                connection.Execute(sql, item);
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