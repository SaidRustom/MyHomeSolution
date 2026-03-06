namespace BlazorUI.Infrastructure.Navigation;

/// <summary>
/// Maps entity types and IDs to navigable page routes.
/// </summary>
public static class EntityNavigator
{
    /// <summary>
    /// Returns the relative URL for the given entity type and ID,
    /// or <c>null</c> if the entity type is not recognized.
    /// </summary>
    public static string? GetEntityUrl(string? entityType, Guid? entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId is null)
            return null;

        return entityType.ToLowerInvariant() switch
        {
            "bill" => $"/bills/{entityId}",
            "householdtask" or "task" or "taskitem" => $"/tasks/{entityId}",
            "shoppinglist" => $"/shopping-lists/{entityId}",
            "occurrence" or "taskoccurrence" => $"/occurrences/{entityId}",
            "user" => $"/admin/users/{entityId}",
            "userconnection" or "connection" => "/connections",
            "notification" => $"/notifications/{entityId}",
            _ => null
        };
    }
}
