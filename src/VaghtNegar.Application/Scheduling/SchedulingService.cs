using System.Globalization;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Application.Scheduling;

public sealed class SchedulingService(ISchedulingRepository repository)
{
    private static readonly PersianCalendar PersianCalendar = new();
    private static readonly string[] PersianDayNames = ["یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"];

    public async Task<IReadOnlyList<WorkingHourDto>> GetWorkingHoursAsync(Guid doctorId, CancellationToken ct)
    {
        EnsureDoctorId(doctorId);
        var hours = await repository.GetWorkingHoursAsync(doctorId, ct);
        return hours.OrderBy(x => PersianWeekOrder(x.DayOfWeek))
            .ThenBy(x => x.StartTime)
            .Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<WorkingHourDto>> ReplaceWorkingHoursAsync(Guid doctorId, IReadOnlyList<WorkingHourInput> inputs, CancellationToken ct)
    {
        EnsureDoctorId(doctorId);
        if (!await repository.DoctorExistsAsync(doctorId, ct)) throw new DoctorForScheduleNotFoundException();
        if (inputs.Count > 28) throw new ArgumentException("حداکثر چهار بازه کاری برای هر روز مجاز است.");

        var hours = new List<DoctorWorkingHour>();
        foreach (var item in inputs)
        {
            if (item.DayOfWeek is < 0 or > 6) throw new ArgumentException("روز هفته نامعتبر است.");
            if (!TimeOnly.TryParseExact(item.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                throw new ArgumentException("ساعت شروع باید با قالب HH:mm ارسال شود.");
            if (!TimeOnly.TryParseExact(item.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                throw new ArgumentException("ساعت پایان باید با قالب HH:mm ارسال شود.");
            hours.Add(new DoctorWorkingHour(doctorId, (DayOfWeek)item.DayOfWeek, start, end, item.SlotDurationMinutes));
        }

        foreach (var day in hours.GroupBy(x => x.DayOfWeek))
        {
            var ordered = day.OrderBy(x => x.StartTime).ToArray();
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                    throw new ArgumentException($"بازه‌های کاری روز {GetDayName(day.Key)} با هم تداخل دارند.");
        }

        await repository.ReplaceWorkingHoursAsync(doctorId, hours, ct);
        return await GetWorkingHoursAsync(doctorId, ct);
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, string jalaliDate, CancellationToken ct)
    {
        EnsureDoctorId(doctorId);
        if (!await repository.DoctorExistsAsync(doctorId, ct)) throw new DoctorForScheduleNotFoundException();
        var date = ParseJalaliDate(jalaliDate);
        var hours = await repository.GetWorkingHoursAsync(doctorId, ct);
        var matchingHours = hours.Where(x => x.IsActive && x.DayOfWeek == date.DayOfWeek).OrderBy(x => x.StartTime).ToArray();
        if (matchingHours.Length == 0) return [];

        var zone = GetTehranTimeZone();
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var dayStartUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone), TimeSpan.Zero);
        var dayEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, zone), TimeSpan.Zero);
        var appointments = await repository.GetAppointmentsForDayAsync(doctorId, dayStartUtc, dayEndUtc, ct);
        var result = new List<AvailableSlotDto>();

        foreach (var working in matchingHours)
        {
            var cursor = working.StartTime;
            while (cursor.AddMinutes(working.SlotDurationMinutes) <= working.EndTime)
            {
                var slotEnd = cursor.AddMinutes(working.SlotDurationMinutes);
                var localSlotStart = date.ToDateTime(cursor, DateTimeKind.Unspecified);
                var localSlotEnd = date.ToDateTime(slotEnd, DateTimeKind.Unspecified);
                var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localSlotStart, zone), TimeSpan.Zero);
                var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localSlotEnd, zone), TimeSpan.Zero);

                var occupied = appointments.Any(x => x.Status != AppointmentStatus.Cancelled &&
                    x.StartsAtUtc < endUtc && x.EndsAtUtc > startUtc);
                if (!occupied && startUtc > DateTimeOffset.UtcNow)
                    result.Add(new AvailableSlotDto(FormatJalaliDate(date), GetDayName(date.DayOfWeek),
                        cursor.ToString("HH:mm"), slotEnd.ToString("HH:mm"), startUtc, endUtc));
                cursor = slotEnd;
            }
        }

        return result;
    }

    public static DateOnly ParseJalaliDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("تاریخ شمسی الزامی است.");
        var parts = value.Trim().Replace('-', '/').Split('/');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            !int.TryParse(parts[2], out var day))
            throw new ArgumentException("تاریخ باید به‌صورت ۱۴۰۵/۰۸/۲۴ ارسال شود.");

        try
        {
            var gregorian = PersianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return DateOnly.FromDateTime(gregorian);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentException("تاریخ شمسی معتبر نیست.");
        }
    }

    public static string FormatJalaliDate(DateOnly date)
    {
        var value = date.ToDateTime(TimeOnly.MinValue);
        return $"{PersianCalendar.GetYear(value):0000}/{PersianCalendar.GetMonth(value):00}/{PersianCalendar.GetDayOfMonth(value):00}";
    }

    private static WorkingHourDto ToDto(DoctorWorkingHour x) =>
        new(x.Id, x.DoctorId, (int)x.DayOfWeek, GetDayName(x.DayOfWeek),
            x.StartTime.ToString("HH:mm"), x.EndTime.ToString("HH:mm"), x.SlotDurationMinutes, x.IsActive);

    private static string GetDayName(DayOfWeek day) => PersianDayNames[(int)day];
    private static int PersianWeekOrder(DayOfWeek day) => day == DayOfWeek.Saturday ? 0 : (int)day + 1;
    private static void EnsureDoctorId(Guid id) { if (id == Guid.Empty) throw new ArgumentException("شناسه پزشک معتبر نیست."); }

    private static TimeZoneInfo GetTehranTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}

public sealed class DoctorForScheduleNotFoundException() : Exception("پزشک موردنظر یافت نشد.");