using AsvsSecurityAuditor.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsvsSecurityAuditor.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
