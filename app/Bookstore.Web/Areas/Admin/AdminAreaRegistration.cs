
// This file is intentionally left as a placeholder.
// The MVC5 AdminAreaRegistration class has no ASP.NET Core equivalent.
// Area routing must be registered in Program.cs endpoint configuration.
// See the PORT-TODO above for the exact route pattern to add.

namespace Bookstore.Web.Areas
{
    // Legacy AreaRegistration removed — ASP.NET Core areas use:
    //   1. [Area("Admin")] attribute on each controller in the Admin area
    //   2. app.MapControllerRoute("Admin_default", "{area:exists}/{controller=Home}/{action=Index}/{id?}") in Program.cs
}