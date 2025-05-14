using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using Microsoft.AspNetCore.SignalR.Client;
using ServerToDoList;

namespace WpfToDoList
{
    /// <summary>
    /// ViewModel responsible for managing the task list and handling user interactions.
    /// Connects to the SignalR hub, performs CRUD operations, and updates the UI accordingly.
    /// </summary>
    public class TaskViewModel : INotifyPropertyChanged
    {
        private HubConnection connection;
        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();

        private string _taskText;
        public string TaskText
        {
            get => _taskText;
            set
            {
                _taskText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaskText)));
                ((RelayCommand)AddTaskCommand).RaiseCanExecuteChanged();
            }
        }


        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleTaskStatusCommand { get; }

        public TaskViewModel()
        {
            AddTaskCommand = new RelayCommand(async () => await AddTask(), CanAddTask);
            DeleteTaskCommand = new RelayCommand<TaskModel>(async (task) => await DeleteTask(task));
            ToggleTaskStatusCommand = new RelayCommand<TaskModel>(async (task) => await UpdateTaskStatus(task));
            InitializeSignalR();
        }

        private async void InitializeSignalR()
        {

            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7273/tasksHub")
                .WithAutomaticReconnect()
                .Build();


            connection.On<List<TaskModel>>("ReceiveTasks", (tasksFromServer) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Tasks.Clear();
                    foreach (var task in tasksFromServer)
                    {
                        Tasks.Add(task);
                    }
                    SortTasks();
                });
            });

            connection.On<int, string>("ReceiveUpdatedTask", (taskId, updatedDescription) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var task = Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        task.Description = updatedDescription;
                    }
                });
            });


            try
            {
                await connection.StartAsync();
                Debug.WriteLine("SignalR connected!");
                await UpdateList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SignalR connection failed: {ex.Message}");
            }
        }

        // Adds a new task to the list via the SignalR hub.
        private async Task AddTask()
        {
            if (!string.IsNullOrWhiteSpace(TaskText))
            {
                await connection.InvokeAsync("AddTask", TaskText);
                TaskText = string.Empty;
            }           
        }

        // Deletes the task from the server via SignalR.
        private async Task DeleteTask(TaskModel task)
        {
            try
            {
                await connection.InvokeAsync("DeleteTask", task.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting task: {ex.Message}");
            }
        }

        //Updates the description of the task via SignalR.
        public async Task UpdateTaskDescription(TaskModel task)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(task.Description))
                {
                    await connection.InvokeAsync("UpdateTask", task.Id, task.Description);
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating task: {ex.Message}");
            }
        }

        // Toggles the "IsDone" status of the task via SignalR.
        private async Task UpdateTaskStatus(TaskModel task)
        {
            try
            {
                await connection.InvokeAsync("UpdateTaskStatus", task.Id, task.IsDone);
                SortTasks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating task status: {ex.Message}");
            }
        }

        // Retrieves all tasks from the server and updates the local task list.
        public async Task UpdateList()
        {
            try
            {
                var allTasks = await connection.InvokeAsync<List<TaskModel>>("GetAllTasks");
                Tasks.Clear();
                foreach (var task in allTasks)
                {
                    Tasks.Add(task);
                }
                SortTasks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching tasks: {ex.Message}");

            }
        }

        // Sorts the task list so that incomplete tasks appear before completed ones.
        private void SortTasks()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Tasks);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("IsDone", ListSortDirection.Ascending));
        }

        //Determines whether a task can be added (i.e., the input is not empty).
        private bool CanAddTask()
        {
            return !string.IsNullOrWhiteSpace(TaskText);
        }

        //Locks a task for editing (e.g., to prevent concurrent changes).
        public async Task LockTask(int taskId)
        {
            try
            {
                await connection.InvokeAsync("LockTask", taskId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error locking task: {ex.Message}");
            }
        }

        // Unlocks a task after editing is complete.
        public async Task UnlockTask(int taskId)
        {
            try
            {
                await connection.InvokeAsync("UnlockTask", taskId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unlocking task: {ex.Message}");
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
