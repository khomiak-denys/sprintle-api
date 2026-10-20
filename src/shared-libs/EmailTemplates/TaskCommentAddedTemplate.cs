using System;
using System.Net;

namespace EmailTemplates
{
    /// <summary>
    /// Provides HTML template generation for task comment notification emails.
    /// </summary>
    public static class TaskCommentAddedTemplate
    {
        /// <summary>
        /// Default email subject for task comment notifications.
        /// </summary>
        public const string DefaultSubject = "New comment on task - Sprintle";

        /// <summary>
        /// Generates a customized subject line including the task title and optional workspace name.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="workspaceName">The optional name of the workspace.</param>
        /// <returns>A formatted subject string.</returns>
        public static string GetSubject(string taskTitle, string? workspaceName = null)
        {
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return DefaultSubject;
            }

            return string.IsNullOrWhiteSpace(workspaceName)
                ? $"New comment on '{taskTitle}' - Sprintle"
                : $"New comment on '{taskTitle}' in '{workspaceName}' - Sprintle";
        }

        /// <summary>
        /// Generates a responsive, styled HTML email body notifying a user that a new comment was posted on a task.
        /// </summary>
        /// <param name="taskTitle">The title of the task.</param>
        /// <param name="workspaceName">The name of the workspace the task belongs to.</param>
        /// <param name="commentContent">The content of the posted comment.</param>
        /// <param name="commentAuthorName">The display name or username of the comment author.</param>
        /// <param name="recipientName">The optional display name or email of the recipient for the greeting.</param>
        /// <returns>A fully-styled HTML email document matching frontend design tokens.</returns>
        public static string Render(
            string taskTitle,
            string workspaceName,
            string commentContent,
            string commentAuthorName,
            string? recipientName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(workspaceName);
            ArgumentException.ThrowIfNullOrWhiteSpace(commentContent);

            var encodedTaskTitle = WebUtility.HtmlEncode(taskTitle);
            var encodedWorkspaceName = WebUtility.HtmlEncode(workspaceName);
            var encodedCommentContent = WebUtility.HtmlEncode(commentContent);
            var encodedAuthorName = string.IsNullOrWhiteSpace(commentAuthorName)
                ? "A team member"
                : WebUtility.HtmlEncode(commentAuthorName);
            var greetingText = string.IsNullOrWhiteSpace(recipientName)
                ? "Hello,"
                : $"Hello <strong style=\"color: #323130;\">{WebUtility.HtmlEncode(recipientName)}</strong>,";

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>New Comment - Sprintle</title>
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
                                            New Comment on Task
                                        </td>
                                    </tr>

                                    <!-- Greeting & Context -->
                                    <tr>
                                        <td style="padding-bottom: 24px; font-size: 15px; line-height: 1.6; color: #605e5c;">
                                            {{greetingText}}<br>
                                            <strong style="color: #323130;">{{encodedAuthorName}}</strong> commented on <strong style="color: #323130;">{{encodedTaskTitle}}</strong> in workspace <strong style="color: #0078d4;">{{encodedWorkspaceName}}</strong>:
                                        </td>
                                    </tr>

                                    <!-- Comment Quote Card -->
                                    <tr>
                                        <td style="padding-bottom: 24px;">
                                            <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f8f9fa; border-left: 4px solid #0078d4; border-radius: 4px; padding: 16px 20px;">
                                                <tr>
                                                    <td style="font-size: 14px; line-height: 1.6; color: #323130; white-space: pre-wrap; word-break: break-word;">{{encodedCommentContent}}</td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Task Metadata Summary Box -->
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
                                                    <td style="padding: 6px 0; font-size: 13px; color: #605e5c;">Commented by:</td>
                                                    <td style="padding: 6px 0; font-size: 14px; font-weight: 600; color: #323130;">{{encodedAuthorName}}</td>
                                                </tr>
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
                                            You are receiving this email because you are the requester or assignee on this task in Sprintle.<br>
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
