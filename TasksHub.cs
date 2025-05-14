using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServerToDoList;

namespace ServerToDoList
{

    /// <summary>
    /// SignalR hub for managing real-time task operations such as adding, updating, and deleting tasks.
    /// </summary>
    /// <remarks>
    /// This hub enables connected clients to receive live updates about task changes.
    /// It interacts with the task database context and uses logging to record operations.
    /// </remarks>
    public class TasksHub : Hub
    {
        private readonly TaskContext _context;

        private static readonly Dictionary<int, string> TaskLocks = new();

        private readonly ILogger<TasksHub> _logger;

        public TasksHub(ILogger<TasksHub> logger, TaskContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }


        //Create Task
        public async Task AddTask(string task)
        {
            try
            {
                var newTask = new TaskModel { Description = task };
                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[AddTask] Task saved to DB: ** {task} **");

            }
            catch (Exception ex)
            {
                _logger.LogError($"[AddTask] Error saving to DB: {ex.Message}");
            }

            var allTasks = await _context.Tasks.ToListAsync();
            await Clients.All.SendAsync("ReceiveTasks", allTasks);
        }

        //Read Tasks
        public async Task<List<TaskModel>> GetAllTasks()
        {
            _logger.LogInformation("[GetAllTasks] Called by client");
            Console.WriteLine("[GetAllTasks] Called by client");

            try
            {
                return await _context.Tasks.ToListAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError("[GetAllTasks] Error: {ex.Message}");
                return new List<TaskModel>();
            }
        }

        //Update Task
        public async Task UpdateTask(int taskId, string updatedContent)
        {
            var connectionId = Context.ConnectionId;

            lock (TaskLocks)
            {
                if (TaskLocks.TryGetValue(taskId, out var lockedBy) && lockedBy != connectionId)
                {
                    _logger.LogInformation($"[UpdateTask] Task {taskId} is locked by another user");
                    Console.WriteLine($"[UpdateTask] Task {taskId} is locked by another user");
                    return;
                }
            }

            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.Description = updatedContent;

                await _context.SaveChangesAsync();

                await Clients.All.SendAsync("ReceiveUpdatedTask", taskId, updatedContent);
            }
        }


        //Delate Task
        public async Task DeleteTask(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _logger.LogInformation($"[DeleteTask]  ** {task.Description} ** - was deleted");

                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("ReceiveTasks", await _context.Tasks.ToListAsync());
            }
        }

        //Is done?
        public async Task UpdateTaskStatus(int taskId, bool isDone)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.IsDone = isDone;
                _logger.LogInformation($"[UpdateTaskStatus] ** {task.Description} **  + {(isDone ? "is done" : "isn't done")}");
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("ReceiveTasks", await _context.Tasks.ToListAsync());
            }
        }

        public async Task LockTask(int taskId)
        {
            var connectionId = Context.ConnectionId;
            lock (TaskLocks)
            {
                if (!TaskLocks.ContainsKey(taskId))
                    TaskLocks[taskId] = connectionId;
            }

            await Clients.All.SendAsync("TaskLocked", taskId, connectionId);
        }

        public async Task UnlockTask(int taskId)
        {
            var connectionId = Context.ConnectionId;
            lock (TaskLocks)
            {
                if (TaskLocks.TryGetValue(taskId, out var owner) && owner == connectionId)
                {
                    TaskLocks.Remove(taskId);
                }
            }

            await Clients.All.SendAsync("TaskUnlocked", taskId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            var releasedTasks = new List<int>();

            lock (TaskLocks)
            {
                foreach (var kvp in TaskLocks.Where(kvp => kvp.Value == connectionId).ToList())
                {
                    TaskLocks.Remove(kvp.Key);
                    releasedTasks.Add(kvp.Key);
                }
            }

            foreach (var taskId in releasedTasks)
            {
                await Clients.All.SendAsync("TaskUnlocked", taskId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        
    }
}
