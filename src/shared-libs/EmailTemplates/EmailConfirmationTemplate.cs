using System.Net;

namespace EmailTemplates
{
    /// <summary>
    /// Provides HTML template generation for user email confirmation emails using the application frontend design system.
    /// </summary>
    public static class EmailConfirmationTemplate
    {
        /// <summary>
        /// Default email subject for email confirmation.
        /// </summary>
        public const string DefaultSubject = "Confirm your email address - Sprintle";

        /// <summary>
        /// Generates a responsive, styled HTML email body for confirming a user's email address.
        /// </summary>
        /// <param name="confirmationUrl">The destination URL for confirming the email address.</param>
        /// <param name="recipientEmail">The optional recipient email address or username to display in the greeting.</param>
        /// <returns>A fully-styled HTML email document matching frontend design tokens.</returns>
        public static string Render(string confirmationUrl, string? recipientEmail = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(confirmationUrl);

            var encodedUrl = WebUtility.HtmlEncode(confirmationUrl);
            var greetingText = string.IsNullOrWhiteSpace(recipientEmail)
                ? "Hello,"
                : $"Hello <strong style=\"color: #323130;\">{WebUtility.HtmlEncode(recipientEmail)}</strong>,";

            return $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Confirm Your Email - Sprintle</title>
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
                                            <h1 style="margin: 0; font-size: 24px; font-weight: 600; color: #323130; line-height: 1.3;">Confirm your email address</h1>
                                        </td>
                                    </tr>

                                    <!-- Body Description -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 24px; font-size: 15px; line-height: 1.6; color: #605e5c;">
                                            <p style="margin: 0 0 14px 0;">{{greetingText}}</p>
                                            <p style="margin: 0;">Thank you for registering with Sprintle. Please confirm your email address to complete your account setup and ensure secure access to your tasks and workspaces.</p>
                                        </td>
                                    </tr>

                                    <!-- Primary CTA Button -->
                                    <tr>
                                        <td align="center" style="padding: 12px 0 32px 0;">
                                            <table role="presentation" border="0" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td align="center" style="border-radius: 4px; background-color: #0078d4;">
                                                        <a href="{{encodedUrl}}" target="_blank" style="display: inline-block; padding: 12px 32px; font-family: 'Segoe UI', 'Inter', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; font-size: 14px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 4px; border: 1px solid #0078d4; letter-spacing: 0.2px; text-align: center;">Confirm Email</a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>

                                    <!-- Secondary Direct Link Fallback -->
                                    <tr>
                                        <td align="left" style="padding-bottom: 28px; border-top: 1px solid #edebe9; padding-top: 24px; font-size: 13px; line-height: 1.5; color: #605e5c;">
                                            <p style="margin: 0 0 8px 0; font-weight: 500; color: #323130;">Having trouble with the button?</p>
                                            <p style="margin: 0 0 8px 0;">Copy and paste the following URL into your browser:</p>
                                            <p style="margin: 0; word-break: break-all;">
                                                <a href="{{encodedUrl}}" target="_blank" style="color: #0078d4; text-decoration: none;">{{encodedUrl}}</a>
                                            </p>
                                        </td>
                                    </tr>

                                    <!-- Security Notice & Footer -->
                                    <tr>
                                        <td align="left" style="font-size: 12px; line-height: 1.5; color: #a19f9d;">
                                            <p style="margin: 0 0 8px 0;">If you did not create a Sprintle account or request this verification, please disregard this email.</p>
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
    }
}
