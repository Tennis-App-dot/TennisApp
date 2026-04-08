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

public partial class TrainerPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService = null!;
    private readonly ObservableCollection<TrainerItem> _allTrainers = new();
    
    [ObservableProperty]
    private string _searchKeyword = string.Empty;
    
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

    public TrainerPageViewModel() : this(((App)Microsoft.UI.Xaml.Application.Current).DatabaseService) { }

    public TrainerPageViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        System.Diagnostics.Debug.WriteLine("✅ TrainerPageViewModel created via DI");
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
            Trainers.Clear();

            var result = await _databaseService.Trainers.SearchAsync(
                SearchKeyword,
                "All",
                _currentPage,
                PageSize
            );

            foreach (var trainer in result.Items)
            {
                // แปลง ImageData → ImageSource สำหรับ UI binding
                if (trainer.ImageData != null && trainer.ImageData.Length > 0)
                {
                    try
                    {
                        trainer.ImageSource = await ImageHelper.CreateBitmapFromBytesAsync(trainer.ImageData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load image for {trainer.TrainerId}: {ex.Message}");
                    }
                }
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
                "All",
                _currentPage,
                PageSize
            );

            foreach (var trainer in result.Items)
            {
                // แปลง ImageData → ImageSource สำหรับ UI binding
                if (trainer.ImageData != null && trainer.ImageData.Length > 0)
                {
                    try
                    {
                        trainer.ImageSource = await ImageHelper.CreateBitmapFromBytesAsync(trainer.ImageData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load image for {trainer.TrainerId}: {ex.Message}");
                    }
                }
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
            
            var success = await _databaseService.Trainers.DeleteTrainerAsync(trainerId);
            
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
