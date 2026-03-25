// MoodKitApp/Views/MoodTrackerPage.xaml.cs
// นี่คือไฟล์โค้ด C# ที่ทำงานกับหน้าจอ MoodTrackerPage.xaml
// หน้านี้ใช้สำหรับ:
// 1. สร้างรายการบันทึกอารมณ์ใหม่ (เมื่อผู้ใช้เลือกอิโมจิจาก HomePage)
// 2. แก้ไขรายการบันทึกอารมณ์ที่มีอยู่ (เมื่อถูกเรียกมาจาก DailyMoodTrackerPage)

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;     // Library หลักของ MAUI สำหรับ UI
using Microsoft.Maui.Media;       // Library สำหรับเข้าถึง Media (เช่น เลือกรูปภาพจากเครื่อง)
using System;                       // Library พื้นฐาน C# (DateTime, EventArgs, Guid, etc.)
using System.IO;                    // Library สำหรับทำงานกับไฟล์และ Path (เช่น Path.Combine, File.Exists, File.Delete)
using System.Threading.Tasks;       // สำหรับการทำงานแบบ Asynchronous (ไม่ให้ UI ค้าง)
using MoodKitApp.Models;         // เรียกใช้ Model 'MoodRecord' (โครงสร้างข้อมูลบันทึกอารมณ์)
using MoodKitApp.Data;           // เรียกใช้ 'User' (ตัวจัดการฐานข้อมูล DbContext)
using Microsoft.Extensions.DependencyInjection; // สำหรับ GetService (ดึง Service ที่ลงทะเบียนไว้)
using System.Linq;                  // Library สำหรับจัดการ List ข้อมูล (ไม่ได้ใช้โดยตรงในโค้ดนี้ แต่มีประโยชน์)
using System.Globalization;         // สำหรับการจัดรูปแบบวันที่/เวลา (เช่น แสดงชื่อเดือนเป็นภาษาอังกฤษ)
using Microsoft.Maui.Storage; // **ต้องมี** Library นี้สำหรับ Preferences
// using Microsoft.EntityFrameworkCore; // อาจจะไม่จำเป็นถ้า FindAsync เพียงพอ (ในโค้ดนี้ใช้ FindAsync)

namespace MoodKitApp.Views // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Views
{
    // ประกาศคลาส MoodTrackerPage เป็นหน้าจอ (ContentPage)
    public partial class MoodTrackerPage : ContentPage
    {
        // --- Fields (ตัวแปรที่ใช้ภายในคลาสนี้) ---
        private string _selectedMood = string.Empty; // เก็บชื่อของอารมณ์ที่ถูกเลือก (เช่น "happy", "sad")
        private int _rating = 0;                     // เก็บค่าคะแนนดาว (0-3)
        private List<ImageButton> _starButtons;      // List ของปุ่มดาวทั้ง 3 ปุ่ม (Star1, Star2, Star3 จาก XAML)
        private string? _attachedImageFilePath = null; // เก็บ Path (ที่อยู่) ของไฟล์รูปภาพที่ผู้ใช้แนบมา (ถ้ามี)

        private readonly User _dbContext;             // ตัวเชื่อมต่อและจัดการฐานข้อมูล (ชื่อ User อาจทำให้สับสน ควรพิจารณาเปลี่ยนชื่อ Class นี้ใน MoodKitApp.Data)
        private readonly string _currentUserName;     // ชื่อผู้ใช้ที่กำลัง Login อยู่

        private MoodRecord? _editingMoodRecord = null; // ถ้ากำลังแก้ไขรายการเก่า จะเก็บข้อมูลของรายการนั้นไว้ที่นี่
        private bool _isEditingMode = false;          // เป็น Flag (ตัวบอกสถานะ) ว่าตอนนี้กำลัง "แก้ไข" (true) หรือ "สร้างใหม่" (false)

