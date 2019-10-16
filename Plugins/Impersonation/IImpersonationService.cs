using Rhetos.Processing;

namespace Impersonation
{
    public interface IImpersonationService
    {
        void Impersonate(string userName);
        void StopImpersonating();
    }
}