using MediatR;

namespace Collaboration.Application.Interfaces.Commands;

public interface ICommand : IRequest<Unit> { }
public interface ICommand<out TResponse> : IRequest<TResponse> { }