using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Domain.Entities;

public sealed class AppointmentPayment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public long AmountToman { get; private set; }
    public AppointmentPaymentStatus Status { get; private set; } = AppointmentPaymentStatus.AwaitingReceipt;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string? ReceiptReference { get; private set; }
    public string? ReceiptContentType { get; private set; }
    public string? ReceiptImageBase64 { get; private set; }
    public DateTimeOffset? ReceiptSubmittedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNote { get; private set; }
    public Appointment Appointment { get; private set; } = null!;

    private AppointmentPayment() { }

    public AppointmentPayment(Guid appointmentId, Guid patientId, Guid doctorId, long amountToman, DateTimeOffset expiresAtUtc)
    {
        if (appointmentId == Guid.Empty || patientId == Guid.Empty || doctorId == Guid.Empty)
            throw new ArgumentException("Appointment, patient and doctor are required.");
        if (amountToman < 0) throw new ArgumentOutOfRangeException(nameof(amountToman));
        if (expiresAtUtc <= DateTimeOffset.UtcNow) throw new ArgumentException("Payment hold expiry must be in the future.");
        AppointmentId = appointmentId;
        PatientId = patientId;
        DoctorId = doctorId;
        AmountToman = amountToman;
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
    }

    public void SubmitReceipt(string reference, string contentType, string imageBase64)
    {
        EnsureNotExpired();
        if (Status != AppointmentPaymentStatus.AwaitingReceipt)
            throw new InvalidOperationException("رسید این درخواست قبلاً ثبت شده یا وضعیت آن قابل تغییر نیست.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 100)
            throw new ArgumentException("شماره پیگیری یا شرح پرداخت معتبر نیست.");
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            throw new ArgumentException("فرمت تصویر رسید باید JPG، PNG یا WEBP باشد.");
        if (string.IsNullOrWhiteSpace(imageBase64) || imageBase64.Length > 1_500_000)
            throw new ArgumentException("تصویر رسید الزامی است و حجم آن نباید بیشتر از یک مگابایت باشد.");
        try { _ = Convert.FromBase64String(imageBase64); }
        catch (FormatException) { throw new ArgumentException("اطلاعات تصویر رسید معتبر نیست."); }

        ReceiptReference = reference.Trim();
        ReceiptContentType = contentType;
        ReceiptImageBase64 = imageBase64;
        ReceiptSubmittedAtUtc = DateTimeOffset.UtcNow;
        Status = AppointmentPaymentStatus.ReceiptSubmitted;
    }

    public void Approve(string reviewer)
    {
        if (Status != AppointmentPaymentStatus.ReceiptSubmitted)
            throw new InvalidOperationException("فقط رسیدهای ارسال‌شده قابل تأیید هستند.");
        Status = AppointmentPaymentStatus.Approved;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedBy = reviewer;
        ReviewNote = null;
    }

    public void Reject(string reviewer, string reason)
    {
        if (Status != AppointmentPaymentStatus.ReceiptSubmitted)
            throw new InvalidOperationException("فقط رسیدهای ارسال‌شده قابل رد هستند.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("علت رد رسید الزامی است و حداکثر ۵۰۰ کاراکتر دارد.");
        Status = AppointmentPaymentStatus.Rejected;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedBy = reviewer;
        ReviewNote = reason.Trim();
    }

    public void CancelBeforeApproval(string actor)
    {
        if (Status is not (AppointmentPaymentStatus.AwaitingReceipt or AppointmentPaymentStatus.ReceiptSubmitted))
            return;
        Status = AppointmentPaymentStatus.Cancelled;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ReviewedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor;
        ReviewNote = "درخواست نوبت پیش از تأیید پرداخت لغو شد.";
    }

    public bool ExpireIfDue(DateTimeOffset now)
    {
        if (Status != AppointmentPaymentStatus.AwaitingReceipt || ExpiresAtUtc > now)
            return false;
        Status = AppointmentPaymentStatus.Expired;
        return true;
    }

    private void EnsureNotExpired()
    {
        if (ExpiresAtUtc <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("مهلت ۱۰ دقیقه‌ای پرداخت به پایان رسیده است.");
    }
}
