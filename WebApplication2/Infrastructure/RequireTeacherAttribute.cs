using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApplication2.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireTeacherAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var login = session.GetString("Login");

            if (string.IsNullOrEmpty(login))
            {
                context.Result = new RedirectToActionResult("Index", "Authorization", null);
                return;
            }

            if (session.GetString("Role") != "Teacher")
            {
                context.Result = HttpMethods.IsGet(context.HttpContext.Request.Method)
                    ? new ViewResult { ViewName = "AccessDenied" }
                    : new ForbidResult();
            }
        }
    }
}
