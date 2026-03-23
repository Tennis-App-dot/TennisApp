using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Helpers;

namespace TennisApp.Presentation.ViewModels;

public partial class TraineePageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService = null!;
    private readonly ObservableCollection<TraineeItem> _allTrainees = new();
    
    [ObservableProperty]
    private string _searchKeyword = string.Empty;
    
    [ObservableProperty]
    private string _selectedSearchField = "All";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _hasNoResults;
    
    [ObservableProperty]
    private int _totalCount;
    
    [ObservableProperty]
    private bool _hasMoreData;
    
    private int _currentPage = 1;
    private const int PageSize = 50;
    
    public ObservableCollection<TraineeItem> Trainees { get; } = new();
    
    public ObservableCollection<string> SearchFields { get; } = new()
    {
        "ทั้งหมด",
        "รหัสประจำตัวผู้เรียน",
        "ชื่อ",
        "เบอร์โทรศัพท์"
    };

    public TraineePageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("TraineePageViewModel constructor started");
        
        try
        {
            _databaseService = ((App)Microsoft.UI.Xaml.Application.Current).DatabaseService;
            System.Diagnostics.Debug.WriteLine("DatabaseService created successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TraineePageViewModel constructor error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        try
        {
            _databaseService.EnsureInitialized();
            IsLoading = true;
            HasNoResults = false;
            _currentPage = 1;
            Trainees.Clear();

            var result = await _databaseService.Trainees.SearchAsync(
                SearchKeyword,
                SelectedSearchField,
                _currentPage,
                PageSize
            ).ConfigureAwait(false);

            foreach (var trainee in result.Items)
            {
                if (trainee.ImageData != null && trainee.ImageData.Length > 0)
                {
                    try
                    {
                        trainee.ImageSource = await ImageHelper.CreateBitmapFromBytesAsync(trainee.ImageData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load image for {trainee.TraineeId}: {ex.Message}");
                    }
                }
                Trainees.Add(trainee);
            }

            TotalCount = result.TotalCount;
            HasMoreData = result.HasNextPage;
            HasNoResults = result.Items.Count == 0;

            System.Diagnostics.Debug.WriteLine($"🔍 Search completed: {result.Items.Count}/{result.TotalCount} items, page {result.Page}/{result.TotalPages}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Search error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    public async Task LoadMoreAsync()
    {
        try
        {
            IsLoading = true;
            _currentPage++;

            var result = await _databaseService.Trainees.SearchAsync(
                SearchKeyword,
                SelectedSearchField,
                _currentPage,
                PageSize
            ).ConfigureAwait(false);

            foreach (var trainee in result.Items)
            {
                if (trainee.ImageData != null && trainee.ImageData.Length > 0)
                {
                    try
                    {
                        trainee.ImageSource = await ImageHelper.CreateBitmapFromBytesAsync(trainee.ImageData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load image for {trainee.TraineeId}: {ex.Message}");
                    }
                }
                Trainees.Add(trainee);
            }

            HasMoreData = result.HasNextPage;

            System.Diagnostics.Debug.WriteLine($"📄 Load more: page {_currentPage}, loaded {result.Items.Count} items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Load more error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLoadMore() => HasMoreData && !IsLoading;

    [RelayCommand]
    public async Task LoadTraineesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔍 LoadTraineesAsync started...");
            
            await SearchAsync();
            
            System.Diagnostics.Debug.WriteLine($"✅ Load complete - UI showing {Trainees.Count} trainees");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainees: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    public async Task<bool> DeleteTraineeAsync(string traineeId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ Deleting trainee {traineeId}...");
            
            var success = await _databaseService.Trainees.DeleteTraineeAsync(traineeId).ConfigureAwait(false);
            
            if (success)
            {
                var traineeToRemove = _allTrainees.FirstOrDefault(t => t.TraineeId == traineeId);
                if (traineeToRemove != null)
                {
                    _allTrainees.Remove(traineeToRemove);
                }

                var traineeInDisplay = Trainees.FirstOrDefault(t => t.TraineeId == traineeId);
                if (traineeInDisplay != null)
                {
                    Trainees.Remove(traineeInDisplay);
                }

                TotalCount--;

                System.Diagnostics.Debug.WriteLine($"✅ Trainee deleted successfully");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to delete trainee");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting trainee: {ex.Message}");
            return false;
        }
    }
}
