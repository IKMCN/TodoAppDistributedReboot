using Microsoft.AspNetCore.Mvc;
using ToDoApp.ClassLibrary;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly ITodoDataAccess _todoDataAccess;

    public TodoItemsController(ITodoService todoService, ITodoDataAccess todoDataAccess)
    {
        _todoService = todoService;
        _todoDataAccess = todoDataAccess;
    }

    // GET: api/TodoItems
    [HttpGet]
    public ActionResult<IEnumerable<TodoModel>> GetTodoItems()
    {
        try
        {
            var todos = _todoDataAccess.LoadTodoItems();
            return Ok(todos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public ActionResult<TodoModel> GetTodoItem(int id)
    {
        try
        {
            var todos = _todoDataAccess.LoadTodoItems();
            var todo = todos.FirstOrDefault(t => t.Id == id);

            if (todo == null)
            {
                return NotFound();
            }

            return Ok(todo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/TodoItems
    [HttpPost]
    public ActionResult<TodoModel> PostTodoItem(CreateTodoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TaskDescription))
            {
                return BadRequest("Task description is required");
            }

            // Use the new method that returns the actual database ID
            var newTodo = _todoDataAccess.CreateTodoItem(request.TaskDescription);

            return CreatedAtAction(nameof(GetTodoItem), new { id = newTodo.Id }, newTodo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public IActionResult PutTodoItem(int id, UpdateTodoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TaskDescription))
            {
                return BadRequest("Task description is required");
            }

            var todos = _todoDataAccess.LoadTodoItems();
            var originalCount = todos.Count;
            var existingTodo = todos.FirstOrDefault(t => t.Id == id);

            if (existingTodo == null)
            {
                return NotFound();
            }

            todos = _todoService.UpdateTodoItem(todos, id, request.TaskDescription);
            _todoDataAccess.SaveTodoItems(todos);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/TodoItems/5/complete
    [HttpPut("{id}/complete")]
    public IActionResult CompleteTodoItem(int id)
    {
        try
        {
            var todos = _todoDataAccess.LoadTodoItems();
            var existingTodo = todos.FirstOrDefault(t => t.Id == id);

            if (existingTodo == null)
            {
                return NotFound();
            }

            _todoService.MarkTodoComplete(todos, id, true);
            _todoDataAccess.SaveTodoItems(todos);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/TodoItems/5/incomplete
    [HttpPut("{id}/incomplete")]
    public IActionResult IncompleteTodoItem(int id)
    {
        try
        {
            var todos = _todoDataAccess.LoadTodoItems();
            var existingTodo = todos.FirstOrDefault(t => t.Id == id);

            if (existingTodo == null)
            {
                return NotFound();
            }

            _todoService.MarkTodoComplete(todos, id, false);
            _todoDataAccess.SaveTodoItems(todos);

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public IActionResult DeleteTodoItem(int id)
    {
        try
        {
            var todos = _todoDataAccess.LoadTodoItems();
            var originalCount = todos.Count;

            todos = _todoService.DeleteTodoItem(todos, id);

            if (todos.Count == originalCount)
            {
                return NotFound();
            }

            _todoDataAccess.SaveTodoItems(todos);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

// DTOs for request bodies
public record CreateTodoRequest(string TaskDescription);
public record UpdateTodoRequest(string TaskDescription);