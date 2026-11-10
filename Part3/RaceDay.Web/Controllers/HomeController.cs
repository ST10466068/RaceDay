using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;

public class HomeController(ApiService api) : Controller
{
    public async Task<IActionResult> Index(string? q, string? type, DateTime? from)
    {
        var events = await api.GetAsync<List<EventVm>>("api/events");
        if (events == null)
        {
            ViewBag.Offline = true;
            events = new();
        }
        var list = events.Where(e => e.EventDate >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(q))
            list = list.Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                   e.Location.Contains(q, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type)) list = list.Where(e => e.EventType == type);
        if (from.HasValue) list = list.Where(e => e.EventDate >= from.Value);
        ViewBag.Q = q; ViewBag.Type = type; ViewBag.From = from?.ToString("yyyy-MM-dd");
        return View(list.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var e = await api.GetAsync<EventVm>($"api/events/{id}");
        if (e == null)
        {
            TempData["Error"] = "We couldn't find that event.";
            return RedirectToAction("Index");
        }
        return View(e);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        if (HttpContext.Session.GetString("Role") != "Participant")
        {
            TempData["Error"] = "Please log in as a Participant to enrol.";
            return RedirectToAction("Login", "Account");
        }
        var (status, _) = await api.SendAsync(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");
        if (status == 201) TempData["Success"] = "You're in! Your enrolment is pending confirmation.";
        else if (status == 409) TempData["Error"] = "You're already enrolled in that category.";
        else TempData["Error"] = "We couldn't enrol you right now. Please try again.";
        return RedirectToAction("Details", new { id = eventId });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}