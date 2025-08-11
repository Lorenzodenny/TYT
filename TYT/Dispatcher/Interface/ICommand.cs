using MediatR;

namespace TYT.Dispatcher.Interface
{
    public interface ICommand<out TResponse> : IRequest<TResponse> { }
}
