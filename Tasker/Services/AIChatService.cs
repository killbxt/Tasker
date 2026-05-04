using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TaskManager.Services
{
    public class AIChatService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCIsImtpZCI6IjFrYnhacFJNQGJSI0tSbE1xS1lqIn0.eyJ1c2VyIjoiYW04MzYxNCIsInR5cGUiOiJhcGlfa2V5IiwiYXBpX2tleV9pZCI6IjhhZmM3NDhkLThhYjItNDlmYi04MzExLTI1YjMxYTlhZjQwNiIsImlhdCI6MTc3NjEwMDU0NH0.WHrA7oZuZY1PIFQ0hJxkSMCARRaNENYCwJtu9yyZcLL9ENfIMcYmFyr71W9483TCleO39iseKmZUITtdOwm6lrja2c2f6SDr-D-j02belEjBD8qlFlhnwg-BgOC4UZ4AC7OWEsU_12L6k2njIv1If2KHDCsHqfSe8b-vrYNaJFR15X-G4peyu3kXoXlx272WrKjVRe3F2GYKu-1zVSHakNnkq_ID3gRI8K1NJhQDgYQRNhb8-f9E6AOw4DOnHTwL0WMeGhHlYe-LL3QBnvUp6VRe__BSOefpD6ULhtu8OWvL6f8ygQgqE_n1CTD8Nbs2trH0F6tnc1aF8hjq7mSKPKFvScNKC8gbSe1kYZ1X4THb_1WTG_KlckpIjMJWNlrTMn6yFY9Bve_zSlZhodNuON2P_Kz07_w5M5SElaM7Eih0JO2Wvhqt604c2hOepJdTKr0DrTsHo2OkGpbx3XssMOn3oqDAlNEV1m9_YIbWaz43zZRycriuZbNrLTze58uX";
        private const string AgentAccessId = "f8f2a682-a55b-4911-94da-6271f7e09e2a";
        private const string ApiUrl = "https://agent.timeweb.cloud/api/v1/cloud-ai/agents/{agent_access_id}/call";

        public AIChatService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            _httpClient.DefaultRequestHeaders.Add("x-proxy-source", "wpf-app");
        }

        public async Task<string> SendMessageAsync(string userMessage, List<Models.Task> currentTasks, string workspaceContextJson)
        {
            var tasksJson = JsonSerializer.Serialize(currentTasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                PlannedStartAt = t.PlannedStartAt?.ToString("yyyy-MM-ddTHH:mm"),
                PlannedEndAt = t.PlannedEndAt.ToString("yyyy-MM-ddTHH:mm"),
                t.TeamId
            }));

            var systemPrompt = $@"Ты помощник приложения Tasker (задачи, организации, команды). Пиши message пользователю по-русски, коротко и ясно.

КОНТЕКСТ РАБОЧЕГО ПРОСТРАНСТВА (JSON): {workspaceContextJson}

ТЕКУЩИЕ ЗАДАЧИ (JSON): {tasksJson}

Ответь СТРОГО одним JSON-объектом без текста до или после:
{{
  ""action"": ""create_task|update_task|delete_task|update_organization|delete_organization|create_team|update_team|delete_team|none"",
  ""taskId"": null или число,
  ""teamId"": null или число (id команды из контекста),
  ""taskData"": {{
    ""title"": ""..."",
    ""description"": ""..."",
    ""plannedStartAt"": ""yyyy-MM-ddTHH:mm"" или null,
    ""plannedEndAt"": ""yyyy-MM-ddTHH:mm"",
    ""priority"": ""Urgent"" или ""Normal""
  }},
  ""organizationData"": {{
    ""name"": ""..."",
    ""description"": ""...""
  }},
  ""teamData"": {{
    ""name"": ""..."",
    ""description"": ""...""
  }},
  ""message"": ""понятное объяснение для пользователя""
}}

Правила:
- Если речь только о задачах — используй create_task / update_task / delete_task. Старые имена create/update/delete тоже допустимы внутри логики, но в поле action выводи именно create_task, update_task, delete_task.
- update_organization: только если у пользователя есть organization в контексте; заполни organizationData (хотя бы name). Любой участник организации может переименовать её.
- delete_organization: только если пользователь явно просит удалить организацию/компанию целиком; teamId и taskId должны быть null. Очень разрушительное действие — в message предупреди.
- create_team: нужна организация в контексте; teamData.name обязателен.
- update_team / delete_team: укажи teamId из списка команд; удалять и переименовывать может только владелец (ownerId в контексте должен совпадать с userId).
- Если сомневаешься или не хватает данных — action = ""none"" и в message спроси или объясни.
- plannedEndAt обязателен для create_task; если не сказано — придумай разумный срок через несколько дней.";

            var requestBody = new
            {
                message = $"{systemPrompt}\n\nСообщение пользователя: {userMessage}",
                parent_message_id = (string?)null,
                file_ids = new string[] { },
                metadata = new { }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var url = ApiUrl.Replace("{agent_access_id}", AgentAccessId);
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString() ?? responseString;
                }

                return responseString;
            }
            catch (Exception ex)
            {
                return $"{{\"action\": \"none\", \"message\": \"Ошибка: {ex.Message}\"}}";
            }
        }
    }
}
