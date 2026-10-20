using System;
using System.Globalization;
using System.Net;

namespace EmailTemplates
{
    /// <summary>
    /// Provides HTML template generation for task time logging notification emails.
    /// </summary>
    public static class TaskTimeLoggedTemplate
    {
        /// <summary>
        /// Default email subject for task time logging notifications.
        /// </summary>
        public const string DefaultSubject = "Time logged on task - Sprintle";

        /// <summary>
        /// Formats an integer minute count into a human-readable duration (e.g., 90 -> 1h 30m, 60 -> 1h, 45 -> 45m).
        /// </summary>
        /// <param name="minutes">The number of minutes.</param>
        /// <returns>A formatted duration string.</returns>
        public static string FormatDuration(int minutes)
        {
            if (minutes <= 0)
            {
                return "0m";
            }

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            if (hours > 0 && remainingMinutes > 0)
            {
                return $"{hours}h {remainingMinutes}m";
            }

            if (hours > 0)
            {
                return $"{hours}h";
            }

            return $"{remainingMinutes}m";
        }

        /// <summary>
        /// Generates a customized subject line including the task title, formatted duration, and optional workspace name.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="minutesSpent">The number of minutes logged.</param>
        /// <param name="workspaceName">The optional name of the workspace.</param>
        /// <returns>A formatted subject string.</returns>
        public static string GetSubject(string taskTitle, int minutesSpent, string? workspaceName = null)
        {
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return DefaultSubject;
            }

            var duration = FormatDuration(minutesSpent);

            return string.IsNullOrWhiteSpace(workspaceName)
                ? $"Time logged on '{taskTitle}' ({duration}) - Sprintle"
                : $"Time logged on '{taskTitle}' ({duration}) in '{workspaceName}' - Sprintle";
        }

        /// <summary>
        /// Generates a responsive, styled HTML email body notifying a user that time was logged on a task.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="workspaceName">The name of the workspace the task belongs to.</param>
        /// <param name="minutesSpent">The number of minutes spent.</param>
        /// <param name="description">The optional description or note of the work done.</param>
        /// <param name="loggedDate">The calendar date on which the work was performed.</param>
        /// <param name="loggedByUserName">The name of the user who logged the time.</param>
        /// <param name="recipientName">The optional display name or email of the recipient for the greeting.</param>
        /// <returns>A fully-styled HTML email document matching frontend design tokens.</returns>
        public static string Render(
            string taskTitle,
            string workspaceName,
            int minutesSpent,
            string? description,
            DateOnly loggedDate,
            string loggedByUserName,
            string? recipientName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(workspaceName);

            var encodedTaskTitle = WebUtility.HtmlEncode(taskTitle);
            var encodedWorkspaceName = WebUtility.HtmlEncode(workspaceName);
            var encodedUserName = string.IsNullOrWhiteSpace(loggedByUserName)
                ? "A team member"
                : WebUtility.HtmlEncode(loggedByUserName);
            var greetingText = string.IsNullOrWhiteSpace(recipientName)
                ? "Hello,"
                : $"Hello <strong style=\"color: #323130;\">{WebUtility.HtmlEncode(recipientName)}</strong>,";

            var durationText = FormatDuration(minutesSpent);
            var dateText = loggedDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);

            var descriptionSection = string.IsNullOrWhiteSpace(description)
                ? string.Empty
                : $"""
                  <tr>
                      <td style="padding: 6px 0; font-size: 13px; color: #605e5c; vertical-align: top;">Notes:</td>
                      <td style="padding: 6px 0; font-size: 14px; color: #323130; white-space: pre-wrap; word-break: break-word;">{WebUtility.HtmlEncode(description)}</td>
                  </tr>
                  """;

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Time Logged - Sprintle</title>
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
                <body style="margin: 0; padding: 0; background-color: #f3f2f1; font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #323130; -webkit-font-smoothing: antialiased;">
                    <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f3f2f1;">
                        <tr>
                            <td align="center" class="email-container" style="padding: 40px 16px;">
                                <table role="presentation" class="email-card" width="560" border="0" cellspacing="0" cellpadding="0" style="width: 560px; max-width: 560px; background-color: #ffffff; border-radius: 8px; border: 1px solid #edebe9; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); overflow: hidden; padding: 40px 48px; box-sizing: border-box; text-align: left;">
                                    <!-- Brand Logo / Header -->
                                    <tr>
                                        <td style="padding-bottom: 24px;">
                                            <table role="presentation" border="0" cellspacing="0" cellpadding="0">
                                                <tr>
                                                    <td style="background-color: #0078d4; width: 36px; height: 36px; border-radius: 6px; text-align: center; vertical-align: middle;">
                                                        <span style="color: #ffffff; font-weight: 700; font-size: 20px; line-height: 36px; display: block;">T</span>
                                                    </td>
                                                    <td style="padding-left: 12px; font-size: 20px; font-weight: 700; color: #0078d4; letter-spacing: -0.5px;">
                                                        Sprintle
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Headline -->
                                    <tr>
                                        <td style="padding-bottom: 16px; font-size: 24px; font-weight: 700; color: #201f1e; letter-spacing: -0.3px;">
                                            Time Logged on Task
                                        </td>
                                    </tr>

                                    <!-- Greeting & Context -->
                                    <tr>
                                        <td style="padding-bottom: 24px; font-size: 15px; line-height: 1.6; color: #605e5c;">
                                            {{greetingText}}<br>
                                            <strong style="color: #323130;">{{encodedUserName}}</strong> logged <strong style="color: #0078d4;">{{durationText}}</strong> on <strong style="color: #323130;">{{encodedTaskTitle}}</strong> in workspace <strong style="color: #0078d4;">{{encodedWorkspaceName}}</strong>.
                                        </td>
                                    </tr>

                                    <!-- Time Log Details Card -->
                                    <tr>
                                        <td style="padding-bottom: 28px;">
                                            <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #faf9f8; border: 1px solid #edebe9; border-radius: 6px; padding: 16px;">
                                                <tr>
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c; width: 35%;">Task:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #323130;">{{encodedTaskTitle}}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c;">Workspace:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #323130;">{{encodedWorkspaceName}}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c;">Logged by:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #323130;">{{encodedUserName}}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c;">Time spent:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #0078d4;">{{durationText}} ({{minutesSpent}} minutes)</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c;">Date:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #323130;">{{dateText}}</td>
                                                </tr>
                                                {{descriptionSection}}
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Divider -->
                                    <tr>
                                        <td style="border-top: 1px solid #edebe9; padding-bottom: 24px;"></td>
                                    </tr>

                                    <!-- Footer -->
                                    <tr>
                                        <td style="font-size: 12px; line-height: 1.5; color: #a19f9d; text-align: center;">
                                            You are receiving this email because you created this task in Sprintle.<br>
                                            &copy; {{DateTime.UtcNow.Year}} Sprintle. All rights reserved.
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
    }
}
