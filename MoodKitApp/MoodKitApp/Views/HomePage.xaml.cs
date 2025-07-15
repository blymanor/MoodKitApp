// MoodKitApp/Views/HomePage.xaml.cs
// นี่คือไฟล์โค้ด C# ที่ทำงานคู่กับหน้าจอ HomePage.xaml
// HomePage เป็นหน้าหลักของแอปหลังจากผู้ใช้ Login หรือตั้งค่าไดอารี่เสร็จ
// หน้านี้จะแสดงรายการบันทึกอารมณ์ และมีปุ่มให้เพิ่มรายการใหม่ หรือดูรายการแบบรายวัน

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;     // Library หลักของ MAUI
using MoodKitApp.Models;         // เรียกใช้ Model 'MoodRecord' (โครงสร้างข้อมูลบันทึกอารมณ์)
using MoodKitApp.Data;           // เรียกใช้ 'User' (ตัวจัดการฐานข้อมูล)
using Microsoft.EntityFrameworkCore; // Library สำหรับทำงานกับฐานข้อมูล (เช่น ToListAsync)
using Microsoft.Extensions.DependencyInjection; // สำหรับการเรียกใช้ Service ที่ลงทะเบียนไว้ (เช่น DbContext)
using System;                       // Library พื้นฐานของ C#
using System.Collections.ObjectModel; // สำหรับ ObservableCollection (List ที่ UI อัปเดตตามได้)
using System.Linq;                  // สำหรับการจัดการ List ข้อมูล (เช่น Where, OrderByDescending)
using System.Threading.Tasks;       // สำหรับการทำงานแบบ Asynchronous (ไม่ให้ UI ค้าง)

namespace MoodKitApp.Views // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Views
{
    public partial class HomePage : ContentPage // ประกาศคลาส HomePage เป็นหน้าจอแบบ ContentPage
    {
        // --- Properties สำหรับผูกข้อมูลกับ UI ---
        // MoodEntries: กล่องเก็บรายการบันทึกอารมณ์ที่จะแสดงใน CollectionView
        public ObservableCollection<MoodRecord> MoodEntries { get; set; }
        // _dbContext: ตัวเชื่อมต่อฐานข้อมูล
        private readonly User _dbContext;
        // _currentUserName: ชื่อผู้ใช้ที่กำลัง Login
        private readonly string _currentUserName;

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อหน้านี้ถูกสร้างขึ้น) ---
        public HomePage()
        {
            InitializeComponent(); // โหลด UI จาก HomePage.xaml
            MoodEntries = new ObservableCollection<MoodRecord>(); // สร้างกล่องเปล่าสำหรับ MoodEntries
            BindingContext = this; // บอก UI ว่าให้ดึงข้อมูลจาก Property ในคลาสนี้

            // พยายามดึง DbContext (ตัวเชื่อมฐานข้อมูล) ที่ลงทะเบียนไว้ใน MauiProgram.cs
            _dbContext = Application.Current?.Handler?.MauiContext?.Services.GetService<User>()
                         ?? throw new InvalidOperationException("DbContext (User) not found."); // ถ้าหาไม่เจอ แสดงว่ามีปัญหาการตั้งค่า
            // ดึงชื่อผู้ใช้ที่ Login ล่าสุดจาก Preferences (ที่เก็บข้อมูลเล็กๆ ของแอป)
            _currentUserName = Preferences.Get("LoggedInUserName", "UnknownUser");

            // --- ตั้งค่า Event Handlers สำหรับ ReactionPicker (ตัวเลือกอิโมจิ) ---
            // เมื่อ ReactionPicker เลือกอิโมจิเสร็จ ให้ไปเรียกเมธอด ReactionPicker_ReactionSelected
            reactionPicker.ReactionSelected += ReactionPicker_ReactionSelected;
            // เมื่อ ReactionPicker ขอปิดตัวเอง ให้ไปเรียกเมธอด ReactionPicker_CloseRequested
            reactionPicker.CloseRequested += ReactionPicker_CloseRequested;

            // เราจะโหลดข้อมูลใน OnAppearing แทน เพื่อให้ข้อมูลอัปเดตทุกครั้งที่หน้าแสดง
        }

        // --- เมธอดที่ทำงานเมื่อหน้าจอถูกแสดงผล (Life Cycle Method) ---
        // OnAppearing จะถูกเรียกทุกครั้งที่หน้านี้ปรากฏขึ้นมา
        protected override async void OnAppearing()
        {
            base.OnAppearing(); // เรียกการทำงานพื้นฐาน
            System.Diagnostics.Debug.WriteLine("HomePage OnAppearing: Loading mood entries.");
            await LoadMoodEntriesAsync(); // สั่งให้โหลดข้อมูลรายการบันทึกอารมณ์
        }

