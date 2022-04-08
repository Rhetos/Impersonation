# Rhetos.Impersonation Release notes

## 5.1.0 (2022-04-08)

* Bugfix: NullReferenceException may occur if there is no HttpContext active, for example in a CLI utility.

## 5.0.0 (2022-03-25)

### Breaking changes

1. Migrated from .NET Framework to .NET 5 and Rhetos 5.
2. For Rhetos web application, add a dependency to "Rhetos.Host.AspNet.Impersonation" NuGet package, instead of "Rhetos.Impersonation".
   Rhetos.Impersonation NuGet package is used for libraries built with Rhetos that don't contain web API.
3. Removed class ImpersonationUserInfo, use IImpersonationUserInfo instead.
4. IImpersonationUserInfo provides properties OriginalUsername and IsImpersonated instead of ImpersonatedBy and AuthenticatedUserName.
   * Code that tested if `ImpersonatedBy != null` should check IsImpersonated instead.
   * Code that used AuthenticatedUserName or ImpersonatedBy, should use OriginalUsername instead. Both old properties provided the same value, but ImpersonatedBy was *null* if the impersonation was not active.
5. If there is no HttpContext active, for example in unit tests, the impersonation will no longer use fake (in-memory) session storage.
