using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using SmartHomeSystem.Data;

namespace SmartHomeSystem.Services
{
    public class GoogleTasksService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public GoogleTasksService(IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, List<GoogleTask>>> GetAllTasksAsync()
        {
            var tasksByList = new Dictionary<string, List<GoogleTask>>();
            var accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("No access token available");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get all task lists
            var response = await _httpClient.GetAsync("https://tasks.googleapis.com/tasks/v1/users/@me/lists");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var listTitle = item.GetProperty("title").GetString();
                var listId = item.GetProperty("id").GetString();

                var taskUrl = $"https://tasks.googleapis.com/tasks/v1/lists/{listId}/tasks";
                var tasksResponse = await _httpClient.GetAsync(taskUrl);
                tasksResponse.EnsureSuccessStatusCode();

                var tasksJson = await tasksResponse.Content.ReadAsStringAsync();
                using var tasksDoc = JsonDocument.Parse(tasksJson);

                var tasks = new List<GoogleTask>();
                if (tasksDoc.RootElement.TryGetProperty("items", out var taskItems))
                {
                    foreach (var taskItem in taskItems.EnumerateArray())
                    {
                        var task = new GoogleTask
                        {
                            Title = taskItem.GetProperty("title").GetString(),
                            Status = taskItem.GetProperty("status").GetString()
                        };

                        if (taskItem.TryGetProperty("notes", out var notes))
                            task.Notes = notes.GetString();

                        if (taskItem.TryGetProperty("due", out var due))
                            task.Due = DateTime.Parse(due.GetString());

                        tasks.Add(task);
                    }
                }
                tasksByList[listTitle] = tasks;
            }

            return tasksByList;
        }

        public string FormatTasksForSpeech(Dictionary<string, List<GoogleTask>> tasksByList)
        {
            if (!tasksByList.Any())
            {
                return "You have no tasks in your lists.";
            }

            var message = "Here are your tasks: ";
            foreach (var taskList in tasksByList)
            {
                if (taskList.Value.Any())
                {
                    message += $"In {taskList.Key}: ";
                    foreach (var task in taskList.Value)
                    {
                        message += $"{task.Title}. ";
                        if (!string.IsNullOrEmpty(task.Notes))
                        {
                            message += $"Notes: {task.Notes}. ";
                        }
                        if (task.Due.HasValue)
                        {
                            if (task.Due.HasValue)
                            {
                                message += $"Due {task.Due.Value.ToString("MMMM dd")}";
                            }
                        }
                    }
                }
                else
                {
                    message += "Jokes... There are no tasks Mr. Egbert!";
                }
            }

            return message;
        }
    }
}