        // --- Constructor 1: สำหรับ "สร้างรายการใหม่" ---
        // จะถูกเรียกเมื่อผู้ใช้เลือกอิโมจิจาก HomePage แล้วเปิดหน้านี้ขึ้นมา
        // selectedMood คือชื่อของอิโมจิที่ผู้ใช้เลือกมา (เช่น "happy")
        public MoodTrackerPage(string selectedMood)
        {
            InitializeComponent(); // โหลด UI จาก MoodTrackerPage.xaml

            // พยายามดึง DbContext (ตัวจัดการฐานข้อมูล) ที่ลงทะเบียนไว้ใน MauiProgram.cs
            _dbContext = Application.Current?.Handler?.MauiContext?.Services.GetService<User>()
                         ?? throw new InvalidOperationException("DbContext (User) not found. Ensure it's registered in MauiProgram.cs");
            // ดึงชื่อผู้ใช้ที่ Login ล่าสุดจาก Preferences (ที่เก็บข้อมูลเล็กๆ ของแอป)
            _currentUserName = Preferences.Get("LoggedInUserName", "UnknownUser");

            _selectedMood = selectedMood; // เก็บชื่ออารมณ์ที่ส่งมา
            _isEditingMode = false;       // ตั้งค่าว่าเป็นโหมด "สร้างใหม่"
            SetupNewEntryUI();            // เรียกเมธอดเพื่อตั้งค่าหน้าจอสำหรับสร้างรายการใหม่
        }

        // --- Constructor 2: สำหรับ "แก้ไขรายการที่มีอยู่" ---
        // จะถูกเรียกเมื่อผู้ใช้กด "Edit" จาก DailyMoodTrackerPage
        // recordToEdit คือ MoodRecord (ข้อมูลเดิม) ที่ต้องการแก้ไข
        // dbContext และ currentUserName ถูกส่งมาจาก DailyMoodTrackerPage
        public MoodTrackerPage(MoodRecord recordToEdit, User dbContext, string currentUserName)
        {
            InitializeComponent(); // โหลด UI
            _dbContext = dbContext;             // ใช้ DbContext ที่ส่งมา
            _currentUserName = currentUserName; // ใช้ UserName ที่ส่งมา

            _editingMoodRecord = recordToEdit; // เก็บข้อมูลรายการที่จะแก้ไขไว้
            _isEditingMode = true;            // ตั้งค่าว่าเป็นโหมด "แก้ไข"

            // ดึงข้อมูลจาก recordToEdit มาใส่ในตัวแปรของหน้านี้
            _selectedMood = GetMoodNameFromSource(_editingMoodRecord.MoodEmojiSource); // แปลงชื่อไฟล์รูปอิโมจิกลับเป็นชื่ออารมณ์
            _rating = _editingMoodRecord.Rating;                                     // ดึงค่าคะแนนดาวเดิม
            _attachedImageFilePath = _editingMoodRecord.ImagePath;                   // ดึง Path รูปภาพเดิม (ถ้ามี)
            // *** ไม่ต้องดึง DiaryName มาแสดงในหน้านี้แล้ว ตามที่คุณแจ้ง ***

            LoadRecordForEditing(); // เรียกเมธอดเพื่อตั้งค่าหน้าจอสำหรับแก้ไข และแสดงข้อมูลเดิม
        }

        // --- เมธอดสำหรับตั้งค่า UI เมื่อเป็นการ "สร้างรายการใหม่" ---
        private void SetupNewEntryUI()
        {
            // Title = "Track New Mood";     // ตั้งชื่อเรื่องของหน้าจอ
            SaveButton.Text = "Save";       // เปลี่ยนข้อความบนปุ่มเป็น "Save"

            System.Diagnostics.Debug.WriteLine($"MoodTrackerPage (New): reaction = {_selectedMood}"); // Log สำหรับ Debug
            // แสดงอิโมจิที่ผู้ใช้เลือกมา
            SelectedEmojiImage.Source = $"{_selectedMood.ToLowerInvariant()}.png";
            // แสดงวันที่และวันปัจจุบัน
            DateLabel.Text = DateTime.Now.ToString("MMMM yyyy 'AD'", new CultureInfo("en-US")); // แก้ไข Format วันที่ให้สวยงามขึ้น
            DayLabel.Text = DateTime.Now.ToString("dddd", new CultureInfo("en-US")); // แสดงชื่อวันเต็ม

            // เตรียม List ของปุ่มดาว (Star1, Star2, Star3 คือ x:Name ของ ImageButton ใน XAML)
            _starButtons = new List<ImageButton> { Star1, Star2, Star3 };
            UpdateStars(); // เรียก UpdateStars() เพื่อให้ดาวแสดงเป็นแบบ outline (0 ดาว)
        }

