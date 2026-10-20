using System;
using System.Globalization;
using System.Net;

namespace EmailTemplates
{
    /// <summary>
    /// Provides HTML template generation for task assignment notification emails.
    /// </summary>
    public static class TaskAssignedTemplate
    {
        /// <summary>
        /// Default email subject for task assignment notifications.
        /// </summary>
        public const string DefaultSubject = "You've been assigned to a task - Sprintle";

        /// <summary>
        /// Generates a customized subject line including the task title and optional workspace name.
        /// </summary>
        /// <param name="taskTitle">The title of the assigned task.</param>
        /// <param name="workspaceName">The optional name of the workspace.</param>
        /// <returns>A formatted subject string.</returns>
        public static string GetSubject(string taskTitle, string? workspaceName = null)
        {
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return DefaultSubject;
            }

            return string.IsNullOrWhiteSpace(workspaceName)
                ? $"You've been assigned to '{taskTitle}' - Sprintle"
                : $"You've been assigned to '{taskTitle}' in '{workspaceName}' - Sprintle";
        }

        /// <summary>
        /// Generates a responsive, styled HTML email body notifying a user that they were assigned to a task.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="taskDescription">The optional description of the task.</param>
        /// <param name="workspaceName">The name of the workspace the task belongs to.</param>
        /// <param name="assignedByName">The name of the user who assigned the task.</param>
        /// <param name="priority">The optional priority level of the task.</param>
        /// <param name="deadline">The optional deadline date and time for the task.</param>
        /// <param name="recipientName">The optional display name or email of the recipient for the greeting.</param>
        /// <returns>A fully-styled HTML email document matching frontend design tokens.</returns>
        public static string Render(
            string taskTitle,
            string? taskDescription,
            string workspaceName,
            string assignedByName,
            string? priority,
            DateTime? deadline,
            string? recipientName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(workspaceName);

            var encodedTaskTitle = WebUtility.HtmlEncode(taskTitle);
            var encodedWorkspaceName = WebUtility.HtmlEncode(workspaceName);
            var encodedAssignedByName = string.IsNullOrWhiteSpace(assignedByName)
                ? "A team member"
                : WebUtility.HtmlEncode(assignedByName);
            var greetingText = string.IsNullOrWhiteSpace(recipientName)
                ? "Hello,"
                : $"Hello <strong style=\"color: #323130;\">{WebUtility.HtmlEncode(recipientName)}</strong>,";

            var descriptionSection = string.IsNullOrWhiteSpace(taskDescription)
                ? string.Empty
                : $"""
                  <tr>
                      <td style="padding-bottom: 16px; font-size: 14px; line-height: 1.5; color: #605e5c;">
                          {WebUtility.HtmlEncode(taskDescription)}
                      </td>
                  </tr>
                  """;

            var (priorityBg, priorityColor, priorityBorder) = GetPriorityStyles(priority);
            var priorityLabel = string.IsNullOrWhiteSpace(priority) ? "None" : WebUtility.HtmlEncode(priority);
            var deadlineLabel = deadline.HasValue
                ? deadline.Value.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture)
                : "No deadline";

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Task Assigned - Sprintle</title>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
                        body, table, td, a { -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }
                        table, td { mso-table-lspace: 0pt; mso-table-rspace: 0pt; }
                        img { -ms-interpolation-mode: bicubic; border: 0; outline: none; text-decoration: none; }
                        a:hover { text-decoration: underline; }
                        @media screen and (max-width: 600px) {
                            .email-card { width: 100% !important; padding: 24px !important; margin: 0 !important; }
                            .email-container { padding: 12px !important; }
                        }
                    </style>
                </head>
                <body style="margin: 0; padding: 0; width: 100% !important; background-color: #f5f5f5; font-family: 'Segoe UI', 'Inter', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #323130;">
                    <table class="email-container" role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f5f5f5; padding: 40px 16px;">
                        <tr>
                            <td align="center">
                                <table class="email-card" role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="max-width: 560px; background-color: #ffffff; border-radius: 8px; border: 1px solid #edebe9; box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08); overflow: hidden; padding: 40px 36px;">
                                    <!-- Brand Header -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 28px;">
                                            <table role="presentation" border="0" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="background-color: #0078d4; border-radius: 6px; width: 36px; height: 36px; text-align: center; vertical-align: middle;">
                                                        <span style="color: #ffffff; font-size: 20px; font-weight: 700; line-height: 36px; display: inline-block;">✓</span>
                                                    </td>
                                                    <td style="padding-left: 12px; vertical-align: middle;">
                                                        <span style="font-size: 20px; font-weight: 700; color: #0078d4; letter-spacing: -0.3px;">Sprintle</span>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Main Title -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 16px;">
                                            <h1 style="margin: 0; font-size: 24px; font-weight: 600; color: #323130; line-height: 1.3;">You've been assigned to a task</h1>
                                        </td>
                                    </tr>

