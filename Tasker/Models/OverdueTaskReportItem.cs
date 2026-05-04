namespace TaskManager.Models
{
    /// <summary>Строка отчёта о просрочке для владельца команды.</summary>
    public class OverdueTaskReportItem
    {
        public string OrganizationName { get; set; } = "";
        public string TeamName { get; set; } = "";
        public string TaskTitle { get; set; } = "";
        public string AssigneeName { get; set; } = "";
        public DateTime PlannedEndAt { get; set; }

        public string PlannedEndText => PlannedEndAt.ToString("dd.MM.yyyy HH:mm");

        public string OverdueText
        {
            get
            {
                var d = DateTime.Now - PlannedEndAt;
                if (d.TotalDays >= 1)
                {
                    return $"{(int)d.TotalDays} дн.";
                }

                return $"{(int)d.TotalHours} ч.";
            }
        }
    }
}
