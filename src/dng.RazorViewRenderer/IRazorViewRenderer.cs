using System.Threading.Tasks;

namespace dng.RazorViewRenderer;

public interface IRazorViewRenderer
{
    Task<string> RenderViewToStringAsync<TModel>(string name, TModel model);
}
