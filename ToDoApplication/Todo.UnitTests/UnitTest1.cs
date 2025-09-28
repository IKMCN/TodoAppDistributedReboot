
using ToDoApp.ClassLibrary;


namespace ToDoApp.Tests
{
    public class TodoServiceTests
    {
        private readonly TodoService _todoService;

        public TodoServiceTests()
        {
            _todoService = new TodoService();
        }

        [Fact]
        public void CreateTodoItem_ShouldAddNewTodoToList()
        {
            // Arrange
            var todoItems = new List<TodoModel>();
            var taskDescription = "Test todo item";

            // Act
            var result = _todoService.CreateTodoItem(todoItems, taskDescription);

            // Assert
            Assert.Single(result);
            Assert.Equal(taskDescription, result.First().TaskDescription);
            Assert.False(result.First().IsComplete);
            Assert.True(result.First().DateTimeCreated > DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public void UpdateTodoItem_ExistingId_ShouldUpdateTaskDescription()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Original task", DateTimeCreated = DateTime.Now.AddDays(-1) }
            };
            var newDescription = "Updated task description";
            var beforeUpdate = DateTime.Now;

            // Act
            var result = _todoService.UpdateTodoItem(todoItems, 1, newDescription);

            // Assert
            var updatedItem = result.First(x => x.Id == 1);
            Assert.Equal(newDescription, updatedItem.TaskDescription);
            Assert.True(updatedItem.DateTimeCreated >= beforeUpdate);
        }

        [Fact]
        public void UpdateTodoItem_NonExistingId_ShouldNotChangeList()
        {
            // Arrange
            var originalTask = "Original task";
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = originalTask }
            };

            // Act
            var result = _todoService.UpdateTodoItem(todoItems, 999, "Updated task");

            // Assert
            Assert.Single(result);
            Assert.Equal(originalTask, result.First().TaskDescription);
        }

        [Fact]
        public void DeleteTodoItem_ExistingId_ShouldRemoveFromList()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Task 1" },
                new TodoModel { Id = 2, TaskDescription = "Task 2" }
            };

            // Act
            var result = _todoService.DeleteTodoItem(todoItems, 1);

            // Assert
            Assert.Single(result);
            Assert.DoesNotContain(result, x => x.Id == 1);
            Assert.Contains(result, x => x.Id == 2);
        }

        [Fact]
        public void DeleteTodoItem_NonExistingId_ShouldNotChangeList()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Task 1" }
            };

            // Act
            var result = _todoService.DeleteTodoItem(todoItems, 999);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().Id);
        }
    }

    // Integration tests for TodoDatabaseDataAccess
    // Note: These require a test database setup
    public class TodoDatabaseDataAccessTests : IDisposable
    {
        private readonly TodoDatabaseDataAccess _dataAccess;
        private readonly List<int> _createdTodoIds;

        public TodoDatabaseDataAccessTests()
        {
            _dataAccess = new TodoDatabaseDataAccess();
            _createdTodoIds = new List<int>();
        }

        [Fact]
        public void CreateTodoItem_ShouldReturnTodoWithValidId()
        {
            // Arrange
            var taskDescription = "Test database todo";

            // Act
            var result = _dataAccess.CreateTodoItem(taskDescription);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(taskDescription, result.TaskDescription);
            Assert.False(result.IsComplete);
            Assert.True(result.DateTimeCreated > DateTime.UtcNow.AddMinutes(-1));

            // Track for cleanup
            _createdTodoIds.Add(result.Id);
        }

        [Fact]
        public void LoadTodoItems_ShouldReturnAllTodos()
        {
            // Arrange - Create a test todo first
            var testTodo = _dataAccess.CreateTodoItem("Test load todos");
            _createdTodoIds.Add(testTodo.Id);

            // Act
            var result = _dataAccess.LoadTodoItems();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result, x => x.Id == testTodo.Id);
        }

        [Fact]
        public void SaveTodoItems_ShouldReplaceAllTodos()
        {
            // Arrange
            var originalTodos = _dataAccess.LoadTodoItems();
            var testTodos = new List<TodoModel>
            {
                new TodoModel { TaskDescription = "Test Save 1", DateTimeCreated = DateTime.UtcNow, IsComplete = false },
                new TodoModel { TaskDescription = "Test Save 2", DateTimeCreated = DateTime.UtcNow, IsComplete = true }
            };

            try
            {
                // Act
                _dataAccess.SaveTodoItems(testTodos);
                var result = _dataAccess.LoadTodoItems();

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Contains(result, x => x.TaskDescription == "Test Save 1");
                Assert.Contains(result, x => x.TaskDescription == "Test Save 2");
            }
            finally
            {
                // Restore original todos
                _dataAccess.SaveTodoItems(originalTodos);
            }
        }

        public void Dispose()
        {
            // Cleanup any todos created during tests
            if (_createdTodoIds.Any())
            {
                var allTodos = _dataAccess.LoadTodoItems();
                var remainingTodos = allTodos.Where(x => !_createdTodoIds.Contains(x.Id)).ToList();
                _dataAccess.SaveTodoItems(remainingTodos);
            }
        }
    }

    // Additional test class for edge cases and error scenarios
    public class TodoServiceEdgeCaseTests
    {
        private readonly TodoService _todoService;

        public TodoServiceEdgeCaseTests()
        {
            _todoService = new TodoService();
        }

        [Fact]
        public void CreateTodoItem_WithEmptyList_ShouldWork()
        {
            // Arrange
            var todoItems = new List<TodoModel>();

            // Act
            var result = _todoService.CreateTodoItem(todoItems, "New task");

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void CreateTodoItem_WithNullDescription_ShouldStillCreate()
        {
            // Arrange
            var todoItems = new List<TodoModel>();

            // Act
            var result = _todoService.CreateTodoItem(todoItems, null);

            // Assert
            Assert.Single(result);
            Assert.Null(result.First().TaskDescription);
        }

        [Fact]
        public void MarkTodoComplete_ExistingId_ShouldSetIsComplete()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            // Act
            _todoService.MarkTodoComplete(todoItems, 1, true);

            // Assert
            Assert.True(todoItems.First().IsComplete);
        }

        [Fact]
        public void MarkTodoComplete_NonExistingId_ShouldNotThrow()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Test task", IsComplete = false }
            };

            // Act & Assert - Should not throw
            _todoService.MarkTodoComplete(todoItems, 999, true);
            Assert.False(todoItems.First().IsComplete); // Should remain unchanged
        }

        [Fact]
        public void ListAllTodoItems_ShouldReturnSameList()
        {
            // Arrange
            var todoItems = new List<TodoModel>
            {
                new TodoModel { Id = 1, TaskDescription = "Task 1" },
                new TodoModel { Id = 2, TaskDescription = "Task 2" }
            };

            // Act
            var result = _todoService.ListAllTodoItems(todoItems);

            // Assert
            Assert.Equal(todoItems, result);
            Assert.Equal(2, result.Count);
        }
    }
}