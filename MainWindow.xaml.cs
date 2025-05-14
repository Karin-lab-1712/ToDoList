using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ServerToDoList;
using System.ComponentModel;
using System.Globalization;

namespace WpfToDoList
{
    // <summary>
    /// Main Window logic for the To-Do List application.
    /// Provides interaction with task data using a TaskViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {

        // Observable collection to store tasks, which automatically updates the UI when modified.
        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();

        private TaskViewModel _taskViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _taskViewModel = new TaskViewModel();
            DataContext = _taskViewModel;
        }

        //Handles the event when the Enter key is pressed while editing a task.
        private void TaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb && tb.DataContext is TaskModel task)
            {
                task.IsEditing = false;
            }
        }

        // Handles the event when the TextBox loses focus.
        private async void TaskTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is TaskModel task)
            {
                task.IsEditing = false;
                await _taskViewModel.UpdateTaskDescription(task);
                await _taskViewModel.UnlockTask(task.Id);
            }
        }

        // Handles the event when the TextBox gains focus.
        private async void TaskTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TaskModel task)
            {
                task.IsEditing = true;
                await _taskViewModel.LockTask(task.Id);
            }
        }     
    }
}
