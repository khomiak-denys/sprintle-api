using System.Text.Json.Serialization;

namespace Workspaces.Domain.Tasks
{
    /// <summary>
    /// Represents the distinct workflow actions that can be performed on a <see cref="TaskItem"/>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskAction
    {
        /// <summary>
        /// Assigns a user to the task and transitions it to <see cref="TaskStatus.InProgress"/>.
        /// </summary>
        Assign = 1,

        /// <summary>
        /// Submits an in-progress task for review, transitioning it to <see cref="TaskStatus.InReview"/>.
        /// </summary>
        SubmitForReview = 2,

        /// <summary>
        /// Approves a task under review, transitioning it to <see cref="TaskStatus.Done"/>.
        /// </summary>
        Approve = 3,

        /// <summary>
        /// Closes a completed task, transitioning it to <see cref="TaskStatus.Closed"/>.
        /// </summary>
        Close = 4,

        /// <summary>
        /// Cancels the task, transitioning it to <see cref="TaskStatus.Cancelled"/>.
        /// </summary>
        Cancel = 5
    }
}
