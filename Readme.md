# Rhetos Impersonation

Rhetos.Impersonation is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides functionality for impersonating another user in order to execute something with another user's permissions (for testing purposes) and/or behalf of another user.

Rhetos.Host.AspNet.Impersonation is an extension of Rhetos.Impersonation, for Rhetos web applications with ASP.NET.

Contents:

1. [Installation and configuration](#installation-and-configuration)
2. [Usage](#usage)
3. [Impersonated user information in other applications](#impersonated-user-information-in-other-applications)
4. [How to contribute](#how-to-contribute)
   1. [Building and testing the source code](#building-and-testing-the-source-code)

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

## Installation and configuration

Installing this package to a web application that uses Rhetos impersonation:

1. Add "**Rhetos.Host.AspNet.Impersonation**" NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.
2. Extend the Rhetos services configuration (at `services.AddRhetosHost`) with the impersonation service: `.AddImpersonation()`.
3. Extend the application with new endpoints : `.UseRhetosImpersonation()` in the `Startup.Configure` method. It is important to call `.UseRhetosImpersonation()` before `.UseEndpoints()`.

If there is a separate library built with Rhetos, add **Rhetos.Impersonation** NuGet package to the library project.

Configure impersonation options in `AddImpersonation` delegate parameter.
See [ImpersonationOptions](https://github.com/Rhetos/Impersonation/blob/master/src/Rhetos.Host.AspNet.Impersonation/ImpersonationOptions.cs) class.
Example:

```cs
.AddImpersonation(options =>
    {
        Configuration.Bind(ImpersonationOptions.DefaultSectionName, options); // Reads standard app settings.
        options.ApiExplorerGroupName = "impersonation"; // Manual configuration override in code.
    })
```

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

## How to contribute

Contributions are very welcome. The easiest way is to fork this repo, and then
make a pull request from your fork. The first time you make a pull request, you
may be asked to sign a Contributor Agreement.
For more info see [How to Contribute](https://github.com/Rhetos/Rhetos/wiki/How-to-Contribute) on Rhetos wiki.

### Building and testing the source code

* Note: This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
  You don't need to build it from source in order to use it in your application.
* To build the package from source, run `Clean.bat`, `Build.bat` and `Test.bat`.
* For the test script to work, you need to create an empty database and
  a settings file `test\TestApp\ConnectionString.local.json`
  with the database connection string (configuration key "ConnectionStrings:RhetosConnectionString").
* The build output is a NuGet package in the "Install" subfolder.
