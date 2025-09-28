using ToDoApp.ClassLibrary;

ITodoService todoService = new TodoService();
//ITodoDataAccess todoDataAccess = new TodoDataAccess();//Text file storage
ITodoDataAccess todoDataAccess = new TodoDatabaseDataAccess(); // Database implementation
List<TodoModel> todoListitems = todoDataAccess.LoadTodoItems();

bool keepRunning = true;
while (keepRunning)
{
    string operation = ShowMenuMain();
    if (operation == "q")
        keepRunning = false;
    else
        MainMenuLogic(operation);
}

string ShowMenuMain()
{
    string menuInput;
    do
    {
        Console.WriteLine("Welcome TodoList Direct Client Version");
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

void MainMenuLogic(string operation)
{
    switch (operation)
    {
        case "1":
            Console.WriteLine("Enter task description:");
            string taskDescription = Console.ReadLine();
            todoListitems = todoService.CreateTodoItem(todoListitems, taskDescription);
            todoDataAccess.SaveTodoItems(todoListitems);
            Console.WriteLine("Task added successfully!");
            break;

        case "2":
            if (todoListitems.Any())
            {
                foreach (var item in todoListitems)
                {
                    string status = item.IsComplete ? "Complete" : "Pending";
                    Console.WriteLine($"ID:{item.Id}, Task:{item.TaskDescription}, Status:{status}, Created:{item.DateTimeCreated}");
                }
            }
            else
            {
                Console.WriteLine("No todo items found.");
            }
            break;

        case "3":
            Console.WriteLine("Enter task ID to update:");
            if (int.TryParse(Console.ReadLine(), out int updateId))
            {
                Console.WriteLine("Enter new task description:");
                string newDescription = Console.ReadLine();

                var originalCount = todoListitems.Count;
                todoListitems = todoService.UpdateTodoItem(todoListitems, updateId, newDescription);

                // Check if item was found and updated
                var updatedItem = todoListitems.FirstOrDefault(x => x.Id == updateId);
                if (updatedItem != null && updatedItem.TaskDescription == newDescription)
                {
                    todoDataAccess.SaveTodoItems(todoListitems);
                    Console.WriteLine("Task updated successfully!");
                }
                else
                {
                    Console.WriteLine($"Task with ID {updateId} not found.");
                }
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
                var originalCount = todoListitems.Count;
                todoListitems = todoService.DeleteTodoItem(todoListitems, deleteId);

                if (todoListitems.Count < originalCount)
                {
                    todoDataAccess.SaveTodoItems(todoListitems);
                    Console.WriteLine("Task deleted successfully!");
                }
                else
                {
                    Console.WriteLine($"Task with ID {deleteId} not found.");
                }
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
                var item = todoListitems.FirstOrDefault(x => x.Id == completeId);
                if (item != null)
                {
                    Console.WriteLine($"Current status: {(item.IsComplete ? "Complete" : "Pending")}");
                    Console.WriteLine("Mark as (c)omplete or (p)ending?");
                    string statusChoice = Console.ReadLine()?.ToLower();

                    bool newStatus = statusChoice == "c";
                    todoService.MarkTodoComplete(todoListitems, completeId, newStatus);
                    todoDataAccess.SaveTodoItems(todoListitems);
                    Console.WriteLine($"Task marked as {(newStatus ? "Complete" : "Pending")}!");
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