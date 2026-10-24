using System.Text.Json.Serialization;

namespace Workspaces.Domain.Tasks
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskType
    {
        Task = 0,
        Epic = 1,
        Feature = 2,
        UserStory = 3,
        Bug = 4
    }
}
