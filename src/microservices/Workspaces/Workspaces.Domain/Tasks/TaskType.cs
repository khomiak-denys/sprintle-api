using System.Text.Json.Serialization;

namespace Workspaces.Domain.Tasks
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskType
    {
        Epic = 0,
        Feature = 1,
        UserStory = 2,
        Bug = 3,
        Task = 4
    }
}
