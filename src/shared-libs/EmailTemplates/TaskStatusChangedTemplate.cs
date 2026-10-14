using System;
using System.Net;

namespace EmailTemplates
{
    /// <summary>
    /// Provides HTML template generation for task status change notification emails.
    /// </summary>
    public static class TaskStatusChangedTemplate
    {
        /// <summary>
        /// Default email subject for task status change notifications.
        /// </summary>
        public const string DefaultSubject = "Task status updated - TaskTracker";

        /// <summary>
        /// Generates a customized subject line including the task title and the new status.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="newStatus">The new status of the task.</param>
        /// <param name="workspaceName">The optional name of the workspace.</param>
        /// <returns>A formatted subject string.</returns>
        public static string GetSubject(string taskTitle, string newStatus, string? workspaceName = null)
        {
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return DefaultSubject;
            }

            if (string.Equals(newStatus, "Done", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(workspaceName)
                    ? $"Task '{taskTitle}' has been approved - TaskTracker"
                    : $"Task '{taskTitle}' in '{workspaceName}' has been approved - TaskTracker";
            }

            if (string.Equals(newStatus, "InReview", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(workspaceName)
                    ? $"Task '{taskTitle}' has been completed and submitted for review - TaskTracker"
                    : $"Task '{taskTitle}' in '{workspaceName}' has been completed and submitted for review - TaskTracker";
            }

            if (string.Equals(newStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(workspaceName)
                    ? $"Task '{taskTitle}' has been cancelled - TaskTracker"
                    : $"Task '{taskTitle}' in '{workspaceName}' has been cancelled - TaskTracker";
            }

            if (string.Equals(newStatus, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(workspaceName)
                    ? $"Task '{taskTitle}' has been closed - TaskTracker"
                    : $"Task '{taskTitle}' in '{workspaceName}' has been closed - TaskTracker";
            }

            var statusLabel = string.IsNullOrWhiteSpace(newStatus) ? "Updated" : newStatus;
            return string.IsNullOrWhiteSpace(workspaceName)
                ? $"Task '{taskTitle}' status changed to {statusLabel} - TaskTracker"
                : $"Task '{taskTitle}' status changed to {statusLabel} in '{workspaceName}' - TaskTracker";
        }

        /// <summary>
        /// Generates a responsive, styled HTML email body notifying a user of a task status transition.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="workspaceName">The name of the workspace the task belongs to.</param>
        /// <param name="oldStatus">The previous status of the task.</param>
        /// <param name="newStatus">The new status of the task.</param>
        /// <param name="changedByUserName">The username of the person who changed the status.</param>
        /// <param name="recipientName">The optional display name or email of the recipient for the greeting.</param>
        /// <returns>A fully-styled HTML email document matching frontend design tokens.</returns>
        public static string Render(
            string taskTitle,
            string workspaceName,
            string oldStatus,
            string newStatus,
            string changedByUserName,
            string? recipientName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(workspaceName);

            var encodedTaskTitle = WebUtility.HtmlEncode(taskTitle);
            var encodedWorkspaceName = WebUtility.HtmlEncode(workspaceName);
            var encodedOldStatus = string.IsNullOrWhiteSpace(oldStatus) ? "Unknown" : WebUtility.HtmlEncode(oldStatus);
            var encodedNewStatus = string.IsNullOrWhiteSpace(newStatus) ? "Updated" : WebUtility.HtmlEncode(newStatus);
            var encodedChangedBy = string.IsNullOrWhiteSpace(changedByUserName) ? "A team member" : WebUtility.HtmlEncode(changedByUserName);
            var greetingText = string.IsNullOrWhiteSpace(recipientName)
                ? "Hello,"
                : $"Hello <strong style=\"color: #323130;\">{WebUtility.HtmlEncode(recipientName)}</strong>,";

            var headline = GetHeadlineText(newStatus);
            var (oldBg, oldColor, oldBorder) = GetStatusBadgeStyles(oldStatus);
            var (newBg, newColor, newBorder) = GetStatusBadgeStyles(newStatus);

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>{{headline}} - TaskTracker</title>
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
                                                        <span style="font-size: 20px; font-weight: 700; color: #0078d4; letter-spacing: -0.3px;">TaskTracker</span>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Main Title -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 16px;">
                                            <h1 style="margin: 0; font-size: 24px; font-weight: 600; color: #323130; line-height: 1.3;">{{headline}}</h1>
                                        </td>
                                    </tr>

                                    <!-- Body Description -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 20px; font-size: 15px; line-height: 1.6; color: #605e5c;">
                                            <p style="margin: 0 0 14px 0;">{{greetingText}}</p>
                                            <p style="margin: 0;"><strong>{{encodedChangedBy}}</strong> updated the status of <strong>{{encodedTaskTitle}}</strong> in the <strong>{{encodedWorkspaceName}}</strong> workspace.</p>
                                        </td>
                                    </tr>

                                    <!-- Status Transition Card -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 28px;">
                                            <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f8f9fa; border-radius: 6px; border: 1px solid #edebe9; padding: 20px;">
                                                <!-- Transition Header Pill -->
                                                <tr>
                                                    <td align="center" style="padding-bottom: 16px;">
                                                        <table role="presentation" border="0" cellpadding="0" cellspacing="0">
                                                            <tr>
                                                                <td style="vertical-align: middle;">
                                                                    <span style="display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; background-color: {{oldBg}}; color: {{oldColor}}; border: 1px solid {{oldBorder}};">{{encodedOldStatus}}</span>
                                                                </td>
                                                                <td style="padding: 0 10px; vertical-align: middle; font-size: 16px; color: #8a8886; font-weight: 700;">
                                                                    &rarr;
                                                                </td>
                                                                <td style="vertical-align: middle;">
                                                                    <span style="display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; background-color: {{newBg}}; color: {{newColor}}; border: 1px solid {{newBorder}};">{{encodedNewStatus}}</span>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>

                                                <!-- Metadata Table -->
                                                <tr>
                                                    <td style="border-top: 1px solid #edebe9; padding-top: 14px;">
                                                        <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%">
                                                            <tr>
                                                                <td style="width: 50%; vertical-align: top; padding-bottom: 8px;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Task</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 600; color: #323130;">{{encodedTaskTitle}}</p>
                                                                </td>
                                                                <td style="width: 50%; vertical-align: top; padding-bottom: 8px;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Changed By</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 600; color: #323130;">{{encodedChangedBy}}</p>
                                                                </td>
                                                            </tr>
                                                            <tr>
                                                                <td style="width: 50%; vertical-align: top;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">Workspace</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 500; color: #323130;">{{encodedWorkspaceName}}</p>
                                                                </td>
                                                                <td style="width: 50%; vertical-align: top;">
                                                                    <p style="margin: 0 0 2px 0; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; color: #8a8886;">New Status</p>
                                                                    <p style="margin: 0; font-size: 13px; font-weight: 600; color: {{newColor}};">{{encodedNewStatus}}</p>
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
                                            <p style="margin: 0 0 8px 0;">You received this email because you are a participant in this task on TaskTracker.</p>
                                            <p style="margin: 0;">&copy; 2026 TaskTracker. All rights reserved.</p>
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

        private static string GetHeadlineText(string newStatus)
        {
            if (string.Equals(newStatus, "Done", StringComparison.OrdinalIgnoreCase))
            {
                return "Task Approved";
            }

            if (string.Equals(newStatus, "InReview", StringComparison.OrdinalIgnoreCase))
            {
                return "Task Completed &amp; Submitted for Review";
            }

            if (string.Equals(newStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return "Task Cancelled";
            }

            if (string.Equals(newStatus, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return "Task Closed";
            }

            return "Task Status Updated";
        }

        private static (string Background, string Color, string Border) GetStatusBadgeStyles(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return ("#f3f2f1", "#605e5c", "#edebe9");
            }

            return status.ToLowerInvariant() switch
            {
                "todo" => ("#f3f2f1", "#605e5c", "#edebe9"),
                "inprogress" => ("#f0f6ff", "#0078d4", "#b4d6fa"),
                "inreview" => ("#fff4ce", "#795b00", "#fce196"),
                "done" => ("#dff6dd", "#107c10", "#a8e7a7"),
                "closed" => ("#edebe9", "#323130", "#c8c6c4"),
                "cancelled" => ("#fde7e9", "#a80000", "#f3b2b7"),
                _ => ("#f3f2f1", "#605e5c", "#edebe9")
            };
        }
    }
}
