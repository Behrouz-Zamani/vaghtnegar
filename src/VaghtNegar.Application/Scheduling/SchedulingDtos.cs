namespace VaghtNegar.Application.Scheduling;

public sealed record WorkingHourDto(Guid Id, Guid DoctorId, int DayOfWeek, string DayName, string StartTime, string EndTime, int SlotDurationMinutes, bool IsActive);
public sealed record WorkingHourInput(int DayOfWeek, string StartTime, string EndTime, int SlotDurationMinutes);
public sealed record AvailableSlotDto(string JalaliDate, string DayName, string StartTime, string EndTime, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);