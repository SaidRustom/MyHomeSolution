namespace BlazorUI.Infrastructure.Realtime;

public static class HubConstants
{
    public const string TaskHubPath = "/hubs/tasks";
    public const string NotificationHubPath = "/hubs/notifications";

    public static class Methods
    {
        public const string TaskNotification = "TaskNotification";
        public const string OccurrenceNotification = "OccurrenceNotification";
        public const string UserNotification = "UserNotification";
    }

    public static class ServerMethods
    {
        public const string JoinTaskGroup = "JoinTaskGroup";
        public const string LeaveTaskGroup = "LeaveTaskGroup";
    }
}
