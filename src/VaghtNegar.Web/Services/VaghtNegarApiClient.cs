using System.Net;
using System.Text.Json;
using Microsoft.JSInterop;

namespace VaghtNegar.Web.Services;

public sealed class VaghtNegarApiClient(IJSRuntime jsRuntime)
{
    public async Task<ApiDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var clinics = await TryGetCollectionAsync("bff/api/v1/clinics", cancellationToken);
        var doctors = await TryGetCollectionAsync("bff/api/v1/doctors", cancellationToken);
        return new ApiDashboardSummary(clinics.Count, doctors.Count,
            clinics.Take(4).Select(ReadDisplayName).ToArray(),
            doctors.Take(4).Select(ReadDisplayName).ToArray());
    }

    public async Task<ApiCallResult> GetAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetAsync("bff/api/v1/appointments?page=1&pageSize=6", cancellationToken);
        if (response.Status == (int)HttpStatusCode.Unauthorized)
            return new ApiCallResult(false, "نشست ورود منقضی شده است. دوباره وارد شوید.", []);
        if (response.Status == (int)HttpStatusCode.Forbidden)
            return new ApiCallResult(false, "نقش فعلی اجازه مشاهده فهرست نوبت‌ها را ندارد.", []);
        if (response.Status is < 200 or >= 300)
            return new ApiCallResult(false, $"دریافت نوبت‌ها ناموفق بود (کد {response.Status}).", []);

        using var document = JsonDocument.Parse(response.Body);
        return new ApiCallResult(true, null, ExtractItems(document.RootElement).Select(ReadAppointment).ToArray());
    }

    private async Task<List<JsonElement>> TryGetCollectionAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var response = await GetAsync(path, cancellationToken);
            if (response.Status is < 200 or >= 300) return [];
            using var document = JsonDocument.Parse(response.Body);
            return ExtractItems(document.RootElement);
        }
        catch (JSException) { return []; }
        catch (JsonException) { return []; }
    }

    private ValueTask<BrowserApiResponse> GetAsync(string path, CancellationToken cancellationToken) =>
        jsRuntime.InvokeAsync<BrowserApiResponse>("vaghtNegarApi.get", cancellationToken, path);

    private static List<JsonElement> ExtractItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray().Select(x => x.Clone()).ToList();
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var property in new[] { "items", "data", "results" })
                if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
                    return value.EnumerateArray().Select(x => x.Clone()).ToList();
        return [];
    }

    private static string ReadDisplayName(JsonElement item)
    {
        foreach (var key in new[] { "name", "fullName", "displayName", "firstName" })
            if (item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var name = value.GetString() ?? "";
                if (key == "firstName" && item.TryGetProperty("lastName", out var last))
                    name += " " + last.GetString();
                return name;
            }
        return "بدون عنوان";
    }

    private static AppointmentListItem ReadAppointment(JsonElement item)
    {
        var doctor = item.TryGetProperty("doctorFullName", out var doctorName) ? doctorName.GetString() ?? "پزشک" : "پزشک";
        var status = item.TryGetProperty("status", out var statusValue) ? statusValue.ToString() : "نامشخص";
        var time = item.TryGetProperty("startsAtUtc", out var startValue) &&
                   DateTimeOffset.TryParse(startValue.ToString(), out var parsed)
            ? parsed.ToLocalTime().ToString("HH:mm") : "--:--";
        var patient = item.TryGetProperty("patientFullName", out var patientName) ? patientName.GetString() ?? "بیمار" : "بیمار";
        return new AppointmentListItem(doctor, patient, time, status);
    }
}

public sealed record BrowserApiResponse(int Status, string Body);
public sealed record ApiDashboardSummary(int ClinicCount, int DoctorCount, IReadOnlyList<string> Clinics, IReadOnlyList<string> Doctors);
public sealed record ApiCallResult(bool Succeeded, string? Error, IReadOnlyList<AppointmentListItem> Appointments);
public sealed record AppointmentListItem(string Doctor, string Patient, string Time, string Status);
