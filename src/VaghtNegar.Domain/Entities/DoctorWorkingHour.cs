namespace VaghtNegar.Domain.Entities;

public sealed class DoctorWorkingHour
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DoctorId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private DoctorWorkingHour() { }

    public DoctorWorkingHour(Guid doctorId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, int slotDurationMinutes)
    {
        if (doctorId == Guid.Empty) throw new ArgumentException("Doctor is required.", nameof(doctorId));
        if (startTime >= endTime) throw new ArgumentException("End time must be after start time.");
        if (slotDurationMinutes is < 5 or > 240) throw new ArgumentOutOfRangeException(nameof(slotDurationMinutes));
        DoctorId = doctorId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }
}