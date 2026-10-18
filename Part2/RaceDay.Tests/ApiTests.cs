using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;
using Microsoft.EntityFrameworkCore.Storage;

namespace RaceDay.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    static readonly InMemoryDatabaseRoot Root = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(s =>
        {
            var remove = s.Where(x =>
                x.ServiceType == typeof(DbContextOptions<RaceDayContext>) ||
                x.ServiceType == typeof(DbContextOptions) ||
                x.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")).ToList();
            foreach (var d in remove) s.Remove(d);

            s.AddDbContext<RaceDayContext>(o => o.UseInMemoryDatabase("RaceDayTests", Root));

            using var sp = s.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<RaceDayContext>().Database.EnsureCreated();
        });
    }
}

public class ApiTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    const string Pw = "Passw0rd!";

    static object Reg(string role, string email) =>
        new { firstName = "Test", lastName = "User", email, password = Pw, role };

    async Task<HttpClient> LoggedIn(string role)
    {
        var c = f.CreateClient();
        var email = $"{Guid.NewGuid()}@test.com";
        await c.PostAsJsonAsync("/api/auth/register", Reg(role, email));
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password = Pw });
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", j.GetProperty("token").GetString());
        return c;
    }

    static object NewEvent() => new
    {
        name = "Test Run",
        description = "d",
        eventDate = "2027-01-01T06:00:00",
        location = "Durban",
        eventType = "Run"
    };

    async Task<int> CreateEvent(HttpClient org)
    {
        var r = await org.PostAsJsonAsync("/api/events", NewEvent());
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eventId").GetInt32();
    }

    async Task<int> CreateCategory(HttpClient org, int eventId)
    {
        var r = await org.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "10km", distanceKm = 10, fee = 100 });
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("categoryId").GetInt32();
    }

    // ---------- Registration & login ----------
    [Fact]
    public async Task Register_ValidUser_Returns201()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/register", Reg("Participant", $"{Guid.NewGuid()}@t.com"));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var c = f.CreateClient();
        var email = $"{Guid.NewGuid()}@t.com";
        await c.PostAsJsonAsync("/api/auth/register", Reg("Participant", email));
        var r = await c.PostAsJsonAsync("/api/auth/register", Reg("Participant", email));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/register", Reg("Admin", $"{Guid.NewGuid()}@t.com"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var c = f.CreateClient();
        var email = $"{Guid.NewGuid()}@t.com";
        await c.PostAsJsonAsync("/api/auth/register", Reg("Organiser", email));
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password = Pw });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(j.GetProperty("token").GetString()));
        Assert.Equal("Organiser", j.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var c = f.CreateClient();
        var email = $"{Guid.NewGuid()}@t.com";
        await c.PostAsJsonAsync("/api/auth/register", Reg("Participant", email));
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ---------- Authentication ----------
    [Fact]
    public async Task Profile_WithoutToken_Returns401()
    {
        var r = await f.CreateClient().GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_WithToken_Returns200()
    {
        var c = await LoggedIn("Participant");
        var r = await c.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // ---------- Event management ----------
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var org = await LoggedIn("Organiser");
        var r = await org.PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var org = await LoggedIn("Organiser");
        var r = await org.PostAsJsonAsync("/api/events",
            new { name = "x", eventDate = "2027-01-01T06:00:00", location = "y", eventType = "Swim" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnEvent()
    {
        var org = await LoggedIn("Organiser");
        var id = await CreateEvent(org);
        var put = await org.PutAsJsonAsync($"/api/events/{id}",
            new { name = "Renamed", description = "d", eventDate = "2027-02-01T06:00:00", location = "CPT", eventType = "Cycle" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var del = await org.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent()
    {
        var owner = await LoggedIn("Organiser");
        var other = await LoggedIn("Organiser");
        var id = await CreateEvent(owner);
        var r = await other.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task GetEvents_IsPublic()
    {
        var r = await f.CreateClient().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // ---------- Role rejection ----------
    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var p = await LoggedIn("Participant");
        var r = await p.PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var org = await LoggedIn("Organiser");
        var catId = await CreateCategory(org, await CreateEvent(org));
        var r = await org.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_Returns401()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ---------- Enrolments ----------
    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var org = await LoggedIn("Organiser");
        var eventId = await CreateEvent(org);
        var catId = await CreateCategory(org, eventId);
        var p = await LoggedIn("Participant");

        var r = await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var mine = await (await p.GetAsync("/api/users/enrolments")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(catId, mine[0].GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, mine[0].GetProperty("eventId").GetInt32());

        var forOrg = await (await org.GetAsync($"/api/events/{eventId}/enrolments")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, forOrg.GetArrayLength());
    }

    [Fact]
    public async Task Participant_EnrolTwice_Returns409()
    {
        var org = await LoggedIn("Organiser");
        var catId = await CreateCategory(org, await CreateEvent(org));
        var p = await LoggedIn("Participant");
        await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        var r = await p.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var p = await LoggedIn("Participant");
        var r = await p.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ---------- Results ----------
    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantSeesIt()
    {
        var org = await LoggedIn("Organiser");
        var catId = await CreateCategory(org, await CreateEvent(org));
        var p = await LoggedIn("Participant");
        var en = await (await p.PostAsync($"/api/categories/{catId}/enrolments", null)).Content.ReadFromJsonAsync<JsonElement>();
        var enId = en.GetProperty("enrolmentId").GetInt32();

        var r = await org.PostAsJsonAsync($"/api/enrolments/{enId}/results", new { finishTime = "00:52:30", position = 5 });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var dup = await org.PostAsJsonAsync($"/api/enrolments/{enId}/results", new { finishTime = "00:50:00", position = 4 });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var mine = await (await p.GetAsync("/api/users/results")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, mine.GetArrayLength());
    }
}