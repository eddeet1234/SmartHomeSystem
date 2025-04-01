using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Data;
using System.Net.Http.Headers;
using System.Text.Json;

public class DashboardModel : PageModel
{
    public List<GoogleTaskList> TaskLists { get; set; } = new();
    public string ErrorMessage { get; set; }
    public Dictionary<string, List<GoogleTask>> TasksByList { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(accessToken))
            {
                ErrorMessage = "You are not authenticated. Please sign in to access your tasks.";
                return;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://tasks.googleapis.com/tasks/v1/users/@me/lists");

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Unable to fetch task lists. Status: {response.StatusCode}";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    var listTitle = item.GetProperty("title").GetString();
                    var listId = item.GetProperty("id").GetString();
                    
                    TaskLists.Add(new GoogleTaskList
                    {
                        Title = listTitle,
                        Id = listId
                    });

                    // Fetch tasks for this list
                    var tasksResponse = await httpClient.GetAsync($"https://tasks.googleapis.com/tasks/v1/lists/{listId}/tasks?showCompleted=true&showDeleted=false&showHidden=true");


                    if (tasksResponse.IsSuccessStatusCode)
                    {
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

                                // Handle optional properties
                                if (taskItem.TryGetProperty("notes", out var notes))
                                    task.Notes = notes.GetString();

                                if (taskItem.TryGetProperty("due", out var due))
                                    task.Due = DateTime.Parse(due.GetString());

                                tasks.Add(task);
                            }
                        }
                        TasksByList[listTitle] = tasks;
                    }
                }
            }
            catch (JsonException ex)
            {
                ErrorMessage = "Unable to process the task list data. Please try again later.";
                return;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An unexpected error occurred. Please try again later.";
            // You might want to log the actual exception here
            return;
        }
    }
}
