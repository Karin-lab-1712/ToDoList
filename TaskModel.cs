using System.ComponentModel;

namespace ServerToDoList
{

    /// <summary>
    /// Represents a single task item in the to-do list.
    /// </summary>
    /// <remarks>
    /// This model is used to store and transfer task-related data between the database and the application logic.
    /// </remarks>
    public class TaskModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
         
        private string description;
        public string Description
        {
            get => description;
            set
            {
                if (description != value)
                {
                    description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// Indicates whether the task is marked as completed.
        private bool isDone;
        public bool IsDone
        {
            get => isDone;
            set
            {
                if (isDone != value)
                {
                    isDone = value;
                    OnPropertyChanged(nameof(IsDone));
                }
            }
        }

        /// Indicates whether the task is currently being edited.

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }

              

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
