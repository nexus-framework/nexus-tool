# Migrations

```powershell
dotnet ef migrations add "Init" --output-dir .\Data\Migrations
dotnet ef database update
```

# FluentValidation

Fluent Validation can be used in 2 ways:

1. As a middleware
1. Manually by calling `Validate` or `ValidateAsync`

In this project, we're using the manual method due to the following reasons:

1. Auto validation is not asynchronous: If your validator contains asynchronous rules then your validator will not be
   able to run. You will receive an exception at runtime if you attempt to use an asynchronous validator with
   auto-validation.
1. Auto validation is MVC-only: Auto-validation only works with MVC Controllers and Razor Pages. It does not work with
   the more modern parts of ASP.NET such as Minimal APIs or Blazor.
1. Auto validation is hard to debug: The ‘magic’ nature of auto-validation makes it hard to debug/troubleshoot if
   something goes wrong as so much is done behind the scenes.

## References

[FluentValidation Documentation](https://docs.fluentvalidation.net/en/latest/aspnet.html)