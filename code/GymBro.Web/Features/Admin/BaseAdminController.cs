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

        protected PaginationInfo SetupPagination(int? page, int totalItems, int pageSize = 10)
        {
            var pageNumber = page ?? 1;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return new PaginationInfo
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                SkipCount = (pageNumber - 1) * pageSize
            };
        }
    }

    public class PaginationInfo
    {
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
        public int SkipCount { get; init; }
    }
}
