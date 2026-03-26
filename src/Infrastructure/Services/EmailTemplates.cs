namespace MyHomeSolution.Infrastructure.Services;

public static class EmailTemplates
{
    private static string WrapInLayout(string title, string bodyContent)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{title}}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f6f8;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f8;">
            <tr>
              <td align="center" style="padding:40px 20px;">
                <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.08);overflow:hidden;max-width:600px;width:100%;">
                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#1a73e8,#0d47a1);padding:32px 40px;text-align:center;">
                      <h1 style="margin:0;color:#ffffff;font-size:28px;font-weight:700;letter-spacing:-0.5px;">🏠 MyHome</h1>
                    </td>
                  </tr>
                  <!-- Body -->
                  <tr>
                    <td style="padding:40px;">
                      {{bodyContent}}
                    </td>
                  </tr>
                  <!-- Footer -->
                  <tr>
                    <td style="background-color:#f8f9fa;padding:24px 40px;border-top:1px solid #e9ecef;">
                      <p style="margin:0;color:#6c757d;font-size:12px;line-height:1.5;text-align:center;">
                        This email was sent by MyHome. If you did not request this, you can safely ignore it.<br />
                        &copy; {{DateTime.UtcNow.Year}} MyHome. All rights reserved.
                      </p>
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

    public static string EmailConfirmation(string userName, string confirmationUrl)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">Verify Your Email Address</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          Welcome to MyHome! Please confirm your email address to activate your account and get started.
        </p>
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 24px;">
          <tr>
            <td style="border-radius:6px;background-color:#1a73e8;">
              <a href="{{confirmationUrl}}" target="_blank" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">
                Verify Email Address
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0 0 8px;color:#6c757d;font-size:13px;line-height:1.5;">
          If the button above doesn't work, copy and paste this link into your browser:
        </p>
        <p style="margin:0 0 24px;word-break:break-all;">
          <a href="{{confirmationUrl}}" style="color:#1a73e8;font-size:13px;">{{confirmationUrl}}</a>
        </p>
        <p style="margin:0;color:#6c757d;font-size:13px;line-height:1.5;">
          This link will expire in 24 hours. If you didn't create an account, no action is needed.
        </p>
        """;

        return WrapInLayout("Verify Your Email - MyHome", body);
    }

    public static string PasswordReset(string userName, string resetUrl)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">Reset Your Password</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          We received a request to reset the password for your MyHome account. Click the button below to create a new password.
        </p>
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 24px;">
          <tr>
            <td style="border-radius:6px;background-color:#1a73e8;">
              <a href="{{resetUrl}}" target="_blank" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">
                Reset Password
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0 0 8px;color:#6c757d;font-size:13px;line-height:1.5;">
          If the button above doesn't work, copy and paste this link into your browser:
        </p>
        <p style="margin:0 0 24px;word-break:break-all;">
          <a href="{{resetUrl}}" style="color:#1a73e8;font-size:13px;">{{resetUrl}}</a>
        </p>
        <p style="margin:0;color:#6c757d;font-size:13px;line-height:1.5;">
          This link will expire in 1 hour. If you didn't request a password reset, you can safely ignore this email — your password will remain unchanged.
        </p>
        """;

        return WrapInLayout("Reset Your Password - MyHome", body);
    }

    public static string PasswordChanged(string userName)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">Password Changed Successfully</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          Your MyHome account password has been changed successfully. If you made this change, no further action is needed.
        </p>
        <div style="background-color:#fff3cd;border:1px solid #ffc107;border-radius:6px;padding:16px;margin-bottom:24px;">
          <p style="margin:0;color:#856404;font-size:14px;line-height:1.5;">
            <strong>⚠️ Didn't make this change?</strong><br />
            If you did not change your password, your account may be compromised. Please reset your password immediately and contact support.
          </p>
        </div>
        """;

        return WrapInLayout("Password Changed - MyHome", body);
    }

    public static string AccountDeleted(string userName)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">We're Sorry to See You Go</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          Your MyHome account and all associated data have been permanently deleted as requested. This action cannot be undone.
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          We truly appreciate the time you spent with us. If you ever decide to come back, we'd love to welcome you again — just create a new account anytime.
        </p>
        <div style="background-color:#e8f4fd;border:1px solid #1a73e8;border-radius:6px;padding:16px;margin-bottom:24px;">
          <p style="margin:0;color:#0d47a1;font-size:14px;line-height:1.5;">
            <strong>What was deleted:</strong><br />
            Your profile, tasks, bills, shopping lists, notifications, connections, and all shared data have been permanently removed from our systems.
          </p>
        </div>
        <p style="margin:0;color:#6c757d;font-size:13px;line-height:1.5;">
          If you did not request this deletion or believe this was done in error, please contact us immediately.
        </p>
        """;

        return WrapInLayout("Account Deleted - MyHome", body);
    }

    public static string DemoEmailConfirmation(string userName, string confirmationUrl)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">Welcome to Your MyHome Demo!</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 16px;color:#495057;font-size:15px;line-height:1.6;">
          You've signed up for a <strong>24-hour demo account</strong> on MyHome! This is a great way to explore all the features before committing.
        </p>
        <div style="background-color:#e8f5e9;border:1px solid #4caf50;border-radius:6px;padding:16px;margin-bottom:24px;">
          <p style="margin:0;color:#2e7d32;font-size:14px;line-height:1.5;">
            <strong>🎉 What's included in your demo:</strong><br />
            • Pre-loaded tasks, bills, budgets, and shopping lists<br />
            • Fake friend connections so you can see sharing in action<br />
            • Realistic data covering the past month so charts look real<br />
            • Full access to every feature for 24 hours
          </p>
        </div>
        <div style="background-color:#fff3cd;border:1px solid #ffc107;border-radius:6px;padding:16px;margin-bottom:24px;">
          <p style="margin:0;color:#856404;font-size:14px;line-height:1.5;">
            <strong>⏰ Important:</strong> Your demo account and all its data will be <strong>automatically deleted after 24 hours</strong>. A countdown timer will be visible at the top of every page. After expiration, you're welcome to sign up again for another demo or create a permanent account.
          </p>
        </div>
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 24px;">
          <tr>
            <td style="border-radius:6px;background-color:#1a73e8;">
              <a href="{{confirmationUrl}}" target="_blank" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">
                Verify Email &amp; Start Demo
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0 0 8px;color:#6c757d;font-size:13px;line-height:1.5;">
          If the button above doesn't work, copy and paste this link into your browser:
        </p>
        <p style="margin:0 0 24px;word-break:break-all;">
          <a href="{{confirmationUrl}}" style="color:#1a73e8;font-size:13px;">{{confirmationUrl}}</a>
        </p>
        """;

        return WrapInLayout("Welcome to Your MyHome Demo!", body);
    }

    public static string DemoExpired(string userName)
    {
        var body = $$"""
        <h2 style="margin:0 0 16px;color:#1a1a2e;font-size:22px;font-weight:600;">Your Demo Session Has Ended</h2>
        <p style="margin:0 0 12px;color:#495057;font-size:15px;line-height:1.6;">
          Hi {{userName}},
        </p>
        <p style="margin:0 0 16px;color:#495057;font-size:15px;line-height:1.6;">
          Your 24-hour MyHome demo has come to an end. Your demo account and all associated data have been automatically removed from our systems.
        </p>
        <p style="margin:0 0 24px;color:#495057;font-size:15px;line-height:1.6;">
          <strong>Thank you</strong> for taking the time to explore MyHome! We hope you enjoyed the experience.
        </p>
        <div style="background-color:#e8f4fd;border:1px solid #1a73e8;border-radius:6px;padding:16px;margin-bottom:24px;">
          <p style="margin:0;color:#0d47a1;font-size:14px;line-height:1.5;">
            <strong>What's next?</strong><br />
            • <strong>Create a permanent account</strong> to keep your data forever<br />
            • <strong>Try another demo</strong> — the same email can be used again<br />
            • <strong>Tell a friend</strong> — sharing is caring! 🏠
          </p>
        </div>
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 24px;">
          <tr>
            <td style="border-radius:6px;background-color:#1a73e8;">
              <a href="https://saidrustom.ca/register" target="_blank" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">
                Create a Permanent Account
              </a>
            </td>
          </tr>
        </table>
        <p style="margin:0;color:#6c757d;font-size:13px;line-height:1.5;">
          We truly appreciate your interest in MyHome. If you have any feedback, we'd love to hear it!
        </p>
        """;

        return WrapInLayout("Demo Session Ended — MyHome", body);
    }
}
