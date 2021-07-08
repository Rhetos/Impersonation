# Rhetos.Impersonation Release notes

## 5.0.0 (TO BE RELEASED)

### Breaking changes

1. Migrated from .NET Framework to .NET 5 and Rhetos 5.
2. Removed class ImpersonationUserInfo, use IImpersonationUserInfo instead.
3. IImpersonationUserInfo provides properties OriginalUsername and IsImpersonated instead of ImpersonatedBy and AuthenticatedUserName.
   * Code that tested if `ImpersonatedBy != null` should check IsImpersonated instead.
   * Code that used AuthenticatedUserName or ImpersonatedBy, should use OriginalUsername instead. Both old properties provided the same value, but ImpersonatedBy was *null* if the impersonation was not active.
4. If there is no HttpContext active, for example in unit tests, the impersonation will no longer use fake (in-memory) session storage.