        // --- เมธอดสำหรับตั้งค่า UI และโหลดข้อมูลเดิมมาแสดง เมื่อเป็นการ "แก้ไข" ---
        private void LoadRecordForEditing()
        {
            if (_editingMoodRecord == null) return; // ถ้าไม่มีข้อมูลให้แก้ไข ก็ไม่ต้องทำอะไร

            // Title = "Edit Mood Entry";      // ตั้งชื่อเรื่องของหน้าจอ
            SaveButton.Text = "Update";     // เปลี่ยนข้อความบนปุ่มเป็น "Update"

            System.Diagnostics.Debug.WriteLine($"MoodTrackerPage (Edit): MoodRecordId = {_editingMoodRecord.MoodRecordId}");

            // แสดงข้อมูลเดิมในช่องต่างๆ บนหน้าจอ
            SelectedEmojiImage.Source = _editingMoodRecord.MoodEmojiSource; // อิโมจิเดิม
            FeelingEntry.Text = _editingMoodRecord.FeelingLabel;         // ความรู้สึกเดิม
            DescriptionEntry.Text = _editingMoodRecord.Description;     // รายละเอียดเดิม
            // ชื่อไดอารี่เดิม (ถูกโหลดมาใน _editingMoodRecord แล้ว แต่ไม่แสดงในหน้านี้)

            // แสดงวันที่และวันของรายการที่กำลังแก้ไข (ไม่ใช่ของปัจจุบัน)
            DateLabel.Text = _editingMoodRecord.CreatedAt.ToString("MMMM dd,yyyy", new CultureInfo("en-US"));
            DayLabel.Text = _editingMoodRecord.CreatedAt.ToString("dddd", new CultureInfo("en-US"));

            _starButtons = new List<ImageButton> { Star1, Star2, Star3 }; // เตรียม List ปุ่มดาว
            UpdateStars(); // เรียก UpdateStars() เพื่อแสดงดาวตาม _rating ที่โหลดมา

            // ถ้ามีรูปภาพเดิมที่แนบไว้
            if (_editingMoodRecord.HasImage && !string.IsNullOrEmpty(_editingMoodRecord.ImagePath))
            {
                if (File.Exists(_editingMoodRecord.ImagePath)) // เช็คว่าไฟล์รูปนั้นยังอยู่จริงหรือไม่
                {
                    UserSelectedImage.Source = ImageSource.FromFile(_editingMoodRecord.ImagePath); // แสดงรูป
                    UserSelectedImage.IsVisible = true; // ทำให้ Image แสดงผล
                }
                else // ถ้าไฟล์รูปไม่มีแล้ว (อาจจะถูกลบไป)
                {
                    System.Diagnostics.Debug.WriteLine($"Image file not found for editing: {_editingMoodRecord.ImagePath}");
                    UserSelectedImage.IsVisible = false;      // ซ่อน Image
                    _attachedImageFilePath = null;            // เคลียร์ Path รูปที่เคยเก็บไว้
                                                              // การเปลี่ยนแปลง HasImage/ImagePath ในฐานข้อมูลจะเกิดขึ้นเมื่อผู้ใช้กด Save/Update
                }
            }
            else // ถ้าไม่มีรูปภาพแนบมา
            {
                UserSelectedImage.IsVisible = false; // ซ่อน Image
            }
        }

