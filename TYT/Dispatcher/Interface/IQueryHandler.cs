using MediatR;

namespace TYT.Dispatcher.Interface
{
    public interface IQueryHandler<in TQuery, TResponse>
        : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    { }
}
