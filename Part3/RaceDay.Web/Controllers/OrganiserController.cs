using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;

[Role("Organiser")]
public class OrganiserController(ApiService api) : Controller
{
    int Uid => HttpContext.Session.GetInt32("UserId") ?? 0;

    async Task<List<EventVm>> Mine() =>
        (await api.GetAsync<List<EventVm>>("api/events") ?? new())
        .Where(e => e.OrganiserId == Uid).OrderBy(e => e.EventDate).ToList();

    async Task<EventVm?> Owned(int id)
    {
        var e = await api.GetAsync<EventVm>($"api/events/{id}");
        return e != null && e.OrganiserId == Uid ? e : null;
    }

    static object ToDto(EventFormVm v) => new
    {
        name = v.Name,
        description = v.Description,
        eventDate = v.EventDate,
        location = v.Location,
        eventType = v.EventType,
        distanceKm = v.DistanceKm
    };

    IActionResult NotYours()
    {
        TempData["Error"] = "We couldn't find that event.";
        return RedirectToAction("Events");
    }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Index()
    {
        var events = await Mine();
        var counts = new Dictionary<int, int>();
        foreach (var e in events)
            counts[e.EventId] = (await api.GetAsync<List<EnrolmentVm>>($"api/events/{e.EventId}/enrolments"))?.Count ?? 0;
        ViewBag.Counts = counts;
        return View(events);
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events() => View(await Mine());

    [HttpGet]
    public IActionResult CreateEvent() =>
        View("EventForm", new EventFormVm { EventDate = DateTime.Today.AddDays(30).AddHours(6), EventType = "Run" });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid) return View("EventForm", vm);
        var (s, body) = await api.SendAsync(HttpMethod.Post, "api/events", ToDto(vm));
        if (s != 201)
        {
            ModelState.AddModelError("", "We couldn't save the event. Please check the details and try again.");
            return View("EventForm", vm);
        }
        if (vm.Banner is { Length: > 0 })
        {
            using var doc = JsonDocument.Parse(body);
            var id = doc.RootElement.GetProperty("eventId").GetInt32();
            if (await api.UploadAsync($"api/events/{id}/banner", vm.Banner) != 200)
                TempData["Error"] = "Event saved, but the banner upload failed. Use a JPG, PNG or WEBP under 5MB.";
        }
        if (TempData["Error"] == null) TempData["Success"] = "Event created.";
        return RedirectToAction("Events");
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var e = await Owned(id);
        if (e == null) return NotYours();
        return View("EventForm", new EventFormVm
        {
            EventId = e.EventId,
            Name = e.Name,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            EventType = e.EventType,
            DistanceKm = e.DistanceKm,
            BannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid) return View("EventForm", vm);
        var (s, _) = await api.SendAsync(HttpMethod.Put, $"api/events/{vm.EventId}", ToDto(vm));
        if (s != 200)
        {
            ModelState.AddModelError("", "We couldn't update the event. Please check the details and try again.");
            return View("EventForm", vm);
        }
        if (vm.Banner is { Length: > 0 } &&
            await api.UploadAsync($"api/events/{vm.EventId}/banner", vm.Banner) != 200)
            TempData["Error"] = "Event saved, but the banner upload failed. Use a JPG, PNG or WEBP under 5MB.";
        if (TempData["Error"] == null) TempData["Success"] = "Event updated.";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var (s, _) = await api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        if (s == 204) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = "We couldn't delete that event.";
        return RedirectToAction("Events");
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int eventId)
    {
        var e = await Owned(eventId);
        return e == null ? NotYours() : View(e);
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a name, a distance and a fee.";
            return RedirectToAction("Categories", new { eventId });
        }
        var (s, _) = await api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories",
            new { name = vm.Name, distanceKm = vm.DistanceKm, fee = vm.Fee });
        if (s == 201) TempData["Success"] = "Category added.";
        else TempData["Error"] = "We couldn't add that category.";
        return RedirectToAction("Categories", new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int categoryId, int eventId)
    {
        var (s, _) = await api.SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}");
        if (s == 204) TempData["Success"] = "Category removed.";
        else TempData["Error"] = "We couldn't remove that category. It may already have enrolments.";
        return RedirectToAction("Categories", new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var e = await Owned(eventId);
        if (e == null) return NotYours();
        ViewBag.Event = e;
        return View(await api.GetAsync<List<EnrolmentVm>>($"api/events/{eventId}/enrolments") ?? new());
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int enrolmentId, int eventId, string status)
    {
        var (s, _) = await api.SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new { status });
        if (s == 200) TempData["Success"] = $"Enrolment marked {status}.";
        else TempData["Error"] = "We couldn't update that enrolment.";
        return RedirectToAction("Enrolments", new { eventId });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int eventId)
    {
        var e = await Owned(eventId);
        if (e == null) return NotYours();
        ViewBag.Event = e;
        var list = await api.GetAsync<List<EnrolmentVm>>($"api/events/{eventId}/enrolments") ?? new();
        return View(list.Where(x => x.Status != "Cancelled").ToList());
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int enrolmentId, int eventId, string finishTime, int position)
    {
        if (string.IsNullOrWhiteSpace(finishTime) || position < 1)
        {
            TempData["Error"] = "Enter a finish time and a position of 1 or more.";
            return RedirectToAction("Results", new { eventId });
        }
        var (s, _) = await api.SendAsync(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", new { finishTime, position });
        if (s == 201) TempData["Success"] = "Result published.";
        else if (s == 409) TempData["Error"] = "A result has already been captured for that participant.";
        else TempData["Error"] = "We couldn't save that result. Check the time format (hh:mm:ss).";
        return RedirectToAction("Results", new { eventId });
    }
}