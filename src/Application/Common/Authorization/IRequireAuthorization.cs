namespace MyHomeSolution.Application.Common.Authorization;

public interface IRequireAuthorization
{
    string ResourceType { get; }
    Guid ResourceId { get; }
}
