using Dapper;
using Npgsql;

namespace ToDoApp.ClassLibrary
{
    public class TodoDatabaseDataAccess : ITodoDataAccess
    {
        private readonly string _connectionString = "Host=localhost;Database=todoappfundametal;Username=postgres;Password=test123";
        //private readonly string _connectionString = "Host=192.168.1.118;Database=todoapp;Username=postgres;Password=test123";

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