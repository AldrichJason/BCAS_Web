using BCAS.Application.Abstractions;
using BCAS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BCAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IProgramService, ProgramService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