        private void LogoutButton_Clicked(object sender, EventArgs e)
        {
            // 1. ล้างสถานะการ Login
            Preferences.Remove("LoggedInUserName");
            System.Diagnostics.Debug.WriteLine("User logged out: LoggedInUserName preference removed.");

            // 2. นำทางกลับไปหน้า Sign-in (MainPage) 
            // ส่ง _dbContext (Instance ของ User DbContext) ไปให้ Constructor ของ MainPage
            Application.Current.MainPage = new NavigationPage(new MainPage(_dbContext));

            // ถ้าไม่ได้ห่อ: Application.Current.MainPage = new MainPage();

            System.Diagnostics.Debug.WriteLine("Navigated to MainPage (Sign-in page).");
        }


        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Add Entry" (ปุ่มบวก) ---
        private async void AddEntryButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AddEntryButton_Clicked called");
            reactionPicker.IsVisible = true;      // แสดง ReactionPicker (ตัวเลือกอิโมจิ)
            AddEntryButton.IsVisible = false;     // ซ่อนปุ่ม "Add Entry" (ปุ่มบวก)
            ClosePopupButton.IsVisible = true;    // แสดงปุ่ม "Close Popup" (ปุ่มกากบาท)
            HowWasYourDayLabel.IsVisible = true;  // แสดง Label "How was your day?"
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Close Popup" (ปุ่มกากบาทที่มาพร้อม ReactionPicker) ---
        private void ClosePopupButton_Clicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ClosePopupButton_Clicked called");
            CloseReactionPicker(); // เรียกเมธอดสำหรับปิด ReactionPicker
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้เลือกอิโมจิจาก ReactionPicker ---
        private async void ReactionPicker_ReactionSelected(object? sender, string reaction) // reaction คือชื่ออิโมจิที่เลือก เช่น "happy"
        {
            System.Diagnostics.Debug.WriteLine($"Reaction Selected: {reaction}");
            CloseReactionPicker(); // ปิด ReactionPicker ก่อน
            // นำทางไปยังหน้า MoodTrackerPage เพื่อให้ผู้ใช้กรอกรายละเอียดเพิ่มเติม โดยส่งชื่อ reaction ที่เลือกไปด้วย
            await Navigation.PushAsync(new MoodTrackerPage(reaction));
        }

