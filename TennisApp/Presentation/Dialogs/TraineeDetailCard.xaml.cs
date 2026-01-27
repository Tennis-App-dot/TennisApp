using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TennisApp.Models;
using TennisApp.Helpers;
using System;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class TraineeDetailCard : UserControl
{
    public event EventHandler<string>? EditRequested;
    public event EventHandler<string>? DeleteRequested;
    public event EventHandler<string>? HistoryRequested;
    public event EventHandler? CloseRequested;

    private string _traineeId = string.Empty;

    public TraineeDetailCard()
    {
        this.InitializeComponent();
    }

    public async void SetTrainee(TraineeItem trainee)
    {
        _traineeId = trainee.TraineeId;

        // Set trainee ID
        TraineeIdText.Text = $"รหัสประจำตัวผู้เรียน {trainee.TraineeId}";
        
        // Set full name
        NameText.Text = $"ชื่อ-นามสกุล : {trainee.FullName}";
        
        // Set nickname
        NicknameText.Text = string.IsNullOrEmpty(trainee.Nickname) 
            ? "ชื่อเล่น : -" 
            : $"ชื่อเล่น : {trainee.Nickname}";
        
        // Set birth date and age (calculated from birth date)
        if (trainee.BirthDate.HasValue)
        {
            BirthDateText.Text = $"วันเกิด : {trainee.BirthDate.Value:dd/MM/yyyy}";
            
            // Age is automatically calculated from BirthDate in TraineeItem model
            var age = trainee.Age;
            AgeText.Text = age.HasValue ? $"อายุ : {age.Value} ปี" : "อายุ : -";
        }
        else
        {
            BirthDateText.Text = "วันเกิด : -";
            AgeText.Text = "อายุ : -";
        }
        
        // Set phone
        PhoneText.Text = string.IsNullOrEmpty(trainee.Phone) 
            ? "เบอร์ติดต่อ : -" 
            : $"เบอร์ติดต่อ : {trainee.Phone}";

        // Load profile image from database (ImageData is byte[] from database)
        if (trainee.ImageData != null && trainee.ImageData.Length > 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📷 Loading image for trainee {trainee.TraineeId}: {trainee.ImageData.Length} bytes");
                
                var bitmap = await ImageHelper.CreateBitmapFromBytesAsync(trainee.ImageData);
                if (bitmap != null)
                {
                    ProfileImage.Source = bitmap;
                    System.Diagnostics.Debug.WriteLine($"✅ Image loaded successfully for trainee: {trainee.TraineeId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create bitmap for trainee: {trainee.TraineeId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading image for trainee {trainee.TraineeId}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            // Clear image if no data
            ProfileImage.Source = null;
            System.Diagnostics.Debug.WriteLine($"ℹ️ No image data for trainee: {trainee.TraineeId}");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"📋 View history for trainee: {_traineeId}");
        HistoryRequested?.Invoke(this, _traineeId);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        EditRequested?.Invoke(this, _traineeId);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(this, _traineeId);
    }
}
