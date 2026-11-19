using System.Net;
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

    public async Task<ApiCallResult> GetAppointmentsAsync(
        int page = 1,
        Guid? patientId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?page={page}&pageSize=20";
        if (patientId.HasValue) query += $"&patientId={patientId.Value}";
        if (!string.IsNullOrWhiteSpace(status)) query += $"&status={Uri.EscapeDataString(status)}";

        try
        {
            var response = await GetAsync("bff/api/v1/appointments" + query, cancellationToken);
            if (response.Status is < 200 or >= 300)
                return new ApiCallResult(false, ReadError(response.Body, response.Status), [], page, 0, 0);

            using var document = JsonDocument.Parse(response.Body);
            var root = document.RootElement;
            var items = ExtractItems(root).Select(ReadAppointment).ToArray();
            var totalCount = ReadInt(root, "totalCount", items.Length);
            var totalPages = ReadInt(root, "totalPages", totalCount == 0 ? 0 : 1);
            var currentPage = ReadInt(root, "page", page);
            return new ApiCallResult(true, null, items, currentPage, totalCount, totalPages);
        }
        catch (JSException)
        {
            return new ApiCallResult(false, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.", [], page, 0, 0);
        }
        catch (JsonException)
        {
            return new ApiCallResult(false, "پاسخ دریافتی از سرور قابل پردازش نیست.", [], page, 0, 0);
        }
    }

    public async Task<List<PatientListItem>> GetPatientsAsync(string? search, CancellationToken ct = default)
    {
        var path = "bff/api/v1/patients/";
        if (!string.IsNullOrWhiteSpace(search))
            path += "?search=" + Uri.EscapeDataString(search.Trim());
        return await TryGetCollectionAsync<PatientListItem>(path, ct);
    }

    public Task<AdminMutationResult> CreatePatientAsync(
        string firstName, string lastName, string mobileNumber, DateOnly? birthDate, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/patients/", new { firstName, lastName, mobileNumber, birthDate }, ct);

    public async Task<List<AvailableSlotItem>> GetAvailableSlotsAsync(
        Guid doctorId, string jalaliDate, CancellationToken ct = default)
    {
        var path = $"bff/api/v1/doctors/{doctorId}/schedule/available-slots?date={Uri.EscapeDataString(jalaliDate)}";
        return await TryGetCollectionAsync<AvailableSlotItem>(path, ct);
    }

    public Task<AdminMutationResult> CreateReceptionAppointmentAsync(
        Guid doctorId, Guid patientId, DateTimeOffset startsAtUtc, string? patientNote, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/appointments", new { doctorId, patientId, startsAtUtc, patientNote }, ct);

    public Task<AdminMutationResult> ChangeAppointmentStatusAsync(
        Guid id, string action, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return Task.FromResult(new AdminMutationResult(false, "شناسه نوبت معتبر نیست."));

        var allowedActions = new[] { "confirm", "cancel", "complete", "no-show" };
        if (!allowedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(new AdminMutationResult(false, "عملیات تغییر وضعیت معتبر نیست."));

        // Send an explicit empty JSON object so the BFF forwards a PATCH body
        // consistently across browsers and hosting environments.
        return SendAsync("PATCH", $"bff/api/v1/appointments/{id}/{action}", new { }, ct);
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
            if (document.RootElement.TryGetProperty("title", out var title) &&
                title.ValueKind == JsonValueKind.String)
                return title.GetString() ?? $"خطای سرور ({status}).";
        }
        catch (JsonException) { }
        return status switch
        {
            401 => "نشست ورود منقضی شده است. دوباره وارد شوید.",
            403 => "دسترسی لازم برای این عملیات را ندارید.",
            404 => "رکورد موردنظر یافت نشد.",
            409 => "این زمان قبلاً رزرو شده یا اطلاعات با یک رکورد موجود تداخل دارد.",
            >= 500 => $"خطای داخلی سرور ({status}). جزئیات را در لاگ API بررسی کنید.",
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

    private static int ReadInt(JsonElement root, string name, int fallback) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(name, out var value) &&
        value.TryGetInt32(out var number) ? number : fallback;

    private static AppointmentListItem ReadAppointment(JsonElement item)
    {
        var doctor = ReadString(item, "doctorFullName", "پزشک");
        var patient = ReadString(item, "patientFullName", "بیمار");
        var mobile = ReadString(item, "patientMobileNumber", "");
        var statusCode = ReadString(item, "status", "Unknown");
        var startsAt = ReadDate(item, "startsAtUtc");
        var endsAt = ReadDate(item, "endsAtUtc");
        var id = ReadGuid(item, "id");
        var doctorId = ReadGuid(item, "doctorId");
        var patientId = ReadGuid(item, "patientId");
        var time = FormatTehranTime(startsAt);
        return new AppointmentListItem(id, doctorId, patientId, doctor, patient, mobile,
            startsAt, endsAt, statusCode, LocalizeStatus(statusCode), time);
    }

    private static string ReadString(JsonElement item, string name, string fallback) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback : fallback;

    private static Guid ReadGuid(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && Guid.TryParse(value.ToString(), out var result)
            ? result : Guid.Empty;

    private static DateTimeOffset ReadDate(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && DateTimeOffset.TryParse(value.ToString(), out var result)
            ? result : DateTimeOffset.MinValue;

    private static string LocalizeStatus(string status) => status switch
    {
        "Pending" => "در انتظار",
        "Confirmed" => "تأییدشده",
        "Cancelled" => "لغوشده",
        "Completed" => "انجام‌شده",
        "NoShow" => "عدم مراجعه",
        _ => status
    };

    private static string FormatTehranTime(DateTimeOffset utc)
    {
        if (utc == DateTimeOffset.MinValue) return "--:--";
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
        return TimeZoneInfo.ConvertTime(utc, zone).ToString("HH:mm");
    }
}

public sealed record BrowserApiResponse(int Status, string Body);
public sealed record ApiDashboardSummary(int ClinicCount, int DoctorCount, IReadOnlyList<string> Clinics, IReadOnlyList<string> Doctors);
public sealed record ApiCallResult(bool Succeeded, string? Error, IReadOnlyList<AppointmentListItem> Appointments, int Page, int TotalCount, int TotalPages);
public sealed record AppointmentListItem(Guid Id, Guid DoctorId, Guid PatientId, string Doctor, string Patient, string MobileNumber, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string StatusCode, string Status, string Time);
public sealed record AdminMutationResult(bool Succeeded, string Message);
public sealed record AdminClinicItem(Guid Id, string Name, string Slug, bool IsActive);
public sealed record AdminDoctorItem(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, bool IsActive);
public sealed record PatientListItem(Guid Id, string FirstName, string LastName, string MobileNumber, DateOnly? BirthDate, bool IsActive);
public sealed record AvailableSlotItem(string JalaliDate, string DayName, string StartTime, string EndTime, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);
