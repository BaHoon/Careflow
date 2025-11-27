namespace CareFlow.Core.Interfaces
{
    public interface IService<TRequest, TResponse>
    {
        Task<TResponse> ExecuteAsync(TRequest request);
    }
}
