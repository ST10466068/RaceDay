using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RaceDay.Web.Services;

public class ApiService(IHttpClientFactory factory, IHttpContextAccessor accessor)
{
    static readonly JsonSerializerOptions Opt = new(JsonSerializerDefaults.Web);

    HttpClient Client()
    {
        var c = factory.CreateClient("RaceDayApi");
        var token = accessor.HttpContext?.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var r = await Client().GetAsync(url);
            if (!r.IsSuccessStatusCode) return default;
            return JsonSerializer.Deserialize<T>(await r.Content.ReadAsStringAsync(), Opt);
        }
        catch { return default; }
    }

    public async Task<(int Status, string Body)> SendAsync(HttpMethod method, string url, object? body = null)
    {
        try
        {
            var req = new HttpRequestMessage(method, url);
            if (body != null)
                req.Content = new StringContent(JsonSerializer.Serialize(body, Opt), Encoding.UTF8, "application/json");
            var r = await Client().SendAsync(req);
            return ((int)r.StatusCode, await r.Content.ReadAsStringAsync());
        }
        catch { return (0, ""); }
    }

    public async Task<int> UploadAsync(string url, IFormFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var sc = new StreamContent(file.OpenReadStream());
            sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(sc, "file", file.FileName);
            var r = await Client().PostAsync(url, content);
            return (int)r.StatusCode;
        }
        catch { return 0; }
    }
}

public class RoleAttribute(string role) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext ctx)
    {
        var s = ctx.HttpContext.Session;
        if (string.IsNullOrEmpty(s.GetString("Token")))
        {
            ctx.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (s.GetString("Role") != role)
        {
            if (ctx.Controller is Controller c) c.TempData["Error"] = "You don't have access to that page.";
            ctx.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}