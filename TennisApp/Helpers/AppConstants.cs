namespace TennisApp.Helpers;

/// <summary>
/// ค่าคงที่ของแอพ สำหรับใช้ร่วมกันทั้งโปรเจกต์
/// </summary>
public static class AppConstants
{
    /// <summary>เวลาเปิดสนาม (ชั่วโมง)</summary>
    public const int OpenHour = 6;

    /// <summary>เวลาปิดสนาม (ชั่วโมง)</summary>
    public const int CloseHour = 21;

    /// <summary>ช่วงเวลาเลือก (นาที)</summary>
    public const int TimeSlotMinutes = 30;

    /// <summary>ราคาต่อชั่วโมง (บาท) สำหรับเช่าสนาม — ช่วงกลางวัน 07:00-18:00</summary>
    public const int DayPricePerHour = 400;

    /// <summary>ราคาต่อชั่วโมง (บาท) สำหรับเช่าสนาม — ช่วงกลางคืน 18:00-21:00</summary>
    public const int NightPricePerHour = 500;

    /// <summary>เวลาเริ่มต้นช่วงกลางคืน (ชั่วโมง)</summary>
    public const int NightStartHour = 18;

    /// <summary>
    /// คำนวณราคาเช่าสนามตามช่วงเวลา
    /// 07:00-18:00 = 400 บาท/ชม., 18:00-21:00 = 500 บาท/ชม.
    /// ถ้าจองคร่อมช่วงเวลา จะคิดราคาแยกตามสัดส่วน
    /// </summary>
    public static int CalculateCourtPrice(TimeSpan startTime, double durationHours)
    {
        var nightBoundary = TimeSpan.FromHours(NightStartHour);
        var endTime = startTime.Add(TimeSpan.FromHours(durationHours));

        // ทั้งหมดอยู่ในช่วงกลางวัน
        if (endTime <= nightBoundary)
            return (int)(durationHours * DayPricePerHour);

        // ทั้งหมดอยู่ในช่วงกลางคืน
        if (startTime >= nightBoundary)
            return (int)(durationHours * NightPricePerHour);

        // คร่อมช่วง: แยกคำนวณ
        var dayHours = (nightBoundary - startTime).TotalHours;
        var nightHours = durationHours - dayHours;
        return (int)(dayHours * DayPricePerHour + nightHours * NightPricePerHour);
    }

    /// <summary>ความยาวเบอร์โทรศัพท์ (หลัก)</summary>
    public const int PhoneNumberLength = 10;
}
