using Microsoft.Extensions.DependencyInjection;

namespace dng.RazorViewRenderer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRazorViewToStringRenderer(
        this IServiceCollection services,
        Action<RazorViewRendererOptions> options)
    {
        var razorViewRendererOptions = new RazorViewRendererOptions();
        options(razorViewRendererOptions);
        services.Configure(options);
        services.AddTransient<IRazorViewRenderer, RazorViewRenderer>();
        return services;
    }
}
