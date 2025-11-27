namespace CareFlow.Core.Interfaces
{
    public interface IPermissionValidator
    {
        Task<bool> HasPermissionAsync(Guid userId, string permission);
    }
}