        // --- เมธอดช่วย: แปลงชื่อไฟล์รูปอิโมจิ (เช่น "good.png") กลับไปเป็นชื่ออารมณ์ (เช่น "good") ---
        private string GetMoodNameFromSource(string? emojiSource)
        {
            if (string.IsNullOrEmpty(emojiSource)) return "unknown"; // ถ้าไม่มี source หรือว่างเปล่า ก็คืนค่า "unknown"
            // Path.GetFileNameWithoutExtension จะตัดนามสกุลไฟล์ (.png) ออกไป
            return Path.GetFileNameWithoutExtension(emojiSource.ToLowerInvariant());
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกที่ปุ่มดาว ---
        private void Star_Clicked(object sender, EventArgs e)
        {
            if (sender is ImageButton star) // เช็คว่าสิ่งที่ถูกคลิกคือ ImageButton (ปุ่มดาว)
            {
                int clickedStarIndex = _starButtons.IndexOf(star); // หาว่าเป็นดาวดวงที่เท่าไหร่ (0, 1, หรือ 2)
                if (clickedStarIndex != -1) // ถ้าหาเจอ
                {
                    _rating = clickedStarIndex + 1; // กำหนดค่า _rating (1, 2, หรือ 3)
                    UpdateStars(); // เรียกเมธอดเพื่ออัปเดตการแสดงผลของดาว
                }
            }
        }

        // --- เมธอดสำหรับอัปเดตการแสดงผลของดาว (ให้เป็นดาวเต็ม หรือ ดาว outline) ---
        private void UpdateStars()
        {
            for (int i = 0; i < _starButtons.Count; i++) // วนลูป 3 ครั้งสำหรับดาว 3 ดวง
            {
                // ถ้า i (ตำแหน่งดาวปัจจุบัน 0, 1, 2) น้อยกว่า _rating (1, 2, 3) แสดงว่าเป็นดาวเต็ม
                // มิฉะนั้น แสดงเป็นดาว outline
                _starButtons[i].Source = i < _rating ? "star.png" : "star_outline.png";
            }
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Pick Image" ---
        private async void PickImageButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // เปิดหน้าต่างให้ผู้ใช้เลือกรูปภาพจากเครื่อง
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Please pick a photo" });
                if (result != null) // ถ้าผู้ใช้เลือกรูปภาพมา (ไม่ได้กด Cancel)
                {
                    // เตรียม Path สำหรับเก็บไฟล์รูปภาพที่คัดลอกมาไว้ใน Folder ของแอป
                    string localDir = FileSystem.AppDataDirectory; // Folder ที่แอปสามารถเขียนข้อมูลได้
                    string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(result.FileName)}"; // สร้างชื่อไฟล์ใหม่ที่ไม่ซ้ำกัน
                    string newFilePath = Path.Combine(localDir, newFileName); // รวม Path Folder กับชื่อไฟล์ใหม่

                    // คัดลอกข้อมูลรูปภาพจากไฟล์ที่ผู้ใช้เลือก มายัง Path ใหม่ที่สร้างขึ้น
                    using (var destStream = File.Create(newFilePath)) // สร้างไฟล์ปลายทาง
                    using (var sourceStream = await result.OpenReadAsync()) // เปิดอ่านไฟล์ต้นทาง
                    {
                        await sourceStream.CopyToAsync(destStream); // คัดลอกข้อมูล
                    }

                    // ถ้ากำลังแก้ไข และมีรูปเก่าอยู่แล้ว และรูปใหม่ไม่เหมือนรูปเก่า ให้พยายามลบรูปเก่า
                    if (_isEditingMode && !string.IsNullOrEmpty(_attachedImageFilePath) && _attachedImageFilePath != newFilePath)
                    {
                        if (File.Exists(_attachedImageFilePath))
                        {
                            try
                            {
                                File.Delete(_attachedImageFilePath);
                                System.Diagnostics.Debug.WriteLine($"Deleted old image: {_attachedImageFilePath}");
                            }
                            catch (Exception delEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error deleting old image: {delEx.Message}");
                            }
                        }
                    }

                    _attachedImageFilePath = newFilePath; // เก็บ Path ของรูปใหม่ที่คัดลอกมา
                    UserSelectedImage.Source = ImageSource.FromFile(_attachedImageFilePath); // แสดงรูปภาพบน UI จาก Path ใหม่
                    UserSelectedImage.IsVisible = true;    // ทำให้ Image แสดงผล
                    System.Diagnostics.Debug.WriteLine($"Image selected: {_attachedImageFilePath}");
                }
            }
            catch (PermissionException pEx) // ถ้าแอปไม่มีสิทธิ์เข้าถึงคลังรูปภาพ
            {
                System.Diagnostics.Debug.WriteLine($"Permission denied: {pEx.Message}");
                await DisplayAlert("Permission Denied", "Permission to access photos is required.", "OK");
            }
            catch (Exception ex) // ถ้ามีปัญหาอื่นๆ ตอนเลือกรูป
            {
                System.Diagnostics.Debug.WriteLine($"Error picking image: {ex.Message}");
                await DisplayAlert("Error", "Could not pick image.", "OK");
            }
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Save" หรือ "Update" ---
        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            string feeling = FeelingEntry.Text?.Trim() ?? string.Empty;       // ดึงข้อความ Feeling จากช่องกรอก
            string description = DescriptionEntry.Text?.Trim() ?? string.Empty; // ดึงข้อความ Description

            // ตรวจสอบข้อมูลเบื้องต้น
            if (string.IsNullOrWhiteSpace(_selectedMood) || _selectedMood == "unknown")
            {
                await DisplayAlert("Error", "Mood not properly selected.", "OK"); return;
            }
            if (string.IsNullOrWhiteSpace(feeling))
            {
                await DisplayAlert("Error", "Please enter your feeling.", "OK"); return;
            }

