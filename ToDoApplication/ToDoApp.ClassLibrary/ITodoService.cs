
namespace ToDoApp.ClassLibrary
{
    public interface ITodoService
    {
        List<TodoModel> CreateTodoItem(List<TodoModel> todoItems, string taskDescription);
        List<TodoModel> DeleteTodoItem(List<TodoModel> todoItems, int id);
        List<TodoModel> ListAllTodoItems(List<TodoModel> todoItems);
        List<TodoModel> UpdateTodoItem(List<TodoModel> todoItems, int id, string newDescription);

        void MarkTodoComplete(List<TodoModel> todoItems, int id, bool isComplete);
    }
}