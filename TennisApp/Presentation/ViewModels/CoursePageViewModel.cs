using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.ViewModels;

public partial class CoursePageViewModel : ObservableObject
{
    private readonly DatabaseService _database;
    
    [ObservableProperty]
    private ObservableCollection<CourseItem> _courses = new();
    
    [ObservableProperty]
    private ObservableCollection<CourseItem> _filteredCourses = new();
    
    [ObservableProperty]
    private string _searchKeyword = string.Empty;
    
    [ObservableProperty]
    private string _selectedFilterField = "ทั้งหมด";
    
    [ObservableProperty]
    private bool _isLoading;

    public CoursePageViewModel()
    {
        _database = ((App)Microsoft.UI.Xaml.Application.Current).DatabaseService;
    }

    [RelayCommand]
    public async Task LoadCoursesAsync()
    {
        try
        {
            IsLoading = true;
            System.Diagnostics.Debug.WriteLine("📚 Loading courses...");

            var courses = await _database.Courses.GetAllCoursesAsync().ConfigureAwait(false);
            
            Courses.Clear();
            FilteredCourses.Clear();

            foreach (var course in courses)
            {
                Courses.Add(course);
                FilteredCourses.Add(course);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Loaded {Courses.Count} courses");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courses: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchCoursesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                FilteredCourses.Clear();
                foreach (var course in Courses)
                {
                    FilteredCourses.Add(course);
                }
                return;
            }

            var results = await _database.Courses.SearchCoursesAsync(SearchKeyword, SelectedFilterField).ConfigureAwait(false);
            
            FilteredCourses.Clear();
            foreach (var course in results)
            {
                FilteredCourses.Add(course);
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Found {FilteredCourses.Count} courses matching '{SearchKeyword}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error searching courses: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task<bool> DeleteCourseAsync(string classId)
    {
        try
        {
            var success = await _database.Courses.DeleteCourseAsync(classId).ConfigureAwait(false);
            
            if (success)
            {
                var courseToRemove = Courses.FirstOrDefault(c => c.ClassId == classId);
                if (courseToRemove != null)
                {
                    Courses.Remove(courseToRemove);
                }

                var filteredCourseToRemove = FilteredCourses.FirstOrDefault(c => c.ClassId == classId);
                if (filteredCourseToRemove != null)
                {
                    FilteredCourses.Remove(filteredCourseToRemove);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Deleted course: {classId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting course: {ex.Message}");
            return false;
        }
    }
}