            // (เสริม) สามารถนำโค้ดส่วนนี้มาใช้เพื่อ Disable ปุ่มและแสดง Indicator ระหว่างบันทึก เพื่อป้องกันการกดซ้ำ
            // SaveButton.IsEnabled = false;
            // MyActivityIndicator.IsRunning = true; // ต้องมี x:Name="MyActivityIndicator" ใน XAML

            try
            {
                MoodRecord recordToSave; // ตัวแปรสำหรับเก็บ MoodRecord ที่จะบันทึกลงฐานข้อมูล
                string originalImagePathBeforeEdit = null; // เก็บ Path รูปเดิม (ถ้าเป็นการแก้ไข)

                if (_isEditingMode && _editingMoodRecord != null) // ถ้าเป็นโหมด "แก้ไข"
                {
                    // โหลดข้อมูลรายการที่ต้องการแก้ไขจากฐานข้อมูลอีกครั้ง
                    // เพื่อให้แน่ใจว่าเราทำงานกับ instance ที่ EF Core (ตัวจัดการฐานฐานข้อมูล) รู้จักและ Track การเปลี่ยนแปลงอยู่
                    recordToSave = await _dbContext.MoodRecords.FindAsync(_editingMoodRecord.MoodRecordId);
                    if (recordToSave == null) // ถ้าหาไม่เจอ (ไม่ควรเกิดขึ้น)
                    {
                        await DisplayAlert("Error", "Could not find the mood record to update.", "OK");
                        return;
                    }
                    originalImagePathBeforeEdit = recordToSave.ImagePath; // เก็บ Path รูปเดิมไว้ก่อน
                    System.Diagnostics.Debug.WriteLine($"Updating MoodRecordId: {recordToSave.MoodRecordId}, Original ImagePath: {originalImagePathBeforeEdit}");
                    // *** ไม่ต้องยุ่งกับ recordToSave.DiaryName ในโหมดแก้ไข ถ้าชื่อ Diary ควรคงเดิมตามที่บันทึกครั้งแรก ***

                    // อัปเดตค่าต่างๆ ของ recordToSave ด้วยข้อมูลจากหน้าจอ
                    recordToSave.MoodEmojiSource = $"{_selectedMood.ToLowerInvariant()}.png";
                    recordToSave.FeelingLabel = feeling;
                    recordToSave.Description = description;
                    recordToSave.Rating = _rating;
                    recordToSave.ImagePath = _attachedImageFilePath; // อัปเดต Path รูปภาพใหม่
                    recordToSave.HasImage = !string.IsNullOrEmpty(recordToSave.ImagePath);
                    recordToSave.UpdatedAt = DateTimeOffset.UtcNow; // บันทึกเวลาที่แก้ไขล่าสุด

                    // สั่งให้ DbContext บันทึกการเปลี่ยนแปลง (แก้ไข)
                    int recordsAffectedEdit = await _dbContext.SaveChangesAsync();

                    if (recordsAffectedEdit > 0) // ถ้าแก้ไขสำเร็จ
                    {
                        await DisplayAlert("Updated", "Your mood has been updated successfully!", "OK");
                        // ถ้าแก้ไขสำเร็จ: Pop กลับไปหน้าก่อนหน้า (ซึ่งควรจะเป็น DailyMoodTrackerPage)
                        // หน้า DailyMoodTrackerPage จะมี OnAppearing เพื่อโหลดข้อมูลใหม่เอง
                        await Navigation.PopAsync();
                    }
                     else // ถ้าแก้ไขล้มเหลว
                    {
                         await DisplayAlert("Update Failed", "Could not update your mood.", "OK");
                    }

                }
                else // ถ้าเป็นโหมด "สร้างใหม่" <--- การนำทางที่ถูกต้องสำหรับสร้างใหม่
                {
                    recordToSave = new MoodRecord // สร้าง MoodRecord object ใหม่
                    {
                        UserName = _currentUserName,
                        DiaryName = Preferences.Get("DiaryName", "My Diary"), // ดึงชื่อไดอารี่จาก Preferences
                        CreatedAt = DateTimeOffset.UtcNow,
                        // ค่าอื่นๆ จะกำหนดในส่วนอัปเดต/ตั้งค่า ด้านล่าง
                    };
                    _dbContext.MoodRecords.Add(recordToSave);

                    // *** ไม่ต้องบันทึก SaveChangesAsync ตรงนี้ ***
                    // *** การบันทึกจะทำครั้งเดียวที่ท้ายเมธอด ***
                     System.Diagnostics.Debug.WriteLine($"Creating new MoodRecord for user: {_currentUserName}. DiaryName from Prefs: {recordToSave.DiaryName}"); // เพิ่ม Debug เช็คค่า

                    // ค่าอื่นๆ ของ recordToSave จะถูกกำหนดในส่วนอัปเดต/ตั้งค่า ด้านล่าง
                }

                // --- อัปเดต/ตั้งค่า Properties ต่างๆ ของ recordToSave ด้วยข้อมูลจากหน้าจอ ---
                // *** ส่วนนี้ต้องอยู่ด้านล่าง if/else เพื่อให้กำหนดค่าให้กับ recordToSave ได้ทั้งโหมดใหม่และแก้ไข ***
                if (!_isEditingMode) // เฉพาะโหมดสร้างใหม่ ที่ยังไม่ได้กำหนดค่าเหล่านี้ในบล็อก if/else ด้านบน
                {
                    recordToSave.MoodEmojiSource = $"{_selectedMood.ToLowerInvariant()}.png"; // ชื่อไฟล์รูปอิโมจิ
                    recordToSave.FeelingLabel = feeling;                   // ความรู้สึกที่ผู้ใช้กรอก
                    recordToSave.Description = description;                 // รายละเอียดที่ผู้ใช้กรอก
                    recordToSave.Rating = _rating;                         // คะแนนดาว
                    // recordToSave.ImagePath และ HasImage จัดการแยกต่างหาก
                }
                 // จัดการ ImagePath และ HasImage สำหรับทั้งสองโหมด
                 // โค้ดส่วนจัดการรูปภาพนี้ ควรอยู่ด้านล่าง if/else block เพื่อให้ recordToSave ถูกสร้าง/โหลดแล้ว
                 if (!_isEditingMode && _attachedImageFilePath != null) // เฉพาะโหมดสร้างใหม่ที่มีรูปภาพแนบมา
                 {
                      recordToSave.ImagePath = _attachedImageFilePath;
                      recordToSave.HasImage = !string.IsNullOrEmpty(recordToSave.ImagePath);
                 }
                 // ในโหมดแก้ไข ส่วนจัดการรูปภาพอยู่ในบล็อก if (_isEditingMode) ด้านบนแล้ว

                // *** ย้าย SaveChangesAsync มาทำครั้งเดียวที่นี่ ***
                 int recordsAffected = await _dbContext.SaveChangesAsync();


                if (recordsAffected > 0) // ถ้าการบันทึกสำเร็จ (มีการเปลี่ยนแปลงข้อมูลอย่างน้อย 1 record)
                {
                    // แสดง Alert บอกผู้ใช้ว่าบันทึก/อัปเดตสำเร็จ
                    await DisplayAlert(_isEditingMode ? "Updated" : "Saved",
                                       $"Your mood has been {(_isEditingMode ? "updated" : "saved")} successfully!",
                                       "OK");

                    // --- Logic การนำทางหลังบันทึกสำเร็จ ---
                    if (_isEditingMode) // ถ้าเป็นการแก้ไขสำเร็จ
                    {
                         await Navigation.PopAsync(); // Pop กลับไป DailyMoodTrackerPage
                    }
                    else // ถ้าเป็นการสร้างใหม่สำเร็จ
                    {
                        // สร้างใหม่สำเร็จ: Pop หน้า MoodTrackerPage ออกไป (กลับไป HomePage)
                        await Navigation.PopAsync(); // Stack: HomePage -> MoodTrackerPage --> HomePage

                        // Push หน้า DailyMoodTrackerPage เข้ามาใหม่ (ต่อจาก HomePage)
                        await Navigation.PushAsync(new DailyMoodTrackerPage(_dbContext, _currentUserName)); // Stack: HomePage --> HomePage -> DailyMoodTrackerPage
                    }
                     // --- สิ้นสุด Logic การนำทาง ---

                }
                else // ถ้าการบันทึกล้มเหลว
                {
                    await DisplayAlert(_isEditingMode ? "Update Failed" : "Save Failed",
                                       $"Could not {(_isEditingMode ? "update" : "save")} your mood. No changes were made or an error occurred.",
                                       "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving/updating mood record: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // (เสริม) สามารถนำโค้ดส่วนนี้มาใช้เพื่อ Enable ปุ่มและซ่อน Indicator
                // SaveButton.IsEnabled = true;
                // MyActivityIndicator.IsRunning = false;
            }
        }
    }
}