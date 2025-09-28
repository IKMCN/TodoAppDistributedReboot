namespace ToDoApp.ClassLibrary;

public class TodoService : ITodoService
{
    public List<TodoModel> ListAllTodoItems(List<TodoModel> todoItems)
    {
        return todoItems;
    }

    public List<TodoModel> UpdateTodoItem(List<TodoModel> todoItems, int id, string newDescription)
    {
        var item = todoItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            item.TaskDescription = newDescription;
            item.DateTimeCreated = DateTime.Now;
        }
        // No console output - let caller handle "not found" case
        return todoItems;
    }

    public List<TodoModel> DeleteTodoItem(List<TodoModel> todoItems, int id)
    {

        var item = todoItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            todoItems.Remove(item);
        }

        return todoItems;

    }

    public List<TodoModel> CreateTodoItem(List<TodoModel> todoItems, string taskDescription)
    {
        TodoModel newListItem = new TodoModel();


        newListItem.TaskDescription = taskDescription;

        newListItem.DateTimeCreated = DateTime.Now;

        todoItems.Add(newListItem);

        return todoItems;
    }

    public void MarkTodoComplete(List<TodoModel> todoItems, int id, bool isComplete)
    {
        var item = todoItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            item.IsComplete = isComplete;
        }
    }
}
