namespace Impersonation
{
    public interface IImpersonationContext
    {
        void CheckUserImpersonatePermission();
        void CheckImperionatedUserPermissions(string impersonatedUser);
    }
}