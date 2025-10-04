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
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            // Act
            _service.MarkTodoComplete(todos, 1, true);

            // Assert
            Assert.True(todos.First().IsComplete);
        }

        [Fact]
        public void MarkTodoComplete_ShouldSetIsCompleteToFalse()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = true }
            };

            // Act
            _service.MarkTodoComplete(todos, 1, false);

            // Assert
            Assert.False(todos.First().IsComplete);
        }

        [Fact]
        public void MarkTodoComplete_WithNonExistentId_ShouldNotThrowException()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            // Act & Assert - should not throw
            _service.MarkTodoComplete(todos, 999, true);
            Assert.False(todos.First().IsComplete); // Original should be unchanged
        }

        [Fact]
        public void UpdateTodoItem_ShouldUpdateTaskDescription()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Old description", IsComplete = false }
            };

            // Act
            var result = _service.UpdateTodoItem(todos, 1, "New description");

            // Assert
            Assert.Equal("New description", result.First(t => t.Id == 1).TaskDescription);
        }

        [Fact]
        public void UpdateTodoItem_ShouldPreserveIsCompleteStatus()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test", IsComplete = true }
            };

            // Act
            var result = _service.UpdateTodoItem(todos, 1, "Updated");

            // Assert
            Assert.True(result.First(t => t.Id == 1).IsComplete);
        }

        [Fact]
        public void UpdateTodoItem_WithNonExistentId_ShouldReturnOriginalList()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test", IsComplete = false }
            };
            var originalCount = todos.Count;

            // Act
            var result = _service.UpdateTodoItem(todos, 999, "New description");

            // Assert
            Assert.Equal(originalCount, result.Count);
            Assert.Equal("Test", result.First().TaskDescription); // Unchanged
        }
    }

    // ==================== INTEGRATION TESTS ====================
    // These test YOUR ACTUAL TodoDatabaseDataAccess with real database

    public class TodoDataAccessIntegrationTests : IDisposable
    {
        private readonly TodoDatabaseDataAccess _dataAccess;
        private readonly string _testConnectionString;

        public TodoDataAccessIntegrationTests()
        {
            // Create test configuration
            _testConnectionString = "Host=localhost;Database=todoappfundametal_test;Username=postgres;Password=test123";

            var inMemorySettings = new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", _testConnectionString}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _dataAccess = new TodoDatabaseDataAccess(configuration);

            // Clean database before each test
            CleanDatabase();
        }

        private void CleanDatabase()
        {
            using var connection = new NpgsqlConnection(_testConnectionString);
            connection.Execute("DELETE FROM todo_items");
            // Reset sequence if needed
            connection.Execute("ALTER SEQUENCE todo_items_id_seq RESTART WITH 1");
        }

        public void Dispose()
        {
            // Clean up after each test
            CleanDatabase();
        }

        // ===== CREATE TESTS =====

        [Fact]
        public void CreateTodoItem_ShouldReturnTodoWithId()
        {
            // Act
            var result = _dataAccess.CreateTodoItem("Test task");

            // Assert
            Assert.NotEqual(0, result.Id);
            Assert.Equal("Test task", result.TaskDescription);
            Assert.False(result.IsComplete);
        }

        [Fact]
        public void CreateTodoItem_MultipleTimes_ShouldReturnDifferentIds()
        {
            // Act
            var result1 = _dataAccess.CreateTodoItem("Task 1");
            var result2 = _dataAccess.CreateTodoItem("Task 2");

            // Assert
            Assert.NotEqual(result1.Id, result2.Id);
        }

        [Fact]
        public void SaveTodoItems_ShouldPersistTodoToDatabase()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel
                {
                    Id = 1,
                    TaskDescription = "Test task",
                    DateTimeCreated = DateTime.Now,
                    IsComplete = false
                }
            };

            // Act
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            // Assert
            Assert.Single(result);
            Assert.Equal("Test task", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_ShouldPreserveIds()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 5, TaskDescription = "Test", DateTimeCreated = DateTime.Now, IsComplete = false }
            };

            // Act
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            // Assert
            // THIS TEST WILL FAIL because SaveTodoItems doesn't include id in INSERT
            Assert.Equal(5, result.First().Id);
        }

        [Fact]
        public void SaveTodoItems_ShouldPreserveAllFields()
        {
            // Arrange
            var dateTime = DateTime.Now;
            var todos = new List<TodoModel>
            {
                new TodoModel
                {
                    Id = 10,
                    TaskDescription = "Buy groceries",
                    DateTimeCreated = dateTime,
                    IsComplete = true
                }
            };

            // Act
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            // Assert
            var todo = result.First();
            // THIS TEST WILL FAIL because SaveTodoItems doesn't preserve ID
            Assert.Equal(10, todo.Id);
            Assert.Equal("Buy groceries", todo.TaskDescription);
            Assert.True(todo.IsComplete);
            Assert.Equal(dateTime.Date, todo.DateTimeCreated.Date);
        }

        [Fact]
        public void SaveTodoItems_WithMultipleTodos_ShouldPreserveAll()
        {
            // Arrange
            var todos = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Task 1", DateTimeCreated = DateTime.Now, IsComplete = false },
                new TodoModel { Id = 2, TaskDescription = "Task 2", DateTimeCreated = DateTime.Now, IsComplete = true },
                new TodoModel { Id = 3, TaskDescription = "Task 3", DateTimeCreated = DateTime.Now, IsComplete = false }
            };

            // Act
            _dataAccess.SaveTodoItems(todos);
            var result = _dataAccess.LoadTodoItems();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, t => t.TaskDescription == "Task 1");
            Assert.Contains(result, t => t.TaskDescription == "Task 2");
            Assert.Contains(result, t => t.TaskDescription == "Task 3");
            // IDs won't match because of the bug - but descriptions will
        }

        // ===== READ TESTS =====

        [Fact]
        public void LoadTodoItems_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Act
            var result = _dataAccess.LoadTodoItems();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void LoadTodoItems_ShouldReturnAllTodos()
        {
            // Arrange - Use CreateTodoItem which works correctly
            _dataAccess.CreateTodoItem("Task 1");
            _dataAccess.CreateTodoItem("Task 2");

            // Act
            var result = _dataAccess.LoadTodoItems();

            // Assert
            Assert.Equal(2, result.Count);
        }

        // ===== UPDATE TESTS =====

        [Fact]
        public void SaveTodoItems_AfterUpdate_ShouldPersistChanges()
        {
            // Arrange - Create using CreateTodoItem
            var created = _dataAccess.CreateTodoItem("Original");

            // Act - Update and save
            var loadedTodos = _dataAccess.LoadTodoItems();
            loadedTodos.First().TaskDescription = "Updated";
            _dataAccess.SaveTodoItems(loadedTodos);

            // Assert
            var result = _dataAccess.LoadTodoItems();
            Assert.Equal("Updated", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_AfterMarkingComplete_ShouldPreserveId()
        {
            // Arrange - Create using CreateTodoItem
            var created = _dataAccess.CreateTodoItem("Test");
            var originalId = created.Id;

            // Act - Mark complete and save
            var loadedTodos = _dataAccess.LoadTodoItems();
            loadedTodos.First().IsComplete = true;
            _dataAccess.SaveTodoItems(loadedTodos);

            // Assert
            var result = _dataAccess.LoadTodoItems();
            // THIS TEST WILL FAIL - ID changes after SaveTodoItems
            Assert.Equal(originalId, result.First().Id);
            Assert.True(result.First().IsComplete);
        }

        [Fact]
        public void SaveTodoItems_MultipleUpdates_ShouldPreserveSameId()
        {
            // Arrange - Create using CreateTodoItem
            var created = _dataAccess.CreateTodoItem("Test");
            var originalId = created.Id;

            // Act - Update multiple times
            for (int i = 0; i < 5; i++)
            {
                var loaded = _dataAccess.LoadTodoItems();
                loaded.First().TaskDescription = $"Update {i}";
                _dataAccess.SaveTodoItems(loaded);
            }

            // Assert
            var result = _dataAccess.LoadTodoItems();
            // THIS TEST WILL FAIL - ID changes with each save
            Assert.Equal(originalId, result.First().Id);
            Assert.Equal("Update 4", result.First().TaskDescription);
        }

        // ===== DELETE TESTS =====

        [Fact]
        public void SaveTodoItems_WithRemovedTodo_ShouldNotPersistIt()
        {
            // Arrange
            _dataAccess.CreateTodoItem("Keep");
            _dataAccess.CreateTodoItem("Remove");

            // Act - Remove one todo
            var loadedTodos = _dataAccess.LoadTodoItems();
            var toRemove = loadedTodos.First(t => t.TaskDescription == "Remove");
            loadedTodos.Remove(toRemove);
            _dataAccess.SaveTodoItems(loadedTodos);

            // Assert
            var result = _dataAccess.LoadTodoItems();
            Assert.Single(result);
            Assert.Equal("Keep", result.First().TaskDescription);
        }

        [Fact]
        public void SaveTodoItems_WithEmptyList_ShouldClearDatabase()
        {
            // Arrange
            _dataAccess.CreateTodoItem("Test");

            // Act - Save empty list
            _dataAccess.SaveTodoItems(new List<TodoModel>());

            // Assert
            var result = _dataAccess.LoadTodoItems();
            Assert.Empty(result);
        }
    }

    // ==================== FULL CRUD FLOW TESTS ====================
    // These test the complete workflow end-to-end with YOUR actual code

    public class TodoCrudFlowTests : IDisposable
    {
        private readonly TodoDatabaseDataAccess _dataAccess;
        private readonly TodoService _service;
        private readonly string _testConnectionString;

        public TodoCrudFlowTests()
        {
            _testConnectionString = "Host=localhost;Database=todoappfundametal_test;Username=postgres;Password=test123";

            var inMemorySettings = new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", _testConnectionString}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _dataAccess = new TodoDatabaseDataAccess(configuration);
            _service = new TodoService();
            CleanDatabase();
        }

        private void CleanDatabase()
        {
            using var connection = new NpgsqlConnection(_testConnectionString);
            connection.Execute("DELETE FROM todo_items");
            connection.Execute("ALTER SEQUENCE todo_items_id_seq RESTART WITH 1");
        }

        public void Dispose()
        {
            CleanDatabase();
        }

        [Fact]
        public void CompleteCrudFlow_ShouldWorkCorrectly()
        {
            // CREATE
            var created = _dataAccess.CreateTodoItem("Buy groceries");
            var originalId = created.Id;

            // READ
            var todos = _dataAccess.LoadTodoItems();
            Assert.Single(todos);
            Assert.Equal("Buy groceries", todos.First().TaskDescription);

            // UPDATE
            var updated = _service.UpdateTodoItem(todos, originalId, "Buy groceries and milk");
            _dataAccess.SaveTodoItems(updated);
            todos = _dataAccess.LoadTodoItems();
            Assert.Equal("Buy groceries and milk", todos.First().TaskDescription);
            // THIS WILL FAIL - ID changes after save
            Assert.Equal(originalId, todos.First().Id);

            // MARK COMPLETE
            var currentId = todos.First().Id;
            _service.MarkTodoComplete(todos, currentId, true);
            _dataAccess.SaveTodoItems(todos);
            todos = _dataAccess.LoadTodoItems();
            Assert.True(todos.First().IsComplete);
            // THIS WILL FAIL - ID changes again
            Assert.Equal(currentId, todos.First().Id);

            // MARK INCOMPLETE
            currentId = todos.First().Id;
            _service.MarkTodoComplete(todos, currentId, false);
            _dataAccess.SaveTodoItems(todos);
            todos = _dataAccess.LoadTodoItems();
            Assert.False(todos.First().IsComplete);
            // THIS WILL FAIL - ID changes again
            Assert.Equal(currentId, todos.First().Id);

            // DELETE
            todos.Clear();
            _dataAccess.SaveTodoItems(todos);
            todos = _dataAccess.LoadTodoItems();
            Assert.Empty(todos);
        }

        [Fact]
        public void MultipleTodos_CrudOperations_ShouldNotAffectOthers()
        {
            // CREATE multiple
            _dataAccess.CreateTodoItem("Task 1");
            _dataAccess.CreateTodoItem("Task 2");
            _dataAccess.CreateTodoItem("Task 3");

            // UPDATE one
            var todos = _dataAccess.LoadTodoItems();
            var task2 = todos.First(t => t.TaskDescription == "Task 2");
            todos = _service.UpdateTodoItem(todos, task2.Id, "Task 2 Updated");
            _dataAccess.SaveTodoItems(todos);

            // VERIFY
            todos = _dataAccess.LoadTodoItems();
            Assert.Equal(3, todos.Count);
            Assert.Contains(todos, t => t.TaskDescription == "Task 1");
            Assert.Contains(todos, t => t.TaskDescription == "Task 2 Updated");
            Assert.Contains(todos, t => t.TaskDescription == "Task 3");
        }
    }
}