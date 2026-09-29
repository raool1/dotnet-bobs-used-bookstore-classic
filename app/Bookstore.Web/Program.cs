using System.Reflection;
using Amazon.Rekognition;
using Amazon.S3;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bookstore.Common;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using NLog.Web;

// NLog: Setup NLog for dependency injection
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------------------------------------------------------------
    // Configuration: AWS Systems Manager Parameter Store
    // (replaces ConfigurationSetup.ConfigureConfiguration)
    // ---------------------------------------------------------------
    var ssmPath = "/" + Constants.AppName;
    if (builder.Configuration.GetValue<string>("Services:Database") == "aws"
        || builder.Configuration.GetValue<string>("Services:Authentication") == "aws"
        || builder.Configuration.GetValue<string>("Services:FileService") == "aws")
    {
        builder.Configuration.AddSystemsManager(configureSource =>
        {
            configureSource.Path = ssmPath;
            configureSource.Optional = true;
            configureSource.ReloadAfter = TimeSpan.FromMinutes(5);
        });
    }

    // ---------------------------------------------------------------
    // NLog: Wire up ASP.NET Core logging
    // (replaces LoggingSetup.ConfigureLogging)
    // ---------------------------------------------------------------
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ---------------------------------------------------------------
    // Autofac: Replace the built-in DI container
    // (replaces DependencyInjectionSetup.ConfigureDependencyInjection)
    // ---------------------------------------------------------------
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        var configuration = context.Configuration;
        var bookstoreConfig = new BookstoreConfiguration(configuration);

        // Register BookstoreConfiguration as a singleton instance
        containerBuilder.RegisterInstance(bookstoreConfig).AsSelf().SingleInstance();

        // Domain services
        containerBuilder.RegisterType<BookService>().As<IBookService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ReferenceDataService>().As<IReferenceDataService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<OfferService>().As<IOfferService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<AddressService>().As<IAddressService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ImageResizeService>().As<IImageResizeService>().InstancePerLifetimeScope();

        // Repositories
        containerBuilder.RegisterType<CustomerRepository>().As<ICustomerRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<AddressRepository>().As<IAddressRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<BookRepository>().As<IBookRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<OfferRepository>().As<IOfferRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<OrderRepository>().As<IOrderRepository>().InstancePerLifetimeScope();
        containerBuilder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>().InstancePerLifetimeScope();

        // Generic PaginatedList
        containerBuilder.RegisterGeneric(typeof(PaginatedList<>)).As(typeof(IPaginatedList<>)).InstancePerLifetimeScope();

        // File service: AWS S3 or local
        if (bookstoreConfig.GetSetting("Services/FileService") == "aws")
        {
            containerBuilder.RegisterType<AmazonS3Client>().As<IAmazonS3>().SingleInstance();
            containerBuilder.RegisterType<S3FileService>().As<IFileService>().InstancePerLifetimeScope();
        }
        else
        {
            var env = context.HostingEnvironment;
            var webRootPath = Path.Combine(env.ContentRootPath, "Content");
            containerBuilder.RegisterInstance(new LocalFileService(webRootPath)).As<IFileService>();
        }

        // Image validation service: AWS Rekognition or local
        if (bookstoreConfig.GetSetting("Services/ImageValidationService") == "aws")
        {
            containerBuilder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>().SingleInstance();
            containerBuilder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>().InstancePerLifetimeScope();
        }
        else
        {
            containerBuilder.RegisterType<LocalImageValidationService>().As<IImageValidationService>().InstancePerLifetimeScope();
        }
    });

    // ---------------------------------------------------------------
    // EF Core: Register ApplicationDbContext
    // ---------------------------------------------------------------
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection");
        options.UseSqlServer(connectionString);
    });

    // ---------------------------------------------------------------
    // MVC: Controllers with Views
    // (replaces FilterConfig.RegisterGlobalFilters — global [Authorize]
    //  is applied via a FallbackPolicy; HandleErrorAttribute is replaced
    //  by UseExceptionHandler middleware)
    // ---------------------------------------------------------------
    builder.Services.AddControllersWithViews();

    // ---------------------------------------------------------------
    // Authentication: Cookie + OpenID Connect (Cognito) or Local
    // (replaces AuthenticationSetup.ConfigureAuthentication)
    // ---------------------------------------------------------------
    var authMode = builder.Configuration.GetValue<string>("Services:Authentication");
    if (authMode == "aws")
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.MetadataAddress = builder.Configuration["Authentication:Cognito:MetadataAddress"];
            options.ClientId = builder.Configuration["Authentication:Cognito:LocalClientId"];
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
        });
    }
    else
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Authentication/Login";
            });
    }

    // Authorization: The legacy FilterConfig registered a global [Authorize] filter.
    // Replicate this with a fallback policy that requires authenticated users,
    // while still allowing [AllowAnonymous] on specific controllers/actions.
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // ---------------------------------------------------------------
    // Middleware pipeline
    // ---------------------------------------------------------------
    if (!app.Environment.IsDevelopment())
    {
        // Replaces Application_Error + HandleErrorAttribute
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();

    // [PORT-NOTE] Dropped BundleConfig.RegisterBundles — ASP.NET Core has no
    // Web Optimization bundling. Static assets are served directly from wwwroot/
    // (or Content/Scripts directories as-is). Reference individual CSS/JS files
    // in _Layout.cshtml instead of bundle virtual paths.
    app.UseStaticFiles();

    app.UseRouting();

    // Authentication & Authorization — order is load-bearing.
    app.UseAuthentication();
    app.UseAuthorization();

    // Local authentication middleware: when not using Cognito, this middleware
    // creates a ClaimsPrincipal for a hard-coded local dev user.
    if (app.Configuration.GetValue<string>("Services:Authentication") != "aws")
    {
        app.UseMiddleware<LocalAuthenticationMiddleware>();
    }

    // ---------------------------------------------------------------
    // Endpoint routing
    // (replaces RouteConfig.RegisterRoutes + AreaRegistration.RegisterAllAreas)
    // ---------------------------------------------------------------
    app.MapControllerRoute(
        name: "Admin_default",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
