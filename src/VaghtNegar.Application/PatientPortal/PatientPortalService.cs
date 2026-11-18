namespace VaghtNegar.Application.PatientPortal;

public sealed class PatientPortalService(IPatientPortalRepository repository)
{
    public Task<PatientProfileDto?> GetProfileAsync(string identityUserId, CancellationToken cancellationToken)
    {
        ValidateIdentity(identityUserId);
        return repository.GetProfileAsync(identityUserId.Trim(), cancellationToken);
    }

    public async Task<PatientAppointmentsPageDto> GetAppointmentsAsync(
        string identityUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identityUserId);
        if (page < 1) throw new ArgumentException("شماره صفحه باید حداقل ۱ باشد.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");

        var result = await repository.GetAppointmentsAsync(identityUserId.Trim(), page, pageSize, cancellationToken);
        return result ?? throw new PatientPortalProfileNotFoundException();
    }

    public async Task<PatientAppointmentDto> CancelAppointmentAsync(
        string identityUserId,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identityUserId);
        if (appointmentId == Guid.Empty) throw new ArgumentException("شناسه نوبت معتبر نیست.");

        try
        {
            var result = await repository.CancelAppointmentAsync(identityUserId.Trim(), appointmentId, cancellationToken);
            return result ?? throw new PatientPortalAppointmentNotFoundException();
        }
        catch (InvalidOperationException ex)
        {
            throw new PatientPortalInvalidAppointmentStateException(ex.Message);
        }
    }

    private static void ValidateIdentity(string identityUserId)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            throw new ArgumentException("شناسه هویتی کاربر در توکن معتبر نیست.");
    }
}

public sealed class PatientPortalProfileNotFoundException()
    : Exception("پرونده فعال بیمار به حساب کاربری فعلی متصل نیست.");

public sealed class PatientPortalAppointmentNotFoundException()
    : Exception("نوبت موردنظر در پرونده شما یافت نشد.");

public sealed class PatientPortalInvalidAppointmentStateException(string message)
    : Exception(message);
