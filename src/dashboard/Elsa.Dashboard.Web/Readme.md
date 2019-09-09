# Elsa Dashboard

This web project serves as a sample application to demonstrate how to use the Elsa MVC area provided by the `Elsa.Dashboard` package.

## Security
By default, the dashboard will only be reachable when requests are made from `localhost`. This is done by adding an MVC convention from `Startup`.

If you for some reason prefer to allow anonymous access, simply remove the convention.

More likely, you will want to apply the `AddAuthorizeFilterConvention` provided by `Elsa.Dashboard`, which automatically applies the `AuthorizeAttribute` to all controllers in the `Elsa` MVC area.

If that convention doesn't meet your requirements, you can of course implement your own MVC convention.

For more information on MVC conventions, see: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model?view=aspnetcore-2.2#conventions

The `AddAuthorizeFilterConvention` is inspired on the following story: https://joonasw.net/view/apply-authz-by-default.
