using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Helpers;
using System;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class TrainerDetailCard : UserControl
{
    public event EventHandler<string>? EditRequested;
    public event EventHandler<string>? DeleteRequested;
    public event EventHandler? CloseRequested;

    private string _trainerId = string.Empty;

    public TrainerDetailCard()
    {
        this.InitializeComponent();
    }

    public async void SetTrainer(TrainerItem trainer)
    {
        _trainerId = trainer.TrainerId;

        // Set trainer ID
        TrainerIdText.Text = $"รหัสประจำตัวผู้ฝึกสอน {trainer.TrainerId}";
        
        // Set full name
        NameText.Text = $"ชื่อ-นามสกุล : {trainer.FullName}";
        
        // Set nickname
        NicknameText.Text = string.IsNullOrEmpty(trainer.Nickname) 
            ? "ชื่อเล่น : -" 
            : $"ชื่อเล่น : {trainer.Nickname}";
        
        // Set birth date and age (calculated from birth date)
        if (trainer.BirthDate.HasValue)
        {
            BirthDateText.Text = $"วันเกิด : {trainer.BirthDate.Value:dd/MM/yyyy}";
            
            // Age is automatically calculated from BirthDate in TrainerItem model
            var age = trainer.Age;
            AgeText.Text = age.HasValue ? $"อายุ : {age.Value} ปี" : "อายุ : -";
        }
        else
        {
            BirthDateText.Text = "วันเกิด : -";
            AgeText.Text = "อายุ : -";
        }
        
        // Set phone
        PhoneText.Text = string.IsNullOrEmpty(trainer.Phone) 
            ? "เบอร์ติดต่อ : -" 
            : $"เบอร์ติดต่อ : {trainer.Phone}";

        // Load profile image from database (ImageData is byte[] from database)
        if (trainer.ImageData != null && trainer.ImageData.Length > 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📷 Loading image for trainer {trainer.TrainerId}: {trainer.ImageData.Length} bytes");
                
                var bitmap = await ImageHelper.CreateBitmapFromBytesAsync(trainer.ImageData);
                if (bitmap != null)
                {
                    ProfileImage.Source = bitmap;
                    System.Diagnostics.Debug.WriteLine($"✅ Image loaded successfully for trainer: {trainer.TrainerId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Failed to create bitmap for trainer: {trainer.TrainerId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading image for trainer {trainer.TrainerId}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            // Clear image if no data
            ProfileImage.Source = null;
            System.Diagnostics.Debug.WriteLine($"ℹ️ No image data for trainer: {trainer.TrainerId}");
        }
    }

    private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        EditRequested?.Invoke(this, _trainerId);
    }

    private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(this, _trainerId);
    }
}
