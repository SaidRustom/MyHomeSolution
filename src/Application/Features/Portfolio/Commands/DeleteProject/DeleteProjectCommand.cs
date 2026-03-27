using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest;
