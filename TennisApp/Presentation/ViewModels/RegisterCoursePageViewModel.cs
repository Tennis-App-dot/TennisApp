using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.ViewModels;

/// <summary>
/// ViewModel for Register Course Page (สมัครคอร์สเรียน)
/// Manages trainee list and course registration
/// </summary>
public partial class RegisterCoursePageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    
    [ObservableProperty]
    private string _searchKeyword = string.Empty;
    
    [ObservableProperty]
    private string _selectedFilterField = "ทั้งหมด";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _hasNoResults;
    
    [ObservableProperty]
    private int _totalCount;
    
    public ObservableCollection<TraineeItem> Trainees { get; } = new();
    
    public ObservableCollection<string> FilterFields { get; } = new()
    {
        "ทั้งหมด",
        "รหัสประจำตัวผู้เรียน",
        "ชื่อ",
        "นามสกุล"
    };

    public RegisterCoursePageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("RegisterCoursePageViewModel: Constructor started");
        
        try
        {
            _databaseService = new DatabaseService();
            System.Diagnostics.Debug.WriteLine("✅ DatabaseService created successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RegisterCoursePageViewModel constructor error: {ex.Message}");
        }
    }

    /// <summary>
    /// Load all trainees for registration
    /// </summary>
    [RelayCommand]
    public async Task LoadTraineesAsync()
    {
        try
        {
            _databaseService.EnsureInitialized();
            System.Diagnostics.Debug.WriteLine("🔍 LoadTraineesAsync started...");
            
            IsLoading = true;
            HasNoResults = false;
            Trainees.Clear();

            var trainees = await _databaseService.Trainees.GetAllTraineesAsync();
            
            foreach (var trainee in trainees)
            {
                Trainees.Add(trainee);
            }

            TotalCount = trainees.Count;
            HasNoResults = trainees.Count == 0;
            
            System.Diagnostics.Debug.WriteLine($"✅ Loaded {trainees.Count} trainees for registration");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainees: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Search trainees by keyword and filter field
    /// </summary>
    [RelayCommand]
    public async Task SearchTraineesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Searching trainees: '{SearchKeyword}' in '{SelectedFilterField}'");
            
            IsLoading = true;
            HasNoResults = false;
            Trainees.Clear();

            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // If no keyword, load all trainees
                await LoadTraineesAsync();
                return;
            }

            var allTrainees = await _databaseService.Trainees.GetAllTraineesAsync();
            var filteredTrainees = allTrainees.Where(t => MatchesFilter(t, SearchKeyword, SelectedFilterField)).ToList();

            foreach (var trainee in filteredTrainees)
            {
                Trainees.Add(trainee);
            }

            TotalCount = filteredTrainees.Count;
            HasNoResults = filteredTrainees.Count == 0;
            
            System.Diagnostics.Debug.WriteLine($"✅ Search completed: {filteredTrainees.Count} trainees found");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error searching trainees: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Check if trainee matches the search filter
    /// </summary>
    private bool MatchesFilter(TraineeItem trainee, string keyword, string filterField)
    {
        var lowerKeyword = keyword.ToLower();

        return filterField switch
        {
            "รหัสประจำตัวผู้เรียน" => trainee.TraineeId.ToLower().Contains(lowerKeyword),
            "ชื่อ" => trainee.FirstName.ToLower().Contains(lowerKeyword),
            "นามสกุล" => trainee.LastName.ToLower().Contains(lowerKeyword),
            _ => trainee.TraineeId.ToLower().Contains(lowerKeyword) ||
                 trainee.FirstName.ToLower().Contains(lowerKeyword) ||
                 trainee.LastName.ToLower().Contains(lowerKeyword) ||
                 (trainee.Nickname?.ToLower().Contains(lowerKeyword) ?? false)
        };
    }

    /// <summary>
    /// Get all available courses for registration
    /// </summary>
    public async Task<List<CourseItem>> GetAvailableCoursesAsync()
    {
        try
        {
            var courses = await _databaseService.Courses.GetAllCoursesAsync();
            System.Diagnostics.Debug.WriteLine($"📚 Loaded {courses.Count} courses");
            return courses;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courses: {ex.Message}");
            return new List<CourseItem>();
        }
    }

    /// <summary>
    /// Register a trainee to a course
    /// </summary>
    public async Task<bool> RegisterToCourseAsync(string traineeId, string classId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"📝 Registering trainee {traineeId} to course {classId}...");

            // Check if already registered
            var exists = await _databaseService.Registrations.RegistrationExistsAsync(traineeId, classId);
            if (exists)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Trainee already registered to this course");
                return false;
            }

            // Create registration record
            var registration = new ClassRegisRecordItem
            {
                TraineeId = traineeId,
                ClassId = classId,
                RegisDate = DateTime.Now
            };

            var success = await _databaseService.Registrations.AddRegistrationAsync(registration);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Registration successful");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Registration failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error registering to course: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get trainee's registered courses
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> GetTraineeRegistrationsAsync(string traineeId)
    {
        try
        {
            var registrations = await _databaseService.Registrations.GetRegistrationsByTraineeIdAsync(traineeId);
            System.Diagnostics.Debug.WriteLine($"📋 Trainee {traineeId} has {registrations.Count} registrations");
            return registrations;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting trainee registrations: {ex.Message}");
            return new List<ClassRegisRecordItem>();
        }
    }

    /// <summary>
    /// Get course details by ID
    /// </summary>
    public async Task<CourseItem?> GetCourseByIdAsync(string classId)
    {
        try
        {
            return await _databaseService.Courses.GetCourseByIdAsync(classId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting course: {ex.Message}");
            return null;
        }
    }
}
