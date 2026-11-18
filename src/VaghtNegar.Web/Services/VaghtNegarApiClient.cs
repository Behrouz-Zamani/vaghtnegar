using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace VaghtNegar.Web.Services;

public sealed class VaghtNegarApiClient(IJSRuntime jsRuntime)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var clinics = await TryGetCollectionAsync<AdminClinicItem>("bff/api/v1/clinics", cancellationToken);
        var doctors = await TryGetCollectionAsync<AdminDoctorItem>("bff/api/v1/doctors", cancellationToken);
        return new ApiDashboardSummary(clinics.Count, doctors.Count,
            clinics.Take(4).Select(x => x.Name).ToArray(),
            doctors.Take(4).Select(x => $"{x.FirstName} {x.LastName}").ToArray());
    }

    public Task<List<AdminClinicItem>> GetClinicsAsync(CancellationToken ct = default) =>
        TryGetCollectionAsync<AdminClinicItem>("bff/api/v1/clinics", ct);

    public Task<List<AdminDoctorItem>> GetDoctorsAsync(CancellationToken ct = default) =>
        TryGetCollectionAsync<AdminDoctorItem>("bff/api/v1/doctors", ct);

    public Task<AdminMutationResult> CreateClinicAsync(string name, string slug, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/clinics/", new { name, slug }, ct);

    public Task<AdminMutationResult> UpdateClinicAsync(Guid id, string name, string slug, CancellationToken ct = default) =>
        SendAsync("PUT", $"bff/api/v1/clinics/{id}", new { name, slug }, ct);

    public Task<AdminMutationResult> SetClinicActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        SendAsync("PATCH", $"bff/api/v1/clinics/{id}/active", new { isActive }, ct);

    public Task<AdminMutationResult> CreateDoctorAsync(Guid clinicId, string firstName, string lastName, string? medicalCode, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/doctors/", new { clinicId, firstName, lastName, medicalCode }, ct);

    public Task<AdminMutationResult> UpdateDoctorAsync(Guid id, string firstName, string lastName, string? medicalCode, CancellationToken ct = default) =>
        SendAsync("PUT", $"bff/api/v1/doctors/{id}", new { firstName, lastName, medicalCode }, ct);

    public Task<AdminMutationResult> SetDoctorActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        SendAsync("PATCH", $"bff/api/v1/doctors/{id}/active", new { isActive }, ct);

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

    private async Task<List<T>> TryGetCollectionAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            var response = await GetAsync(path, ct);
            if (response.Status is < 200 or >= 300) return [];
            using var document = JsonDocument.Parse(response.Body);
            return ExtractItems(document.RootElement)
                .Select(x => x.Deserialize<T>(JsonOptions))
                .Where(x => x is not null).Cast<T>().ToList();
        }
        catch (JSException) { return []; }
        catch (JsonException) { return []; }
    }

    private async Task<AdminMutationResult> SendAsync(string method, string path, object body, CancellationToken ct)
    {
        try
        {
            var response = await jsRuntime.InvokeAsync<BrowserApiResponse>(
                "vaghtNegarApi.request", ct, method, path, JsonSerializer.Serialize(body, JsonOptions));
            if (response.Status is >= 200 and < 300)
                return new AdminMutationResult(true, "عملیات با موفقیت انجام شد.");
            return new AdminMutationResult(false, ReadError(response.Body, response.Status));
        }
        catch (JSException)
        {
            return new AdminMutationResult(false, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.");
        }
    }

    private static string ReadError(string body, int status)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? $"خطای سرور ({status}).";
            if (document.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object)
                return string.Join(" ", errors.EnumerateObject().SelectMany(x =>
                    x.Value.EnumerateArray().Select(v => v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v))));
        }
        catch (JsonException) { }
        return status switch
        {
            401 => "نشست ورود منقضی شده است. دوباره وارد شوید.",
            403 => "دسترسی لازم برای این عملیات را ندارید.",
            409 => "این اطلاعات با یک رکورد موجود تداخل دارد.",
            _ => $"عملیات ناموفق بود (کد {status})."
        };
    }

    private ValueTask<BrowserApiResponse> GetAsync(string path, CancellationToken ct) =>
        jsRuntime.InvokeAsync<BrowserApiResponse>("vaghtNegarApi.get", ct, path);

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
public sealed record AdminMutationResult(bool Succeeded, string Message);
public sealed record AdminClinicItem(Guid Id, string Name, string Slug, bool IsActive);
public sealed record AdminDoctorItem(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, bool IsActive);
