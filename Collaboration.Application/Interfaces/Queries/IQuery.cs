using MediatR;

namespace Collaboration.Application.Interfaces.Queries;

public interface IQuery<out TResponse> : IRequest<TResponse> { }