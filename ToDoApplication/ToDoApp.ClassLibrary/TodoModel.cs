namespace ToDoApp.ClassLibrary;

public class TodoModel
{
    private static int _nextId = 1; // Static counter
    public int Id { get; set; }

    public string? TaskDescription { get; set; }

    public DateTime DateTimeCreated { get; set; }

    public bool IsComplete { get; set; } = false;

    public TodoModel()
    {
        Id = _nextId++; // Auto-assign and increment
    }
}



