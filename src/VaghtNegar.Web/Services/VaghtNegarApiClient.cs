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

    public Task<ApiReadResult<List<AdminClinicItem>>> GetClinicsResultAsync(CancellationToken ct = default) =>
        TryGetCollectionResultAsync<AdminClinicItem>("bff/api/v1/clinics", ct);

    public Task<ApiReadResult<List<AdminDoctorItem>>> GetDoctorsResultAsync(CancellationToken ct = default) =>
        TryGetCollectionResultAsync<AdminDoctorItem>("bff/api/v1/doctors", ct);

    public Task<ApiReadResult<List<AdminDoctorItem>>> GetPublicDoctorsResultAsync(CancellationToken ct = default) =>
        TryGetCollectionResultAsync<AdminDoctorItem>("public-bff/api/v1/doctors", ct);

    public Task<ApiReadResult<List<AvailableSlotItem>>> GetPublicAvailableSlotsResultAsync(
        Guid doctorId, string jalaliDate, CancellationToken ct = default) =>
        TryGetCollectionResultAsync<AvailableSlotItem>(
            $"public-bff/api/v1/doctors/{doctorId}/schedule/available-slots?date={Uri.EscapeDataString(jalaliDate)}", ct);

    public Task<ApiReadResult<BookingHoldCreatedDto>> CreateBookingHoldAsync(
        Guid doctorId, DateTimeOffset startsAtUtc, string? patientNote, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingHoldCreatedDto>("POST", $"public-bff/api/v1/booking-requests/{doctorId}",
            new CreateBookingHoldRequest(startsAtUtc, patientNote), ct);

    public Task<ApiReadResult<BookingRequestDto>> GetBookingHoldAsync(string token, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingRequestDto>("GET", $"public-bff/api/v1/booking-requests/{Uri.EscapeDataString(token)}", null, ct);

    public Task<ApiReadResult<BookingRequestDto>> ClaimBookingHoldAsync(string token, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingRequestDto>("POST", $"bff/api/v1/booking-requests/{Uri.EscapeDataString(token)}/claim",
            new Dictionary<string, object>(), ct);

    public Task<AdminMutationResult> SubmitBookingReceiptAsync(
        string token, string fileName, string contentType, string base64Content, CancellationToken ct = default) =>
        SendAsync("POST", $"bff/api/v1/booking-requests/{Uri.EscapeDataString(token)}/receipt",
            new SubmitPaymentReceiptRequest(fileName, contentType, base64Content), ct);

    public Task<ApiReadResult<BookingRequestsPageDto>> GetMyBookingRequestsAsync(int page = 1, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingRequestsPageDto>("GET", $"bff/api/v1/booking-requests/me?page={page}&pageSize=20", null, ct);

    public Task<ApiReadResult<BookingRequestDto>> GetMyBookingRequestAsync(Guid requestId, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingRequestDto>("GET", $"bff/api/v1/me/booking-requests/{requestId}", null, ct);

    public Task<AdminMutationResult> SubmitMyBookingReceiptAsync(
        Guid requestId, string fileName, string contentType, string base64Content, CancellationToken ct = default) =>
        SendAsync("POST", $"bff/api/v1/me/booking-requests/{requestId}/receipt",
            new SubmitPaymentReceiptRequest(fileName, contentType, base64Content), ct);

    public Task<ApiReadResult<BookingRequestsPageDto>> GetPendingBookingReviewsAsync(int page = 1, CancellationToken ct = default) =>
        RequestItemResultAsync<BookingRequestsPageDto>("GET", $"bff/api/v1/admin/booking-requests/pending-review?page={page}&pageSize=20", null, ct);

    public Task<ApiReadResult<ReceiptImageDto>> GetBookingReceiptAsync(Guid requestId, CancellationToken ct = default) =>
        RequestItemResultAsync<ReceiptImageDto>("GET", $"bff/api/v1/admin/booking-requests/{requestId}/receipt", null, ct);

    public Task<AdminMutationResult> ApproveBookingPaymentAsync(Guid requestId, CancellationToken ct = default) =>
        SendAsync("POST", $"bff/api/v1/admin/booking-requests/{requestId}/approve", new Dictionary<string, object>(), ct);

    public Task<AdminMutationResult> RejectBookingPaymentAsync(Guid requestId, string reason, CancellationToken ct = default) =>
        SendAsync("POST", $"bff/api/v1/admin/booking-requests/{requestId}/reject", new RejectBookingPaymentRequest(reason), ct);

    public Task<AdminMutationResult> CreateClinicAsync(string name, string slug, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/clinics/", new { name, slug }, ct);

    public Task<AdminMutationResult> UpdateClinicAsync(Guid id, string name, string slug, CancellationToken ct = default) =>
        SendAsync("PUT", $"bff/api/v1/clinics/{id}", new { name, slug }, ct);

    public Task<AdminMutationResult> SetClinicActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        SendAsync("PATCH", $"bff/api/v1/clinics/{id}/active", new { isActive }, ct);

    public Task<AdminMutationResult> CreateDoctorAsync(Guid clinicId, string firstName, string lastName, string? medicalCode, long visitFeeToman, CancellationToken ct = default) =>
        SendAsync("POST", "bff/api/v1/doctors/", new { clinicId, firstName, lastName, medicalCode, visitFeeToman }, ct);

    public Task<AdminMutationResult> UpdateDoctorAsync(Guid id, string firstName, string lastName, string? medicalCode, long visitFeeToman, CancellationToken ct = default) =>
        SendAsync("PUT", $"bff/api/v1/doctors/{id}", new { firstName, lastName, medicalCode, visitFeeToman }, ct);

    public Task<AdminMutationResult> SetDoctorActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        SendAsync("PATCH", $"bff/api/v1/doctors/{id}/active", new { isActive }, ct);

    public Task<AdminMutationResult> LinkDoctorIdentityAsync(Guid id, string? identityUserId, CancellationToken ct = default) =>
        SendAsync("PUT", $"bff/api/v1/doctors/{id}/identity", new { identityUserId }, ct);

    public Task<ApiReadResult<AdminDoctorItem>> GetMyDoctorResultAsync(CancellationToken ct = default) =>
        TryGetItemResultAsync<AdminDoctorItem>("bff/api/v1/me/doctor", ct, allowNotFound: true);

    public async Task<AdminDoctorItem?> GetMyDoctorAsync(CancellationToken ct = default)
    {
        var result = await GetMyDoctorResultAsync(ct);
        return result.Succeeded ? result.Data : null;
    }

    public Task<ApiReadResult<PatientProfileDto>> GetMyPatientProfileResultAsync(CancellationToken ct = default) =>
        TryGetItemResultAsync<PatientProfileDto>("bff/api/v1/me/patient", ct, allowNotFound: true);

    public async Task<PatientProfileDto?> GetMyPatientProfileAsync(CancellationToken ct = default)
    {
        var result = await GetMyPatientProfileResultAsync(ct);
        return result.Succeeded ? result.Data : null;
    }

    public async Task<ApiReadResult<PatientAppointmentsPageDto>> GetMyAppointmentsResultAsync(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var response = await GetAsync($"bff/api/v1/me/appointments?page={page}&pageSize=20", ct);
            if (response.Status is < 200 or >= 300)
                return new ApiReadResult<PatientAppointmentsPageDto>(false, null, ReadError(response.Body, response.Status));
            var data = JsonSerializer.Deserialize<PatientAppointmentsPageDto>(response.Body, JsonOptions);
            return data is null
                ? new ApiReadResult<PatientAppointmentsPageDto>(false, null, "پاسخ دریافتی از سرور خالی است.")
                : new ApiReadResult<PatientAppointmentsPageDto>(true, data, null);
        }
        catch (JSException)
        {
            return new ApiReadResult<PatientAppointmentsPageDto>(false, null, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.");
        }
        catch (JsonException)
        {
            return new ApiReadResult<PatientAppointmentsPageDto>(false, null, "پاسخ دریافتی از سرور قابل پردازش نیست.");
        }
    }

    public async Task<PatientAppointmentsPageDto?> GetMyAppointmentsAsync(int page = 1, CancellationToken ct = default)
    {
        var result = await GetMyAppointmentsResultAsync(page, ct);
        return result.Succeeded ? result.Data : null;
    }

    public Task<AdminMutationResult> CancelMyAppointmentAsync(Guid id, CancellationToken ct = default) =>
        SendAsync("PATCH", $"bff/api/v1/me/appointments/{id}/cancel", new Dictionary<string, object>(), ct);

    public Task<AdminMutationResult> BookMyAppointmentAsync(Guid doctorId, DateTimeOffset startsAtUtc, string? patientNote, CancellationToken ct = default) =>
        SendAsync("POST", $"bff/api/v1/doctors/{doctorId}/appointments", new { startsAtUtc, patientNote }, ct);

    public Task<ApiReadResult<List<WorkingHourItem>>> GetMyWorkingHoursResultAsync(CancellationToken ct = default) =>
        TryGetCollectionResultAsync<WorkingHourItem>("bff/api/v1/me/doctor/schedule/working-hours", ct);

    public async Task<List<WorkingHourItem>> GetMyWorkingHoursAsync(CancellationToken ct = default)
    {
        var result = await GetMyWorkingHoursResultAsync(ct);
        return result.Succeeded && result.Data is not null ? result.Data : [];
    }

    public Task<ApiReadResult<List<AvailableSlotItem>>> GetAvailableSlotsResultAsync(
        Guid doctorId, string jalaliDate, CancellationToken ct = default)
    {
        var path = $"bff/api/v1/doctors/{doctorId}/schedule/available-slots?date={Uri.EscapeDataString(jalaliDate)}";
        return TryGetCollectionResultAsync<AvailableSlotItem>(path, ct);
    }

    public Task<AdminMutationResult> ReplaceMyWorkingHoursAsync(
        IReadOnlyList<WorkingHourInputItem> items, CancellationToken ct = default) =>
        SendAsync("PUT", "bff/api/v1/me/doctor/schedule/working-hours", new { items }, ct);

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
        return SendAsync("PATCH", $"bff/api/v1/appointments/{id}/{action}", new Dictionary<string, object>(), ct);
    }

    private async Task<ApiReadResult<T>> RequestItemResultAsync<T>(
        string method, string path, object? body, CancellationToken ct) where T : class
    {
        try
        {
            var response = method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                ? await GetAsync(path, ct)
                : await jsRuntime.InvokeAsync<BrowserApiResponse>(
                    "vaghtNegarApi.request", ct, method, path, JsonSerializer.Serialize(body ?? new Dictionary<string, object>(), JsonOptions));
            if (response.Status is < 200 or >= 300)
                return new ApiReadResult<T>(false, null, ReadError(response.Body, response.Status));
            var data = JsonSerializer.Deserialize<T>(response.Body, JsonOptions);
            return data is null
                ? new ApiReadResult<T>(false, null, "پاسخ دریافتی از سرور خالی است.")
                : new ApiReadResult<T>(true, data, null);
        }
        catch (JSException)
        {
            return new ApiReadResult<T>(false, null, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.");
        }
        catch (JsonException)
        {
            return new ApiReadResult<T>(false, null, "پاسخ دریافتی از سرور قابل پردازش نیست.");
        }
    }

    private async Task<ApiReadResult<T>> TryGetItemResultAsync<T>(
        string path, CancellationToken ct, bool allowNotFound = false) where T : class
    {
        try
        {
            var response = await GetAsync(path, ct);
            if (response.Status == 404 && allowNotFound)
                return new ApiReadResult<T>(false, null, null, true);
            if (response.Status is < 200 or >= 300)
                return new ApiReadResult<T>(false, null, ReadError(response.Body, response.Status));
            var item = JsonSerializer.Deserialize<T>(response.Body, JsonOptions);
            return item is null
                ? new ApiReadResult<T>(false, null, "پاسخ دریافتی از سرور خالی است.")
                : new ApiReadResult<T>(true, item, null);
        }
        catch (JSException)
        {
            return new ApiReadResult<T>(false, null, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.");
        }
        catch (JsonException)
        {
            return new ApiReadResult<T>(false, null, "پاسخ دریافتی از سرور قابل پردازش نیست.");
        }
    }

    private async Task<ApiReadResult<List<T>>> TryGetCollectionResultAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            var response = await GetAsync(path, ct);
            if (response.Status is < 200 or >= 300)
                return new ApiReadResult<List<T>>(false, null, ReadError(response.Body, response.Status));

            using var document = JsonDocument.Parse(response.Body);
            var items = ExtractItems(document.RootElement)
                .Select(x => x.Deserialize<T>(JsonOptions))
                .Where(x => x is not null).Cast<T>().ToList();

            return new ApiReadResult<List<T>>(true, items, null);
        }
        catch (JSException)
        {
            return new ApiReadResult<List<T>>(false, null, "ارتباط با سرور برقرار نشد. اتصال API را بررسی کنید.");
        }
        catch (JsonException)
        {
            return new ApiReadResult<List<T>>(false, null, "پاسخ دریافتی از سرور قابل پردازش نیست.");
        }
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

public sealed record ApiReadResult<T>(bool Succeeded, T? Data, string? Error, bool NotFound = false);
public sealed record BrowserApiResponse(int Status, string Body);
public sealed record ApiDashboardSummary(int ClinicCount, int DoctorCount, IReadOnlyList<string> Clinics, IReadOnlyList<string> Doctors);
public sealed record ApiCallResult(bool Succeeded, string? Error, IReadOnlyList<AppointmentListItem> Appointments, int Page, int TotalCount, int TotalPages);
public sealed record AppointmentListItem(Guid Id, Guid DoctorId, Guid PatientId, string Doctor, string Patient, string MobileNumber, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string StatusCode, string Status, string Time);
public sealed record AdminMutationResult(bool Succeeded, string Message);
public sealed record AdminClinicItem(Guid Id, string Name, string Slug, bool IsActive);
public sealed record AdminDoctorItem(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, long VisitFeeToman, bool HasLinkedIdentity, bool IsActive);
public sealed record PatientListItem(Guid Id, string FirstName, string LastName, string MobileNumber, DateOnly? BirthDate, bool IsActive);
public sealed record AvailableSlotItem(string JalaliDate, string DayName, string StartTime, string EndTime, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

public sealed record WorkingHourItem(Guid Id, Guid DoctorId, int DayOfWeek, string DayName, string StartTime, string EndTime, int SlotDurationMinutes, bool IsActive);
public sealed record WorkingHourInputItem(int DayOfWeek, string StartTime, string EndTime, int SlotDurationMinutes);

public sealed record PatientProfileDto(Guid Id, string FirstName, string LastName, string MobileNumber, DateOnly? BirthDate);
public sealed record PatientAppointmentDto(Guid Id, Guid ClinicId, Guid DoctorId, string DoctorFullName, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status, string? PatientNote, DateTimeOffset CreatedAtUtc);
public sealed record PatientAppointmentsPageDto(IReadOnlyList<PatientAppointmentDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record CreateBookingHoldRequest(DateTimeOffset StartsAtUtc, string? PatientNote);
public sealed record SubmitPaymentReceiptRequest(string FileName, string ContentType, string Base64Content);
public sealed record RejectBookingPaymentRequest(string Reason);
public sealed record BookingHoldCreatedDto(Guid RequestId, string AccessToken, DateTimeOffset HoldExpiresAtUtc, long VisitFeeToman, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status);
public sealed record BookingRequestDto(Guid Id, Guid DoctorId, string DoctorFullName, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, long VisitFeeToman, string Status, DateTimeOffset HoldExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReceiptSubmittedAtUtc, string? AdminNote, bool HasReceipt);
public sealed record BookingRequestsPageDto(IReadOnlyList<BookingRequestDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record ReceiptImageDto(string ContentType, string FileName, string Base64Content);
