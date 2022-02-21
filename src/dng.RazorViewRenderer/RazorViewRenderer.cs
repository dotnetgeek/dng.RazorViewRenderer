using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace dng.RazorViewRenderer;

public class RazorViewRenderer : IRazorViewRenderer
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RazorViewRenderer(
       IHttpContextAccessor httpContextAccessor,
        IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider)
    {
        _razorViewEngine = razorViewEngine;
        _tempDataProvider = tempDataProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> RenderViewToStringAsync<TModel>(
        string viewName,
        TModel model)
    {
        var actionContext = GetActionContext();

        var view = FindView(actionContext, viewName, _razorViewEngine);

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

    private IView FindView(
        ActionContext actionContext,
        string viewName,
        IRazorViewEngine viewEngine)
    {
        var getViewResult = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false);
        if (getViewResult.Success)
            return getViewResult.View;

        var findViewResult = viewEngine.FindView(actionContext, viewName, isMainPage: false);
        if (findViewResult.Success)
            return findViewResult.View;

        var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
        var errorMessage = string.Join(
            Environment.NewLine,
            new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations)); ;

        throw new InvalidOperationException(errorMessage);
    }

    private ActionContext GetActionContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var actionContext = httpContext?.RequestServices.GetService<IActionContextAccessor>()?.ActionContext;

        if (actionContext != null)
            return actionContext;

        var routeData = new RouteData();
        routeData.Routers.Add(new RouteCollection());

        return new ActionContext(httpContext, routeData, new ActionDescriptor());
    }
}
