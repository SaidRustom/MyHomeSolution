using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.DeleteSkill;

public sealed record DeleteSkillCommand(Guid Id) : IRequest;