        // --- เมธอดที่ทำงานเมื่อ ReactionPicker ส่งสัญญาณว่าต้องการปิดตัวเอง ---
        private void ReactionPicker_CloseRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ReactionPicker_CloseRequested called");
            CloseReactionPicker(); // เรียกเมธอดสำหรับปิด ReactionPicker
        }

        // --- เมธอดช่วยสำหรับปิด ReactionPicker และคืนค่าสถานะปุ่มต่างๆ ---
        private void CloseReactionPicker()
        {
            reactionPicker.IsVisible = false;     // ซ่อน ReactionPicker
            AddEntryButton.IsVisible = true;      // แสดงปุ่ม "Add Entry" (ปุ่มบวก) กลับมา
            ClosePopupButton.IsVisible = false;   // ซ่อนปุ่ม "Close Popup" (ปุ่มกากบาท)
            HowWasYourDayLabel.IsVisible = false; // ซ่อน Label "How was your day?"
        }

        // --- เมธอดสำหรับโหลดข้อมูลรายการบันทึกอารมณ์จากฐานข้อมูล ---
        private async Task LoadMoodEntriesAsync()
        {
            // ตรวจสอบว่ามีตัวเชื่อมฐานข้อมูลหรือไม่
            if (_dbContext == null)
            {
                System.Diagnostics.Debug.WriteLine("HomePage: DbContext is null.");
                return; // ถ้าไม่มี ก็ไม่ต้องทำอะไรต่อ
            }

            try
            {
                // 1. ดึงข้อมูล MoodRecord จากฐานข้อมูล
                //    - กรอง (Where) เอาเฉพาะรายการที่เป็นของ _currentUserName
                //    - ToListAsync() คือการดึงข้อมูลทั้งหมดที่ตรงเงื่อนไขออกมาจากฐานข้อมูล
                var entriesFromDbUnsorted = await _dbContext.MoodRecords
                                                    .Where(mr => mr.UserName == _currentUserName)
                                                    .ToListAsync();

                // 2. เรียงลำดับข้อมูลที่ได้มา (ในหน่วยความจำของแอป)
                //    - OrderByDescending(mr => mr.CreatedAt) คือเรียงจากใหม่สุดไปเก่าสุด โดยดูจากเวลาที่สร้าง
                var sortedEntries = entriesFromDbUnsorted
                                        .OrderByDescending(mr => mr.CreatedAt)
                                        .ToList();

                // อัปเดต UI บน Main Thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MoodEntries.Clear(); // ล้างข้อมูลเก่าใน "กล่อง" MoodEntries ออกให้หมด
                    foreach (var record in sortedEntries) // เอาข้อมูลใหม่ที่โหลดและเรียงแล้วใส่เข้าไปทีละรายการ
                    {
                        MoodEntries.Add(record);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {MoodEntries.Count} entries for user {_currentUserName}.");

                    // ตรวจสอบว่ามีข้อมูลหรือไม่ เพื่อแสดง/ซ่อน Label "NoEntriesLabel" และ CollectionView (ตารางแสดงรายการ)
                    NoEntriesLabel.IsVisible = !MoodEntries.Any();
                    MoodEntriesCollectionView.IsVisible = MoodEntries.Any();
                });
            }
            catch (Exception ex) // ถ้ามีปัญหาตอนโหลดข้อมูล
            {
                System.Diagnostics.Debug.WriteLine($"Error loading mood entries: {ex.Message}");
                if (ex.InnerException != null) // ดู Error ย่อย (ถ้ามี)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                await DisplayAlert("Error", $"Could not load mood entries. {ex.GetType().Name}", "OK");
            }
        }

        // --- เมธอดสำหรับให้หน้าอื่นเรียกเพื่อ Refresh ข้อมูลในหน้านี้ ---
        // (เช่น MoodTrackerPage อาจจะเรียกเมธอดนี้หลังจากบันทึกข้อมูลใหม่สำเร็จ)
        public async void RefreshMoodEntries()
        {
            System.Diagnostics.Debug.WriteLine("HomePage RefreshMoodEntries called.");
            await LoadMoodEntriesAsync(); // สั่งให้โหลดข้อมูลใหม่
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้แตะ (Tap) ที่รายการบันทึกอารมณ์ใน CollectionView ---
        private async void MoodEntry_Tapped(object sender, TappedEventArgs e)
        {
            // e.Parameter จะมี MoodRecord ของรายการที่ถูกแตะ (เพราะใน XAML เราตั้ง CommandParameter="{Binding .}")
            if (e.Parameter is MoodRecord selectedEntry)
            {
                System.Diagnostics.Debug.WriteLine($"Mood Entry Tapped: {selectedEntry.FeelingLabel} on {selectedEntry.CreatedAt}");

                 // ตัวเลือกที่ 1: แสดงรายละเอียดใน Alert แบบง่ายๆ
                // await DisplayAlert("Entry Details",
                //                    $"Feeling: {selectedEntry.FeelingLabel}\n" +
                //                    $"Description: {selectedEntry.Description}\n" +
                //                    $"Date: {selectedEntry.CreatedAt:g}\n" + // "g" คือรูปแบบวันที่และเวลาแบบสั้น
                //                    $"Rating: {selectedEntry.Rating} stars", "OK");

                // ตัวเลือกที่ 2 (ถ้าต้องการ): นำทางไปยัง DailyMoodTrackerPage และให้ Focus ที่รายการนี้
                // โดยส่ง DbContext, UserName, และ ID ของรายการที่เลือกไปด้วย
                
                if (_dbContext != null && !string.IsNullOrEmpty(_currentUserName))
                {
                    await Navigation.PushAsync(new DailyMoodTrackerPage(_dbContext, _currentUserName, selectedEntry.MoodRecordId));
                }
                else
                {
                    await DisplayAlert("Error", "Cannot navigate. User context is missing.", "OK");
                }
                
            }
        }

        // // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "View Daily Tracker" ---
        // private async void ViewDailyTrackerButton_Clicked(object sender, EventArgs e)
        // {
        //     System.Diagnostics.Debug.WriteLine("ViewDailyTrackerButton_Clicked called");
        //     // ตรวจสอบว่า DbContext และ UserName พร้อมใช้งาน
        //     if (_dbContext != null && !string.IsNullOrEmpty(_currentUserName))
        //     {
        //         // นำทางไปยังหน้า DailyMoodTrackerPage โดยส่ง DbContext และ UserName ปัจจุบันไปด้วย
        //         await Navigation.PushAsync(new DailyMoodTrackerPage(_dbContext, _currentUserName));
        //     }
        //     else
        //     {
        //         await DisplayAlert("Error", "Cannot navigate. User context is not fully loaded.", "OK");
        //     }
        // }
    }
}