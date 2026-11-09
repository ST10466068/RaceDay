using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
namespace RaceDay.Web.Controllers;

public class AccountController(IHttpClientFactory factory) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var http = factory.CreateClient("RaceDayApi");
            var res = await http.PostAsync("api/auth/login", Json(new { email = vm.Email, password = vm.Password }));
            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                HttpContext.Session.SetString("Token", root.GetProperty("token").GetString()!);
                HttpContext.Session.SetInt32("UserId", root.GetProperty("userId").GetInt32());
                HttpContext.Session.SetString("Role", root.GetProperty("role").GetString()!);
                HttpContext.Session.SetString("Email", vm.Email);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", res.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Incorrect email or password."
                : "Something went wrong. Please try again.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "We can't reach the RaceDay service right now. Please try again shortly.");
        }
        return View(vm);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var http = factory.CreateClient("RaceDayApi");
            var res = await http.PostAsync("api/auth/register", Json(new
            {
                firstName = vm.FirstName,
                lastName = vm.LastName,
                email = vm.Email,
                password = vm.Password,
                role = vm.Role
            }));
            if (res.IsSuccessStatusCode)
            {
                TempData["Success"] = "Account created! Please log in.";
                return RedirectToAction("Login");
            }
            ModelState.AddModelError("", res.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "That email is already registered."
                : "We couldn't create your account. Please check your details.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "We can't reach the RaceDay service right now. Please try again shortly.");
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");
}