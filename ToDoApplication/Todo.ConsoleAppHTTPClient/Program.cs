using System.Text;
using System.Text.Json;

// HTTP client for API calls
var httpClient = new HttpClient();
//httpClient.BaseAddress = new Uri("https://localhost:7149/");
httpClient.BaseAddress = new Uri("http://192.168.1.118:5000/");

bool keepRunning = true;
while (keepRunning)
{
    string operation = ShowMenuMain();
    if (operation == "q")
        keepRunning = false;
    else
        await MainMenuLogic(operation);
}

string ShowMenuMain()
{
    string menuInput;
    do
    {
        Console.WriteLine("Welcome TodoList Api Client Version");
        Console.WriteLine("1. Add Todo item to List");
        Console.WriteLine("2. Read Todo items");
        Console.WriteLine("3. Update Todo item");
        Console.WriteLine("4. Delete Todo item from List");
        Console.WriteLine("5. Mark Todo item as Complete/Incomplete");
        Console.WriteLine("q - to quit");
        menuInput = Console.ReadLine();
    } while (menuInput != "1" && menuInput != "2" && menuInput != "3" && menuInput != "4" && menuInput != "5" && menuInput != "q");
    return menuInput;
}

async Task MainMenuLogic(string operation)
{
    switch (operation)
    {
        case "1":
            Console.WriteLine("Enter task description:");
            string taskDescription = Console.ReadLine();
            await CreateTodoItem(taskDescription);
            break;

        case "2":
            await ListAllTodoItems();
            break;

        case "3":
            Console.WriteLine("Enter task ID to update:");
            if (int.TryParse(Console.ReadLine(), out int updateId))
            {
                Console.WriteLine("Enter new task description:");
                string newDescription = Console.ReadLine();
                await UpdateTodoItem(updateId, newDescription);
            }
            else
            {
                Console.WriteLine("Invalid ID entered.");
            }
            break;

        case "4":
            Console.WriteLine("Enter task ID to delete:");
            if (int.TryParse(Console.ReadLine(), out int deleteId))
            {
                await DeleteTodoItem(deleteId);
            }
            else
            {
                Console.WriteLine("Invalid ID entered.");
            }
            break;

        case "5":
            Console.WriteLine("Enter task ID to mark complete/incomplete:");
            if (int.TryParse(Console.ReadLine(), out int completeId))
            {
                // First get the current item to show its status
                var currentItem = await GetTodoItem(completeId);
                if (currentItem != null)
                {
                    Console.WriteLine($"Current status: {(currentItem.IsComplete ? "Complete" : "Pending")}");
                    Console.WriteLine("Mark as (c)omplete or (p)ending?");
                    string statusChoice = Console.ReadLine()?.ToLower();

                    bool newStatus = statusChoice == "c";
                    await MarkTodoComplete(completeId, newStatus);
                }
                else
                {
                    Console.WriteLine($"Task with ID {completeId} not found.");
                }
            }
            else
            {
                Console.WriteLine("Invalid ID entered.");
            }
            break;

        default:
            Console.WriteLine("Invalid operation");
            break;
    }
}

async Task CreateTodoItem(string taskDescription)
{
    try
    {
        var request = new { TaskDescription = taskDescription };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("api/TodoItems", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Task added successfully!");
        }
        else
        {
            Console.WriteLine($"Error creating task: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task ListAllTodoItems()
{
    try
    {
        var response = await httpClient.GetAsync("api/TodoItems");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var todos = JsonSerializer.Deserialize<List<TodoModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (todos != null && todos.Any())
            {
                foreach (var item in todos)
                {
                    string status = item.IsComplete ? "Complete" : "Pending";
                    Console.WriteLine($"ID:{item.Id}, Task:{item.TaskDescription}, Status:{status}, Created:{item.DateTimeCreated}");
                }
            }
            else
            {
                Console.WriteLine("No todo items found.");
            }
        }
        else
        {
            Console.WriteLine($"Error retrieving tasks: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task UpdateTodoItem(int id, string newDescription)
{
    try
    {
        var request = new { TaskDescription = newDescription };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PutAsync($"api/TodoItems/{id}", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Task updated successfully!");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Task with ID {id} not found.");
        }
        else
        {
            Console.WriteLine($"Error updating task: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task DeleteTodoItem(int id)
{
    try
    {
        var response = await httpClient.DeleteAsync($"api/TodoItems/{id}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Task deleted successfully!");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Task with ID {id} not found.");
        }
        else
        {
            Console.WriteLine($"Error deleting task: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task<TodoModel?> GetTodoItem(int id)
{
    try
    {
        var response = await httpClient.GetAsync($"api/TodoItems/{id}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TodoModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return null;
    }
}

async Task MarkTodoComplete(int id, bool isComplete)
{
    try
    {
        string endpoint = isComplete ? $"api/TodoItems/{id}/complete" : $"api/TodoItems/{id}/incomplete";
        var response = await httpClient.PutAsync(endpoint, null);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Task marked as {(isComplete ? "Complete" : "Pending")}!");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Task with ID {id} not found.");
        }
        else
        {
            Console.WriteLine($"Error updating task: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// TodoModel class for deserialization
public class TodoModel
{
    public int Id { get; set; }
    public string? TaskDescription { get; set; }
    public DateTime DateTimeCreated { get; set; }
    public bool IsComplete { get; set; }
}