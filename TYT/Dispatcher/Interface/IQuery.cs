using MediatR;

namespace TYT.Dispatcher.Interface
{
    public interface IQuery<out TResponse> : IRequest<TResponse> { }
}
