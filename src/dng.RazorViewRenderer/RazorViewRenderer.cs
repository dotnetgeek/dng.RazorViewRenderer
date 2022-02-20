using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

using static System.String;

namespace dng.RazorViewRenderer;

public class RazorViewRenderer : IRazorViewRenderer
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RazorViewRenderer(
        IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _razorViewEngine = razorViewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderViewToStringAsync<TModel>(
        string viewName,
        TModel model)
    {
        var actionContext = GetActionContext();

        var viewEngineResult = _razorViewEngine.FindView(actionContext, viewName, false);

        if (!viewEngineResult.Success)
        {
            throw new InvalidOperationException(Format("Couldn't find view '{0}'", viewName));
        }

        var view = viewEngineResult.View;

        using (var output = new StringWriter())
        {
            var viewContext = new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                },
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                output,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext);

            return output.ToString();
        }
    }

    private ActionContext GetActionContext()
    {
        return new ActionContext(
            new DefaultHttpContext()
            {
                RequestServices = _serviceProvider
            },
            new RouteData(),
            new ActionDescriptor());
    }
}
