using System;

namespace EmailTemplates
{
    /// <summary>
    /// Formatting helpers for displaying tasks consistently across email templates.
    /// </summary>
    public static class TaskDisplay
    {
        /// <summary>
        /// Formats a task title with its workspace-scoped number prefix, e.g. <c>DMS-730 Fix login</c>.
        /// Returns the title unchanged when no number is available (e.g. messages published before task numbering existed).
        /// </summary>
        /// <param name="taskNumber">The optional task number (e.g. DMS-730).</param>
        /// <param name="taskTitle">The task title.</param>
        /// <returns>The display title.</returns>
        public static string FormatTitle(string? taskNumber, string taskTitle)
        {
            if (string.IsNullOrWhiteSpace(taskNumber) || string.IsNullOrWhiteSpace(taskTitle))
            {
                return taskTitle;
            }

            return $"{taskNumber} {taskTitle}";
        }
    }
}
