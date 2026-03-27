using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteExperience;

public sealed record DeleteExperienceCommand(Guid Id) : IRequest;
