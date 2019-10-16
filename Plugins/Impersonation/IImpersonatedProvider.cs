namespace Impersonation
{
    public interface IImpersonatedProvider
    {
        string ImpersonatedUserName { get; }
    }
}