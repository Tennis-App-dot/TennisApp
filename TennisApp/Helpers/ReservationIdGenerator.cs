using System;
using System.Linq;
using System.Threading.Tasks;
using TennisApp.Services;

namespace TennisApp.Helpers;

/// <summary>
/// Helper class to generate Reservation IDs in format: YYYYMMDDXX
/// - YYYY: Year (4 digits)
/// - MM: Month (2 digits)
/// - DD: Day (2 digits)
/// - XX: Sequence number (01-99) of reservations made on that day
/// 
/// Example: 2025041609 = 9th reservation made on April 16, 2025
/// </summary>
public static class ReservationIdGenerator
{
    /// <summary>
    /// Generate Paid Court Reservation ID based on request date (Simple version without DB check)
    /// Use this when creating new reservations through UI dialogs
    /// </summary>
    /// <param name="requestDate">Date when the reservation request is made</param>
    /// <returns>New reservation ID (10 digits) with sequence based on timestamp</returns>
    public static string GeneratePaidReservationId(DateTime requestDate)
    {
        // Format: YYYYMMDD
        string datePrefix = requestDate.ToString("yyyyMMdd");
        
        // Use timestamp-based sequence to minimize collision risk
        // Milliseconds mod 99 + 1 gives 01-99
        int sequence = (int)(requestDate.Ticks % 99) + 1;
        
        string sequenceStr = sequence.ToString("D2");
        
        return datePrefix + sequenceStr;
    }

    /// <summary>
    /// Generate Course Court Reservation ID based on request date (Simple version without DB check)
    /// Use this when creating new reservations through UI dialogs
    /// </summary>
    /// <param name="requestDate">Date when the reservation request is made</param>
    /// <returns>New reservation ID (10 digits) with sequence based on timestamp</returns>
    public static string GenerateCourseReservationId(DateTime requestDate)
    {
        // Format: YYYYMMDD
        string datePrefix = requestDate.ToString("yyyyMMdd");
        
        // Use timestamp-based sequence to minimize collision risk
        int sequence = (int)(requestDate.Ticks % 99) + 1;
        
        string sequenceStr = sequence.ToString("D2");
        
        return datePrefix + sequenceStr;
    }

    /// <summary>
    /// Generate Paid Court Reservation ID based on request date
    /// </summary>
    /// <param name="dbService">Database service to check existing IDs</param>
    /// <param name="requestDate">Date when the reservation request is made</param>
    /// <returns>New reservation ID (10 digits)</returns>
    public static async Task<string> GeneratePaidReservationIdAsync(DatabaseService dbService, DateTime requestDate)
    {
        // Format: YYYYMMDD
        string datePrefix = requestDate.ToString("yyyyMMdd");

        // ✅ ตรวจสอบทั้ง Paid และ Course เพื่อป้องกัน ID ชนกัน
        var existingPaid = await dbService.PaidCourtReservations
            .GetReservationsByRequestDateAsync(requestDate);
        var existingCourse = await dbService.CourseCourtReservations
            .GetReservationsByRequestDateAsync(requestDate);

        // รวม sequence จากทั้ง 2 ตารางแล้วหา max
        int maxSequence = 0;
        if (existingPaid.Any())
        {
            maxSequence = Math.Max(maxSequence, existingPaid
                .Select(r => ExtractSequenceNumber(r.ReserveId))
                .Max());
        }
        if (existingCourse.Any())
        {
            maxSequence = Math.Max(maxSequence, existingCourse
                .Select(r => ExtractSequenceNumber(r.ReserveId))
                .Max());
        }

        // Generate new sequence number (max + 1)
        int newSequence = maxSequence + 1;

        // Validate sequence number (must be 01-99)
        if (newSequence > 99)
        {
            throw new InvalidOperationException(
                $"Cannot create more than 99 reservations on {requestDate:yyyy-MM-dd}. " +
                "Maximum daily reservation limit reached.");
        }

        // Format sequence as 2 digits (01, 02, ..., 99)
        string sequenceStr = newSequence.ToString("D2");

        // Combine: YYYYMMDD + XX
        return datePrefix + sequenceStr;
    }

