namespace BlazorUI.Models.UserConnections;

public sealed record SendConnectionRequestModel
{
    public string AddresseeId { get; set; } = string.Empty;
}
