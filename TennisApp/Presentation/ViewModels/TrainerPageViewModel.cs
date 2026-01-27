using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.ViewModels;

public partial class TrainerPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly ObservableCollection<TrainerItem> _allTrainers = new();
    
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
    
    public ObservableCollection<TrainerItem> Trainers { get; } = new();
    
    public ObservableCollection<string> SearchFields { get; } = new()
    {
        "ทั้งหมด",
        "รหัสประจำตัวผู้ฝึกสอน",
        "ชื่อ",
        "เบอร์โทรศัพท์"
    };

    public TrainerPageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("TrainerPageViewModel constructor started");
        
        try
        {
            _databaseService = new DatabaseService();
            System.Diagnostics.Debug.WriteLine("DatabaseService created successfully");
            System.Diagnostics.Debug.WriteLine("TrainerPageViewModel ready - database connected");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TrainerPageViewModel constructor error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        try
        {
            IsLoading = true;
            HasNoResults = false;
            _currentPage = 1;
            Trainers.Clear();

            var result = await _databaseService.Trainers.SearchAsync(
                SearchKeyword,
                SelectedSearchField,
                _currentPage,
                PageSize
            ).ConfigureAwait(false);

            foreach (var trainer in result.Items)
            {
                Trainers.Add(trainer);
            }

            TotalCount = result.TotalCount;
            HasMoreData = _currentPage < result.TotalPages;
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

            var result = await _databaseService.Trainers.SearchAsync(
                SearchKeyword,
                SelectedSearchField,
                _currentPage,
                PageSize
            ).ConfigureAwait(false);

            foreach (var trainer in result.Items)
            {
                Trainers.Add(trainer);
            }

            HasMoreData = _currentPage < result.TotalPages;

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
    public async Task LoadTrainersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔍 LoadTrainersAsync started...");
            
            await SearchAsync();
            
            System.Diagnostics.Debug.WriteLine($"✅ Load complete - UI showing {Trainers.Count} trainers");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    public async Task<bool> DeleteTrainerAsync(string trainerId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ Deleting trainer {trainerId}...");
            
            var success = await _databaseService.Trainers.DeleteTrainerAsync(trainerId).ConfigureAwait(false);
            
            if (success)
            {
                var trainerToRemove = _allTrainers.FirstOrDefault(t => t.TrainerId == trainerId);
                if (trainerToRemove != null)
                {
                    _allTrainers.Remove(trainerToRemove);
                }

                var trainerInDisplay = Trainers.FirstOrDefault(t => t.TrainerId == trainerId);
                if (trainerInDisplay != null)
                {
                    Trainers.Remove(trainerInDisplay);
                }

                TotalCount--;

                System.Diagnostics.Debug.WriteLine($"✅ Trainer deleted successfully");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to delete trainer");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting trainer: {ex.Message}");
            return false;
        }
    }
}
