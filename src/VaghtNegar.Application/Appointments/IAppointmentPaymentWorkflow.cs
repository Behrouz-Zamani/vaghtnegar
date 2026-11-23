namespace VaghtNegar.Application.Appointments;

public sealed record CreateBookingHoldRequest(DateTimeOffset StartsAtUtc, string? PatientNote);
public sealed record SubmitPaymentReceiptRequest(string FileName, string ContentType, string Base64Content);
public sealed record RejectBookingPaymentRequest(string Reason);

public sealed record BookingHoldCreatedDto(
    Guid RequestId, string AccessToken, DateTimeOffset HoldExpiresAtUtc, long VisitFeeToman,
    DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status);

public sealed record BookingRequestDto(
    Guid Id, Guid DoctorId, string DoctorFullName, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    long VisitFeeToman, string Status, DateTimeOffset HoldExpiresAtUtc, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReceiptSubmittedAtUtc, string? AdminNote, bool HasReceipt);

public sealed record BookingRequestsPageDto(
    IReadOnlyList<BookingRequestDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public interface IAppointmentPaymentWorkflow
{
    Task<BookingHoldCreatedDto> CreateHoldAsync(Guid doctorId, CreateBookingHoldRequest request, CancellationToken ct);
    Task<BookingRequestDto?> GetByTokenAsync(string token, CancellationToken ct);
    Task<BookingRequestDto> ClaimAsync(string token, string identityUserId, CancellationToken ct);
    Task<BookingRequestDto> SubmitReceiptAsync(string token, string identityUserId, SubmitPaymentReceiptRequest request, CancellationToken ct);
    Task<BookingRequestsPageDto> GetMyRequestsAsync(string identityUserId, int page, int pageSize, CancellationToken ct);
    Task<BookingRequestDto?> GetMyRequestAsync(Guid requestId, string identityUserId, CancellationToken ct);
    Task<BookingRequestDto> SubmitMyReceiptAsync(Guid requestId, string identityUserId, SubmitPaymentReceiptRequest request, CancellationToken ct);
    Task<BookingRequestsPageDto> GetPendingReviewAsync(int page, int pageSize, CancellationToken ct);
    Task<BookingRequestDto> ApproveAsync(Guid requestId, string reviewerIdentityUserId, CancellationToken ct);
    Task<BookingRequestDto> RejectAsync(Guid requestId, string reviewerIdentityUserId, string reason, CancellationToken ct);
    Task<(byte[] Content, string ContentType, string FileName)?> GetReceiptAsync(Guid requestId, CancellationToken ct);
}