    /// <summary>
    /// Generate Course Court Reservation ID based on request date
    /// </summary>
    /// <param name="dbService">Database service to check existing IDs</param>
    /// <param name="requestDate">Date when the reservation request is made</param>
    /// <returns>New reservation ID (10 digits)</returns>
    public static async Task<string> GenerateCourseReservationIdAsync(DatabaseService dbService, DateTime requestDate)
    {
        // Format: YYYYMMDD
        string datePrefix = requestDate.ToString("yyyyMMdd");

        // ✅ ตรวจสอบทั้ง Paid และ Course เพื่อป้องกัน ID ชนกัน
        var existingPaid = await dbService.PaidCourtReservations
            .GetReservationsByRequestDateAsync(requestDate);
        var existingCourse = await dbService.CourseCourtReservations
            .GetReservationsByRequestDateAsync(requestDate);

        // รวม sequence จากทั้ง 2 ตารางแล้วหา max
        int maxSequence = 0;
        if (existingPaid.Any())
        {
            maxSequence = Math.Max(maxSequence, existingPaid
                .Select(r => ExtractSequenceNumber(r.ReserveId))
                .Max());
        }
        if (existingCourse.Any())
        {
            maxSequence = Math.Max(maxSequence, existingCourse
                .Select(r => ExtractSequenceNumber(r.ReserveId))
                .Max());
        }

        // Generate new sequence number (max + 1)
        int newSequence = maxSequence + 1;

        // Validate sequence number (must be 01-99)
        if (newSequence > 99)
        {
            throw new InvalidOperationException(
                $"Cannot create more than 99 course reservations on {requestDate:yyyy-MM-dd}. " +
                "Maximum daily reservation limit reached.");
        }

        // Format sequence as 2 digits (01, 02, ..., 99)
        string sequenceStr = newSequence.ToString("D2");

        // Combine: YYYYMMDD + XX
        return datePrefix + sequenceStr;
    }

    /// <summary>
    /// Generate Paid Court Use Log ID based on actual use date
    /// </summary>
    /// <param name="dbService">Database service to check existing IDs</param>
    /// <param name="useDate">Date when the court is actually used</param>
    /// <returns>New log ID (10 digits)</returns>
    public static async Task<string> GeneratePaidUseLogIdAsync(DatabaseService dbService, DateTime useDate)
    {
        string datePrefix = useDate.ToString("yyyyMMdd");

        var maxId = await dbService.PaidCourtUseLogs.GetMaxLogIdByPrefixAsync(datePrefix);
        int maxSequence = maxId != null ? ExtractSequenceNumber(maxId) : 0;

        int newSequence = maxSequence + 1;

        if (newSequence > 99)
        {
            throw new InvalidOperationException(
                $"Cannot create more than 99 paid use logs on {useDate:yyyy-MM-dd}.");
        }

        string sequenceStr = newSequence.ToString("D2");
        return datePrefix + sequenceStr;
    }

    /// <summary>
    /// Generate Course Court Use Log ID based on actual use date
    /// </summary>
    /// <param name="dbService">Database service to check existing IDs</param>
    /// <param name="useDate">Date when the court is actually used</param>
    /// <returns>New log ID (10 digits)</returns>
    public static async Task<string> GenerateCourseUseLogIdAsync(DatabaseService dbService, DateTime useDate)
    {
        string datePrefix = useDate.ToString("yyyyMMdd");

        var maxId = await dbService.CourseCourtUseLogs.GetMaxLogIdByPrefixAsync(datePrefix);
        int maxSequence = maxId != null ? ExtractSequenceNumber(maxId) : 0;

        int newSequence = maxSequence + 1;

        if (newSequence > 99)
        {
            throw new InvalidOperationException(
                $"Cannot create more than 99 course use logs on {useDate:yyyy-MM-dd}.");
        }

        string sequenceStr = newSequence.ToString("D2");
        return datePrefix + sequenceStr;
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    /// <summary>
    /// Extract sequence number (last 2 digits) from reservation/log ID
    /// </summary>
    /// <param name="id">ID in format YYYYMMDDXX</param>
    /// <returns>Sequence number (1-99)</returns>
    private static int ExtractSequenceNumber(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 10)
            return 0;

        // Get last 2 digits
        string sequenceStr = id.Substring(8, 2);
        
        if (int.TryParse(sequenceStr, out int sequence))
            return sequence;

        return 0;
    }

    /// <summary>
    /// Parse reservation ID to extract date and sequence
    /// </summary>
    /// <param name="id">Reservation ID (YYYYMMDDXX)</param>
    /// <returns>Tuple of (Date, Sequence) or null if invalid</returns>
    public static (DateTime Date, int Sequence)? ParseReservationId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 10)
            return null;

        try
        {
            // Extract date part (YYYYMMDD)
            string datePart = id.Substring(0, 8);
            int year = int.Parse(datePart.Substring(0, 4));
            int month = int.Parse(datePart.Substring(4, 2));
            int day = int.Parse(datePart.Substring(6, 2));

            DateTime date = new DateTime(year, month, day);

            // Extract sequence part (XX)
            int sequence = ExtractSequenceNumber(id);

            return (date, sequence);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validate reservation ID format
    /// </summary>
    /// <param name="id">ID to validate</param>
    /// <returns>True if valid format</returns>
    public static bool IsValidReservationId(string id)
    {
        return ParseReservationId(id) != null;
    }
}