                                    <!-- Body Description -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 20px; font-size: 15px; line-height: 1.6; color: #605e5c;">
                                            <p style="margin: 0 0 14px 0;">{{greetingText}}</p>
                                            <p style="margin: 0;"><strong>{{encodedAssignedByName}}</strong> has assigned you to a task in the <strong>{{encodedWorkspaceName}}</strong> workspace.</p>
                                        </td>
                                    </tr>

                                    <!-- Task Card Box -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 28px;">
                                            <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f8f9fa; border-radius: 6px; border: 1px solid #edebe9; padding: 20px;">
                                                <tr>
                                                    <td style="padding-bottom: 10px;">
                                                        <p style="margin: 0 0 4px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #605e5c;">Task</p>
                                                        <h2 style="margin: 0; font-size: 18px; font-weight: 600; color: #323130; line-height: 1.3;">{{encodedTaskTitle}}</h2>
                                                    </td>
                                                </tr>
                                                {{descriptionSection}}
                                                <tr>
                                                    <td style="border-top: 1px solid #edebe9; padding-top: 14px;">
                                                        <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%">
                                                            <tr>
                                                                <td style="width: 50%; vertical-align: top; padding-bottom: 8px;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Workspace</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 600; color: #323130;">{{encodedWorkspaceName}}</p>
                                                                </td>
                                                                <td style="width: 50%; vertical-align: top; padding-bottom: 8px;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Priority</p>
                                                                    <p style="margin: 0;">
                                                                        <span style="display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; background-color: {{priorityBg}}; color: {{priorityColor}}; border: 1px solid {{priorityBorder}};">{{priorityLabel}}</span>
                                                                    </p>
                                                                </td>
                                                            </tr>
                                                            <tr>
                                                                <td style="width: 50%; vertical-align: top;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Due Date</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 500; color: #323130;">{{deadlineLabel}}</p>
                                                                </td>
                                                                <td style="width: 50%; vertical-align: top;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Assigned By</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 500; color: #323130;">{{encodedAssignedByName}}</p>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Security Notice & Footer -->
                                    <tr>
                                        <td align="left" style="font-size: 12px; line-height: 1.5; color: #a19f9d; border-top: 1px solid #edebe9; padding-top: 24px;">
                                            <p style="margin: 0 0 8px 0;">You received this email because you are a registered user of Sprintle and were assigned to a task.</p>
                                            <p style="margin: 0;">&copy; 2026 Sprintle. All rights reserved.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;
        }

        private static (string Background, string Color, string Border) GetPriorityStyles(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
            {
                return ("#f3f2f1", "#605e5c", "#edebe9");
            }

            return priority.ToLowerInvariant() switch
            {
                "critical" => ("#fde7e9", "#a80000", "#f3b2b7"),
                "high" => ("#fdf3f0", "#d83b01", "#f8c2b3"),
                "medium" => ("#f0f6ff", "#0078d4", "#b4d6fa"),
                "low" => ("#f3f2f1", "#498205", "#b8e39b"),
                _ => ("#f3f2f1", "#605e5c", "#edebe9")
            };
        }
    }
}
