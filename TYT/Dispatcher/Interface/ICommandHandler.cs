using MediatR;

namespace TYT.Dispatcher.Interface
{
    public interface ICommandHandler<in TCommand, TResponse>
        : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    { }
}
