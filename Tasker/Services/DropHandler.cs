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

            TaskState targetStatus = GetTargetStatus(dropInfo);

            if (sourceItem.Status != targetStatus)
            {
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not TaskViewModel sourceItem)
            {
                return;
            }

            TaskState targetStatus = GetTargetStatus(dropInfo);

            if (sourceItem.Status != targetStatus)
            {
                _viewModel.MoveTask(sourceItem, targetStatus);
            }
        }

        private TaskState GetTargetStatus(IDropInfo dropInfo)
        {
            // Отладочная информация
            System.Diagnostics.Debug.WriteLine($"VisualTarget Type: {dropInfo.VisualTarget?.GetType()}");
            System.Diagnostics.Debug.WriteLine($"VisualTarget Name: {(dropInfo.VisualTarget as FrameworkElement)?.Name}");

            var target = dropInfo.VisualTarget as DependencyObject;

            while (target != null)
            {
                System.Diagnostics.Debug.WriteLine($"Parent Type: {target.GetType()}, Name: {(target as FrameworkElement)?.Name}");

                if (target is Border border)
                {
                    System.Diagnostics.Debug.WriteLine($"Found Border: {border.Name}");
                    switch (border.Name)
                    {
                        case "TodoColumn":
                            return TaskState.Todo;
                        case "InProgressColumn":
                            return TaskState.InProgress;
                        case "DoneColumn":
                            return TaskState.Done;
                    }
                }
                target = VisualTreeHelper.GetParent(target);
            }

            // Если не нашли, пробуем альтернативный метод
            if (dropInfo.VisualTarget is ItemsControl itemsControl)
            {
                switch (itemsControl.Name)
                {
                    case "TodoItemsControl":
                        return TaskState.Todo;
                    case "InProgressItemsControl":
                        return TaskState.InProgress;
                    case "DoneItemsControl":
                        return TaskState.Done;
                }
            }

            return TaskState.Todo;
        }
    }
}