using ToDoApp.ClassLibrary;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace TodoAPI.Tests
{
    // ==================== UNIT TESTS ====================
    // These test business logic in isolation without database

    public class TodoServiceTests
    {
        private readonly TodoService _service;

        public TodoServiceTests()
        {
            _service = new TodoService();
        }

        [Fact]
        public void MarkTodoComplete_ShouldSetIsCompleteToTrue()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            _service.MarkTodoComplete(todos, 1, true);

            Assert.True(todos.First().IsComplete);
        }

        [Fact]
        public void MarkTodoComplete_ShouldSetIsCompleteToFalse()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = true }
            };

            _service.MarkTodoComplete(todos, 1, false);

            Assert.False(todos.First().IsComplete);
        }

        [Fact]
        public void MarkTodoComplete_WithNonExistentId_ShouldNotThrowException()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            _service.MarkTodoComplete(todos, 999, true);
            Assert.False(todos.First().IsComplete);
        }

        [Fact]
        public void UpdateTodoItem_ShouldUpdateTaskDescription()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Old description", IsComplete = false }
            };

            var result = _service.UpdateTodoItem(todos, 1, "New description");

            Assert.Equal("New description", result.First(t => t.Id == 1).TaskDescription);
        }

        [Fact]
        public void UpdateTodoItem_ShouldPreserveIsCompleteStatus()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test", IsComplete = true }
            };

            var result = _service.UpdateTodoItem(todos, 1, "Updated");

            Assert.True(result.First(t => t.Id == 1).IsComplete);
        }

        [Fact]
        public void UpdateTodoItem_WithNonExistentId_ShouldReturnOriginalList()
        {
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test", IsComplete = false }
            };
            var originalCount = todos.Count;

            var result = _service.UpdateTodoItem(todos, 999, "New description");

            Assert.Equal(originalCount, result.Count);
            Assert.Equal("Test", result.First().TaskDescription);
        }
    }

    // ==================== INTEGRATION TESTS ====================

    [Collection("Sequential")]
    public class TodoDataAccessIntegrationTests : IDisposable
    {
        private readonly TodoDatabaseDataAccess _dataAccess;
        private readonly string _testConnectionString = "Host=localhost;Database=todoappfundamental_test;Username=postgres;Password=test123";

        public TodoDataAccessIntegrationTests()
        {
            // Clean FIRST before creating data access
            CleanDatabase();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", _testConnectionString}
                })
                .Build();

            _dataAccess = new TodoDatabaseDataAccess(config);
        }

        private void CleanDatabase()
        {
            using var conn = new NpgsqlConnection(_testConnectionString);
            conn.Open();
            conn.Execute("TRUNCATE TABLE todo_items RESTART IDENTITY CASCADE");
            conn.Close();
        }

        public void Dispose()
        {
            CleanDatabase();
        }

        [Fact]
        public void CreateTodoItem_ShouldReturnTodoWithId()
        {
            var result = _dataAccess.CreateTodoItem("Test task");

            Assert.NotEqual(0, result.Id);
            Assert.Equal("Test task", result.TaskDescription);
            Assert.False(result.IsComplete);
        }

        [Fact]
        public void CreateTodoItem_MultipleTimes_ShouldReturnDifferentIds()
        {
            var result1 = _dataAccess.CreateTodoItem("Task 1");
            var result2 = _dataAccess.CreateTodoItem("Task 2");

            Assert.NotEqual(result1.Id, result2.Id);
        }

        [Fact]
        public void SaveTodoItems_ShouldPersistTodoToDatabase()
        {
            var created = _dataAccess.CreateTodoItem("Test task");

            var todos = _dataAccess.LoadTodoItems();
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            Assert.Single(result);
            Assert.Equal("Test task", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_ShouldPreserveIds()
        {
            var created = _dataAccess.CreateTodoItem("Test");
            var originalId = created.Id;

            var todos = _dataAccess.LoadTodoItems();
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            Assert.Equal(originalId, result.First().Id);
        }

        [Fact]
        public void SaveTodoItems_ShouldPreserveAllFields()
        {
            var created = _dataAccess.CreateTodoItem("Buy groceries");
            var originalId = created.Id;
            var dateTime = created.DateTimeCreated;

            var todos = _dataAccess.LoadTodoItems();
            todos.First().IsComplete = true;
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            var todo = result.First();
            Assert.Single(result);
            Assert.Equal(originalId, todo.Id);
            Assert.Equal("Buy groceries", todo.TaskDescription);
            Assert.True(todo.IsComplete);
            Assert.Equal(dateTime.Date, todo.DateTimeCreated.Date);
        }

        [Fact]
        public void SaveTodoItems_WithMultipleTodos_ShouldPreserveAll()
        {
            var todo1 = _dataAccess.CreateTodoItem("Task 1");
            var todo2 = _dataAccess.CreateTodoItem("Task 2");
            var todo3 = _dataAccess.CreateTodoItem("Task 3");

            var todos = _dataAccess.LoadTodoItems();
            todos.First(t => t.Id == todo2.Id).IsComplete = true;
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, t => t.TaskDescription == "Task 1" && t.Id == todo1.Id);
            Assert.Contains(result, t => t.TaskDescription == "Task 2" && t.Id == todo2.Id && t.IsComplete);
            Assert.Contains(result, t => t.TaskDescription == "Task 3" && t.Id == todo3.Id);
        }

        [Fact]
        public void LoadTodoItems_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            var result = _dataAccess.LoadTodoItems();
            Assert.Empty(result);
        }

        [Fact]
        public void LoadTodoItems_ShouldReturnAllTodos()
        {
            _dataAccess.CreateTodoItem("Task 1");
            _dataAccess.CreateTodoItem("Task 2");

            var result = _dataAccess.LoadTodoItems();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void SaveTodoItems_AfterUpdate_ShouldPersistChanges()
        {
            var created = _dataAccess.CreateTodoItem("Original");

            var loadedTodos = _dataAccess.LoadTodoItems();
            loadedTodos.First().TaskDescription = "Updated";
            _dataAccess.SaveTodoItems(loadedTodos);

            var result = _dataAccess.LoadTodoItems();
            Assert.Equal("Updated", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_AfterMarkingComplete_ShouldPreserveId()
        {
            var created = _dataAccess.CreateTodoItem("Test");
            var originalId = created.Id;

            var loadedTodos = _dataAccess.LoadTodoItems();
            loadedTodos.First().IsComplete = true;
            _dataAccess.SaveTodoItems(loadedTodos);

            var result = _dataAccess.LoadTodoItems();
            Assert.Equal(originalId, result.First().Id);
            Assert.True(result.First().IsComplete);
        }

        [Fact]
        public void SaveTodoItems_MultipleUpdates_ShouldPreserveSameId()
        {
            var created = _dataAccess.CreateTodoItem("Test");
            var originalId = created.Id;

            for (int i = 0; i < 5; i++)
            {
                var loaded = _dataAccess.LoadTodoItems();
                loaded.First().TaskDescription = $"Update {i}";
                _dataAccess.SaveTodoItems(loaded);
            }

            var result = _dataAccess.LoadTodoItems();
            Assert.Equal(originalId, result.First().Id);
            Assert.Equal("Update 4", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_WithRemovedTodo_ShouldNotPersistIt()
        {
            _dataAccess.CreateTodoItem("Keep");
            _dataAccess.CreateTodoItem("Remove");

            var loadedTodos = _dataAccess.LoadTodoItems();
            var toRemove = loadedTodos.First(t => t.TaskDescription == "Remove");
            loadedTodos.Remove(toRemove);
            _dataAccess.SaveTodoItems(loadedTodos);

            var result = _dataAccess.LoadTodoItems();
            Assert.Single(result);
            Assert.Equal("Keep", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_WithEmptyList_ShouldClearDatabase()
        {
            _dataAccess.CreateTodoItem("Test");

            _dataAccess.SaveTodoItems(new List<TodoModel>());

            var result = _dataAccess.LoadTodoItems();
            Assert.Empty(result);
        }
    }

    // ==================== FULL CRUD FLOW TESTS ====================

    [Collection("Sequential")]
    public class TodoCrudFlowTests : IDisposable
    {
        private readonly TodoDatabaseDataAccess _dataAccess;
        private readonly TodoService _service;
        private readonly string _testConnectionString = "Host=localhost;Database=todoappfundamental_test;Username=postgres;Password=test123";

        public TodoCrudFlowTests()
        {
            // Clean FIRST before creating data access
            CleanDatabase();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", _testConnectionString}
                })
                .Build();

            _dataAccess = new TodoDatabaseDataAccess(config);
            _service = new TodoService();
        }

        private void CleanDatabase()
        {
            using var conn = new NpgsqlConnection(_testConnectionString);
            conn.Open();
            conn.Execute("TRUNCATE TABLE todo_items RESTART IDENTITY CASCADE");
            conn.Close();
        }

        public void Dispose()
        {
            CleanDatabase();
        }

        [Fact]
        public void CompleteCrudFlow_ShouldWorkCorrectly()
        {
            var created = _dataAccess.CreateTodoItem("Buy groceries");
            var originalId = created.Id;

            var todos = _dataAccess.LoadTodoItems();
            Assert.Single(todos);
            Assert.Equal("Buy groceries", todos.First().TaskDescription);

            var updated = _service.UpdateTodoItem(todos, originalId, "Buy groceries and milk");
            _dataAccess.SaveTodoItems(updated);

            todos = _dataAccess.LoadTodoItems();
            Assert.Single(todos);
            Assert.Equal("Buy groceries and milk", todos.First().TaskDescription);
            Assert.Equal(originalId, todos.First().Id);

            _service.MarkTodoComplete(todos, originalId, true);
            _dataAccess.SaveTodoItems(todos);

            todos = _dataAccess.LoadTodoItems();
            Assert.True(todos.First().IsComplete);
            Assert.Equal(originalId, todos.First().Id);

            _service.MarkTodoComplete(todos, originalId, false);
            _dataAccess.SaveTodoItems(todos);

            todos = _dataAccess.LoadTodoItems();
            Assert.False(todos.First().IsComplete);
            Assert.Equal(originalId, todos.First().Id);

            todos.Clear();
            _dataAccess.SaveTodoItems(todos);

            todos = _dataAccess.LoadTodoItems();
            Assert.Empty(todos);
        }

        [Fact]
        public void MultipleTodos_CrudOperations_ShouldNotAffectOthers()
        {
            var todo1 = _dataAccess.CreateTodoItem("Task 1");
            var todo2 = _dataAccess.CreateTodoItem("Task 2");
            var todo3 = _dataAccess.CreateTodoItem("Task 3");

            var todos = _dataAccess.LoadTodoItems();
            Assert.Equal(3, todos.Count);

            todos = _service.UpdateTodoItem(todos, todo2.Id, "Task 2 Updated");
            _dataAccess.SaveTodoItems(todos);

            todos = _dataAccess.LoadTodoItems();
            Assert.Equal(3, todos.Count);
            Assert.Contains(todos, t => t.TaskDescription == "Task 1" && t.Id == todo1.Id);
            Assert.Contains(todos, t => t.TaskDescription == "Task 2 Updated" && t.Id == todo2.Id);
            Assert.Contains(todos, t => t.TaskDescription == "Task 3" && t.Id == todo3.Id);

            todos.Remove(todos.First(t => t.Id == todo2.Id));
            _dataAccess.SaveTodoItems(todos);

            todos = _dataAccess.LoadTodoItems();
            Assert.Equal(2, todos.Count);
            Assert.Contains(todos, t => t.Id == todo1.Id);
            Assert.Contains(todos, t => t.Id == todo3.Id);
            Assert.DoesNotContain(todos, t => t.Id == todo2.Id);
        }
    }
}