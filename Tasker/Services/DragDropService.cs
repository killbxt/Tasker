using System.Windows;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.Services
{
    public static class DragDropService
    {
        public static void SetDragData(UIElement element, TaskViewModel task)
        {
            DragDrop.DoDragDrop(element, task, DragDropEffects.Move);
        }

        public static TaskViewModel? GetDragData(IDataObject data)
        {
            return data.GetData(typeof(TaskViewModel)) as TaskViewModel;
        }

        public static bool CanDrop(TaskState targetStatus, TaskViewModel draggedTask)
        {
            if (draggedTask == null)
            {
                return false;
            }

            return draggedTask.Status != targetStatus;
        }
    }
}