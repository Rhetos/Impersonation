# Rhetos.Impersonation

Rhetos.Impersonation is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides functionality for impersonating another user in order to execute something with another user's permissions (for testing purposes) and/or behalf of another user.

Contents:

1. [Installation and configuration](#installation-and-configuration)
2. [Usage](#usage)
3. [Impersonated user information in other applications](#impersonated-user-information-in-other-applications)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## Installation and configuration

Installing this package to a Rhetos web application:

1. Add 'Rhetos.Impersonation' NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery:
2. Extend Rhetos services configuration (at `services.AddRhetos`) with the impersonation service: `.AddImpersonation()`
3. Extend application with new endpoints : `.UseRhetosImpersonation()` in the `Startup.Configure` method. It is important to call `.UseRhetosImpersonation()` before `.UseEndpoints()`.

Impersonation plugin adds the following security claims:

* *ClaimResource*: 'Common.Impersonate',  *ClaimRight*: 'Execute' - claim which allows authenticated user to impersonate another user.
* *ClaimResource*: 'Common.Impersonate',  *ClaimRight*: 'IncreasePermissions' - claim which allows authenticated user to impersonate another user which has more permissions then himself.
* (version 4 and earlier) *ClaimResource*: 'Common.StopImpersonating',  *ClaimRight*: 'Execute' - claim which allows impersonated user to stop impersonation. Every user in the system should have permission for this claim.

## Usage

*Rhetos.Impersonation* provides web request impersonation.
It is not intended for in-process impersonation.

To **start impersonating** another user, call *Common.Impersonate* action providing *UserName* parameter.

* Send a POST request to `<base URL>/rest/Common/Impersonate` with body `{"UserName":"<impersonated user>"}`
* The action returns *Impersonation cookie* in the web response.
* Provide this cookie in the following web requests to impersonate that user (web browser will automatically provide the cookie).

In order to **stop impersonation**, call *Common.StopImpersonating* action.

* Send a POST request to `<base URL>/rest/Common/StopImpersonating`.

## Impersonated user information in other applications

*Version 5 and later:*
To retrieve the original and the impersonated user information, call web method GetImpersonationInfo.

* Send a GET request to `<base URL>/rest/Common/GetImpersonationInfo`. It returns a JSON object with properties Authenticated (the original user) and Impersonated (the impersonated user).

*Version 4 and earlier:*
To retrieve username of impersonated user in your MVC application, your MVC application will have to have same machine key as Rhetos application. The code which extracts impersonated username from *Impersonation* cookie is listed bellow.

```cs
private class ImpersonationInfo
{
    public string Authenticated { get; set; }
    public string Impersonated { get; set; }
}

public string GetImpersonatedUserName()
{
    if (HttpContext.Current == null)
        return null;

    var cookie = HttpContext.Current.Request.Cookies["Impersonation"];
    if (cookie == null)
        return null;

    if (string.IsNullOrWhiteSpace(cookie.Value))
        return null;

    var bytes = Convert.FromBase64String(cookie.Value);
    var output = System.Web.Security.MachineKey.Unprotect(bytes, "Rhetos.Impersonation");
    if (output == null || output.Length == 0)
        return null;

    var json = Encoding.UTF8.GetString(output);
    var impersonatedInfo = Newtonsoft.JsonConvert.DeserializeObject<ImpersonationInfo>(json);

    if (impersonatedInfo.Expires < DateTime.Now)
        return null;

    return impersonatedInfo.Impersonated;
}
```
