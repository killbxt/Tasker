using GongSolutions.Wpf.DragDrop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskManager.Models;
using TaskManager.ViewModels;

namespace TaskManager.Services
{
    public class DropHandler : IDropTarget
    {
        private readonly MainViewModel _viewModel;

        public DropHandler(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not TaskViewModel sourceItem)
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            var targetStatus = ResolveTargetStatus(dropInfo);
            if (targetStatus == null || sourceItem.Status == targetStatus.Value)
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            dropInfo.Effects = DragDropEffects.Move;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not TaskViewModel sourceItem)
            {
                return;
            }

            var targetStatus = ResolveTargetStatus(dropInfo);
            if (targetStatus == null || sourceItem.Status == targetStatus.Value)
            {
                return;
            }

            _viewModel.MoveTask(sourceItem, targetStatus.Value);
        }

        private TaskState? ResolveTargetStatus(IDropInfo dropInfo)
        {
            if (dropInfo.TargetCollection == _viewModel.TodoTasks)
            {
                return TaskState.Todo;
            }

            if (dropInfo.TargetCollection == _viewModel.InProgressTasks)
            {
                return TaskState.InProgress;
            }

            if (dropInfo.TargetCollection == _viewModel.DoneTasks)
            {
                return TaskState.Done;
            }

            return ResolveTargetStatusFromVisualTree(dropInfo);
        }

        private static TaskState? ResolveTargetStatusFromVisualTree(IDropInfo dropInfo)
        {
            var target = dropInfo.VisualTarget as DependencyObject;

            while (target != null)
            {
                if (target is FrameworkElement fe)
                {
                    switch (fe.Name)
                    {
                        case "TodoColumn":
                        case "TodoItemsControl":
                            return TaskState.Todo;
                        case "InProgressColumn":
                        case "InProgressItemsControl":
                            return TaskState.InProgress;
                        case "DoneColumn":
                        case "DoneItemsControl":
                            return TaskState.Done;
                    }
                }

                target = VisualTreeHelper.GetParent(target);
            }

            return null;
        }
    }
}