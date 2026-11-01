using WebLinhKien.Services;

namespace WebLinhKien.WebSockets;

public static class WebSocketServiceExtensions
{
    /// <summary>builder.Services.AddAdminWebSockets();</summary>
    public static IServiceCollection AddAdminWebSockets(this IServiceCollection services)
    {
        services.AddSingleton<AdminConnectionManager>();
        services.AddSingleton<IAdminNotifier, AdminNotifier>();
        return services;
    }

    /// <summary>app.UseAdminWebSockets(); đặt sau UseAuthentication() và UseAuthorization().</summary>
    public static IApplicationBuilder UseAdminWebSockets(this IApplicationBuilder app)
    {
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
        app.UseMiddleware<AdminWebSocketMiddleware>();
        return app;
    }
}
