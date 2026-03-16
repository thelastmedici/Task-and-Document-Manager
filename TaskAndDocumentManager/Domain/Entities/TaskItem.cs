namespace TaskAndDocumentManager.Domain.Entities;

class TaskItem
{
    private int id; // Unique Identifier
    private string Title; // Name of Task

    private string Description; // Details for the task
    private int UserId; // Assigned to a user

    private DateTime CreatedAt; // created time
    

    private bool IsComplete; // task status
    

}