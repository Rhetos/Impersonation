# Rhetos.Impersonation

Rhetos.Impersonation is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides functionality for impersonating another user in order to execute something with another user's permissions (for testing purposes) and/or behalf of another user.

Contents:

1. [Installation and configuration](#installation-and-configuration)
2. [Usage](#usage)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## Installation and configuration

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.Impersonation**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  The Rhetos server can install the package directly from there, if the gallery is listed in *RhetosPackageSources.config* file.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).

Impersonation plugin adds 3 new claims:

* *ClaimResource*: 'Common.Impersonate',  *ClaimRight*: 'Execute' - claim which allows authenticated user to impersonate another user.
* *ClaimResource*: 'Common.Impersonate',  *ClaimRight*: 'IncreasePermissions' - claim which allows authenticated user to impersonate another user which has more permissions then himself.
* *ClaimResource*: 'Common.StopImpersonating',  *ClaimRight*: 'Execute' - claim which allows impersonated user to stop impersonation. Every user in the system should have permission for this claim.

## Usage

To invoke impersonation you have to call **Common.Impersonate** action providing *UserName* parameter. Action returns **Impersonation** cookie in the response. You'll have to provide this cookie in every other request in order to impersonate another user. In order to stop impersonation you'll have to call **Common.StopImpersonating** action.

To retrieve username of impersonated user in your MVC application, your MVC application will have to have same machine key as Rhetos application. The code which extracts impersonated username from *Impersonation* cookie is listed bellow.

```csharp

private class ImpersonationInfo
{
    public string Authenticated { get; set; }
    public string Impersonated { get; set; }
    public DateTime Expires { get; set; }
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
    var impersontedInfo = Newtonsoft.JsonConvert.DeserializeObject<ImpersonationInfo>(json);

    if (impersontedInfo.Expires < DateTime.Now)
        return null;

    return impersontedInfo.Impersonated;
}
```
