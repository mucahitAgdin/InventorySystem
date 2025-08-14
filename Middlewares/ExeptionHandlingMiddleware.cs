using System.Net;
using Serilog;

namespace InventorySystem.Middleware
{
    // Uygulama genelinde yakalanmayan hataları yakalar ve loglar
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                // Yapılandırılmış log: yol, kullanıcı, ip
                Log.ForContext("Path", ctx.Request.Path.Value)
                   .ForContext("User", ctx.User?.Identity?.Name ?? "anonymous")
                   .ForContext("IP", ctx.Connection.RemoteIpAddress?.ToString())
                   .Error(ex, "Unhandled exception");

                // Kullanıcıya dost bir sayfa
                ctx.Response.Clear();
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(
                    "<h3>Beklenmeyen bir hata oluştu</h3><p>Lütfen tekrar deneyin.</p>");
            }
        }
    }

    public static class ExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
