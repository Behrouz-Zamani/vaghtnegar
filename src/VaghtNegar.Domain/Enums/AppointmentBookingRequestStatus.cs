namespace VaghtNegar.Domain.Enums;

public enum AppointmentBookingRequestStatus
{
    AwaitingLogin = 1,
    AwaitingPayment = 2,
    AwaitingReview = 3,
    Approved = 4,
    Rejected = 5,
    Expired = 6
}
