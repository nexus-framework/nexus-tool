using Microsoft.EntityFrameworkCore;
using {{ROOT_NAMESPACE}}.Data;
using {{ROOT_NAMESPACE}}.Extensions;

namespace {{ROOT_NAMESPACE}};

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        CoreWebApplicationBuilder.BuildConfigureAndRun(
            args,
            configureDefaultMiddleware: true,
            preConfiguration: null,
            registerServices: (services, configuration, _) => { services.RegisterDependencies(configuration); },
            configureMiddleware: app =>
            {
                // DB Migration
                using IServiceScope scope = app.Services.CreateScope();
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();

                app.MapControllers();
            });
    }
}