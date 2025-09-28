
namespace ToDoApp.ClassLibrary;

public interface ITodoDataAccess
{
    List<TodoModel> LoadTodoItems();
    void SaveTodoItems(List<TodoModel> todoListitems);
    TodoModel CreateTodoItem(string taskDescription);
}