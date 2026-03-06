using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Domain.Common;

public interface IBillable
{
    Guid Id { get; }
    ICollection<Bill> Bills { get; set; }
}
