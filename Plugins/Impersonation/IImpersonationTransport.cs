namespace Impersonation
{
    public interface IImpersonationTransport
    {
        void SetImpersonation(string impersonatedUser);
        void RemoveImpersonation();
    }
}