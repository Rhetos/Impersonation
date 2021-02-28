# Rhetos.Impersonation Release notes

## 5.0.0 (TO BE RELEASED)

Breaking changes:

1. Removed class ImpersonationUserInfo, use IImpersonationUserInfo instead.
2. IImpersonationUserInfo provides properties OriginalUsername and IsImpersonated instead of ImpersonatedBy and AuthenticatedUserName.
   * Code that tested if `ImpersonatedBy != null` should check IsImpersonated instead.
   * Code that used AuthenticatedUserName or ImpersonatedBy, should use OriginalUsername instead. Both old properties provided the same value, but ImpersonatedBy was *null* if the impersonation was not active.

Internal improvements:

* Migrated to .NET 5 for compatibility with Rhetos 5.
