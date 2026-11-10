using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;

[Role("Participant")]
public class ParticipantController(ApiService api) : Controller
{
    public async Task<IActionResult> MyEnrolments() =>
        View(await api.GetAsync<List<EnrolmentVm>>("api/users/enrolments") ?? new());

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var (s, _) = await api.SendAsync(HttpMethod.Delete, $"api/enrolments/{id}");
        if (s == 204) TempData["Success"] = "Enrolment cancelled.";
        else TempData["Error"] = "We couldn't cancel that enrolment.";
        return RedirectToAction("MyEnrolments");
    }

    public async Task<IActionResult> MyResults() =>
        View(await api.GetAsync<List<ResultVm>>("api/users/results") ?? new());

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var p = await api.GetAsync<ProfileVm>("api/users/profile");
        if (p == null)
        {
            TempData["Error"] = "We couldn't load your profile.";
            return RedirectToAction("Index", "Home");
        }
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        var current = await api.GetAsync<ProfileVm>("api/users/profile");
        vm.ProfilePictureUrl = current?.ProfilePictureUrl;
        vm.Role = current?.Role;
        if (!ModelState.IsValid) return View(vm);

        var (s, _) = await api.SendAsync(HttpMethod.Put, "api/users/profile", new { vm.FirstName, vm.LastName, vm.Email });
        if (s == 200)
        {
            HttpContext.Session.SetString("Email", vm.Email.Trim().ToLower());
            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Profile");
        }
        ModelState.AddModelError("", s == 409 ? "That email is already in use." : "We couldn't update your profile. Please check your details.");
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction("Profile");
        }
        var s = await api.UploadAsync("api/users/profile/picture", file);
        if (s == 200) TempData["Success"] = "Profile picture updated.";
        else TempData["Error"] = "Upload failed. Use a JPG, PNG or WEBP image under 5MB.";
        return RedirectToAction("Profile");
    }
}