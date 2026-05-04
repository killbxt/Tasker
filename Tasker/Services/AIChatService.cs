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

        public async Task<string> SendMessageAsync(string userMessage, List<Models.Task> currentTasks)
        {
            var tasksJson = JsonSerializer.Serialize(currentTasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                PlannedStartAt = t.PlannedStartAt?.ToString("yyyy-MM-ddTHH:mm"),
                PlannedEndAt = t.PlannedEndAt.ToString("yyyy-MM-ddTHH:mm")
            }));

            var systemPrompt = $@"Ты помощник в управлении задачами. Текущие задачи пользователя: {tasksJson}

                Ответь строго в формате JSON без лишнего текста:
                {{
                    ""action"": ""create|update|delete|none"",
                    ""taskId"": число или null,
                    ""taskData"": {{
                        ""title"": ""название задачи"",
                        ""description"": ""описание"",
                        ""plannedStartAt"": ""2024-12-31T09:00"",
                        ""plannedEndAt"": ""2024-12-31T18:00"",
                        ""priority"": ""Urgent|Normal""
                    }},
                    ""message"": ""текст ответа пользователю""
                }}";

            var requestBody = new
            {
                message = $"{systemPrompt}\n\nПользователь сказал: {userMessage}",
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