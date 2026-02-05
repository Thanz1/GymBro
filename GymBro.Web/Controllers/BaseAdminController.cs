using GymBro.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using GymBro.Web.Helpers;

namespace GymBro.Web.Controllers
{
    // Tất cả Admin Controller phải kế thừa class này
    public class BaseAdminController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session.GetObject<User>("User");

            // Nếu chưa đăng nhập hoặc không phải Admin -> Đá về trang Login
            if (user == null || user.Role != "Admin")
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }

            base.OnActionExecuting(context);
        }
    }
}