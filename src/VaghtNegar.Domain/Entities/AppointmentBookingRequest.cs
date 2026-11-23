using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Domain.Entities;

public sealed class AppointmentBookingRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ClinicId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? IdentityUserId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public long VisitFeeToman { get; private set; }
    public string? PatientNote { get; private set; }
    public string AccessTokenHash { get; private set; } = string.Empty;
    public AppointmentBookingRequestStatus Status { get; private set; } = AppointmentBookingRequestStatus.AwaitingLogin;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset HoldExpiresAtUtc { get; private set; }
    public byte[]? ReceiptContent { get; private set; }
    public string? ReceiptContentType { get; private set; }
    public string? ReceiptFileName { get; private set; }
    public DateTimeOffset? ReceiptSubmittedAtUtc { get; private set; }
    public string? AdminNote { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedByIdentityUserId { get; private set; }

    private AppointmentBookingRequest() { }

    public AppointmentBookingRequest(
        Guid clinicId, Guid doctorId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc,
        long visitFeeToman, string? patientNote, string accessTokenHash)
    {
        if (clinicId == Guid.Empty || doctorId == Guid.Empty)
            throw new ArgumentException("Clinic and doctor are required.");
        if (startsAtUtc.Offset != TimeSpan.Zero || endsAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Appointment times must be UTC.");
        if (startsAtUtc <= DateTimeOffset.UtcNow || endsAtUtc <= startsAtUtc)
            throw new ArgumentException("Appointment time range is invalid.");
        if (visitFeeToman < 0) throw new ArgumentOutOfRangeException(nameof(visitFeeToman));
        if (string.IsNullOrWhiteSpace(accessTokenHash) || accessTokenHash.Length > 128)
            throw new ArgumentException("A valid booking token hash is required.", nameof(accessTokenHash));
        if (patientNote?.Length > 1000)
            throw new ArgumentException("Patient note cannot exceed 1000 characters.", nameof(patientNote));

        ClinicId = clinicId;
        DoctorId = doctorId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        VisitFeeToman = visitFeeToman;
        PatientNote = string.IsNullOrWhiteSpace(patientNote) ? null : patientNote.Trim();
        AccessTokenHash = accessTokenHash;
        HoldExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10);
    }

    public void AttachPatient(Guid patientId, string identityUserId)
    {
        EnsureHoldIsActive();
        if (Status != AppointmentBookingRequestStatus.AwaitingLogin)
            throw new InvalidOperationException("This booking request is no longer waiting for login.");
        if (patientId == Guid.Empty || string.IsNullOrWhiteSpace(identityUserId))
            throw new ArgumentException("A valid patient account is required.");
        PatientId = patientId;
        IdentityUserId = identityUserId.Trim();
        Status = AppointmentBookingRequestStatus.AwaitingPayment;
    }

    public void SubmitReceipt(byte[] content, string contentType, string fileName)
    {
        EnsureHoldIsActive();
        if (Status != AppointmentBookingRequestStatus.AwaitingPayment)
            throw new InvalidOperationException("This booking request is not waiting for payment.");
        if (content is null || content.Length == 0 || content.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Receipt image must be between 1 byte and 5 MB.");
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            throw new ArgumentException("Only JPEG, PNG or WebP receipt images are accepted.");
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 180)
            throw new ArgumentException("Receipt file name is invalid.");

        ReceiptContent = content;
        ReceiptContentType = contentType;
        ReceiptFileName = Path.GetFileName(fileName);
        ReceiptSubmittedAtUtc = DateTimeOffset.UtcNow;
        Status = AppointmentBookingRequestStatus.AwaitingReview;
    }

    public void Approve(string reviewerIdentityUserId)
    {
        if (Status != AppointmentBookingRequestStatus.AwaitingReview || PatientId is null)
            throw new InvalidOperationException("Only a submitted receipt for a linked patient can be approved.");
        if (string.IsNullOrWhiteSpace(reviewerIdentityUserId))
            throw new ArgumentException("Reviewer identity is required.", nameof(reviewerIdentityUserId));
        Status = AppointmentBookingRequestStatus.Approved;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedByIdentityUserId = reviewerIdentityUserId.Trim();
        AdminNote = null;
    }

    public void Reject(string reviewerIdentityUserId, string reason)
    {
        if (Status != AppointmentBookingRequestStatus.AwaitingReview)
            throw new InvalidOperationException("Only a submitted receipt can be rejected.");
        if (string.IsNullOrWhiteSpace(reviewerIdentityUserId))
            throw new ArgumentException("Reviewer identity is required.", nameof(reviewerIdentityUserId));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            throw new ArgumentException("A rejection reason of up to 1000 characters is required.", nameof(reason));
        Status = AppointmentBookingRequestStatus.Rejected;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedByIdentityUserId = reviewerIdentityUserId.Trim();
        AdminNote = reason.Trim();
    }

    public bool ExpireIfNeeded(DateTimeOffset now)
    {
        if (Status is not (AppointmentBookingRequestStatus.AwaitingLogin or AppointmentBookingRequestStatus.AwaitingPayment) ||
            HoldExpiresAtUtc > now)
            return false;
        Status = AppointmentBookingRequestStatus.Expired;
        return true;
    }

    private void EnsureHoldIsActive()
    {
        if (ExpireIfNeeded(DateTimeOffset.UtcNow))
            throw new InvalidOperationException("The 10-minute booking hold has expired.");
        if (Status is AppointmentBookingRequestStatus.Rejected or AppointmentBookingRequestStatus.Expired)
            throw new InvalidOperationException("This booking request is no longer active.");
    }
}
