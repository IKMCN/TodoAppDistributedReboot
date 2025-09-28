namespace ToDoApp.ClassLibrary;

public class TodoDataAccess : ITodoDataAccess
{
    public void SaveTodoItems(List<TodoModel> todoListitems)
    {
        try
        {
            string fileName = "todoListItems.txt";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (var item in todoListitems)
                {
                    // Format: ID|TaskDescription|DateTimeCreated|IsComplete
                    writer.WriteLine($"{item.Id}|{item.TaskDescription}|{item.DateTimeCreated}|{item.IsComplete}");
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public List<TodoModel> LoadTodoItems()
    {
        List<TodoModel> loadedTodoListItems = new List<TodoModel>();
        string fileName = "todoListItems.txt";

        try
        {
            if (File.Exists(fileName))
            {
                string[] lines = File.ReadAllLines(fileName);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 4) // Make sure we have all parts
                        {
                            TodoModel todoListItem = new TodoModel();
                            todoListItem.Id = int.Parse(parts[0]);
                            todoListItem.TaskDescription = parts[1];
                            todoListItem.DateTimeCreated = DateTime.Parse(parts[2]);
                            todoListItem.IsComplete = bool.Parse(parts[3]);
                            loadedTodoListItems.Add(todoListItem);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }

        return loadedTodoListItems;
    }

    public TodoModel CreateTodoItem(string taskDescription)
    {
        throw new NotImplementedException();
    }
}