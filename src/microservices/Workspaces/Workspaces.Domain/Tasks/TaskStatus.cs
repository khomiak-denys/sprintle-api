using System.Text.Json.Serialization;

namespace Workspaces.Domain.Tasks
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskStatus
    {
        Todo = 0,
        InProgress = 1,
        InReview = 2,
        Done = 3,
        Cancelled = 4,
        Closed = 5
    }
}
