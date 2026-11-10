using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("RaceDayApi", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ApiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
else app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();