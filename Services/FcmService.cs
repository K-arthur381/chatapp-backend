using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatApp.Server.Services;

public interface IFcmService
{
    Task SendPushAsync(string deviceToken, string title, string body, string? conversationId = null);
    Task SendMulticastAsync(List<string> tokens, string title, string body, string? conversationId = null);
}

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;
    private static bool _initialized = false;

    public FcmService(IConfiguration configuration, ILogger<FcmService> logger)
    {
        _logger = logger;

        if (!_initialized)
        {
            var credentialPath = configuration["Firebase:CredentialPath"];
            if (File.Exists(credentialPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
                _initialized = true;
                _logger.LogInformation("Firebase initialized successfully");
            }
            else
            {
                _logger.LogWarning("Firebase credential file not found at: {Path}", credentialPath);
            }
        }
    }

    public async Task SendPushAsync(string deviceToken, string title, string body, string? conversationId = null)
    {
        if (!_initialized) return;

        try
        {
            var message = new Message
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
                {
                    ["conversationId"] = conversationId ?? "",
                    ["click_action"] = "OPEN_CHAT"
                },
                // ✅ Android
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "chat_messages",
                        ClickAction = "OPEN_CHAT",
                        Sound = "default",
                        Color = "#2563eb"
                    }
                },
                // ✅ Web Push
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = title,
                        Body = body,
                        Icon = "/assets/icons/icon-96x96.png",
                        Badge = "/assets/icons/icon-72x72.png",
                        RequireInteraction = true
                    }
                    //,
                    //FcmOptions = new WebpushFcmOptions
                    //{
                    //    Link = $"/chat/{conversationId}"
                    //}
                }
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM sent successfully: {Response}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send failed for token: {Token}", deviceToken);
        }
    }

    public async Task SendMulticastAsync(List<string> tokens, string title, string body, string? conversationId = null)
    {
        foreach (var token in tokens)
        {
            await SendPushAsync(token, title, body, conversationId);
        }
    }
}