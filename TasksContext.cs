using Microsoft.EntityFrameworkCore;

namespace ServerToDoList
{
    // <summary>
    /// Represents the Entity Framework database context for the task management application.
    /// Provides access to the Tasks table in the database.
    /// </summary>
    public class TaskContext : DbContext
    {
        public TaskContext(DbContextOptions<TaskContext> options) : base(options) { }

        public DbSet<TaskModel> Tasks { get; set; }
    }
}
