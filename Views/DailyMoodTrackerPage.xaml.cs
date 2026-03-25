// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using MoodKitApp.Models; // เรียกใช้ Model 'MoodRecord' (โครงสร้างข้อมูลบันทึกอารมณ์)
using MoodKitApp.Data;   // เรียกใช้ 'User' (ตัวจัดการฐานข้อมูล)
using Microsoft.Maui.Controls; // Library หลักของ MAUI สำหรับสร้างหน้าจอและปุ่มต่างๆ
using Microsoft.EntityFrameworkCore; // Library สำหรับทำงานกับฐานข้อมูล (เช่น ToListAsync)
using Microsoft.Extensions.DependencyInjection; // สำหรับการเรียกใช้ Service ที่ลงทะเบียนไว้ (เช่น DbContext)
using System; // Library พื้นฐานของ C# (เช่น DateTime, EventArgs)
using System.Collections.ObjectModel; // สำหรับ ObservableCollection (List ที่ UI อัปเดตตามได้)
using System.ComponentModel; // สำหรับ INotifyPropertyChanged (แจ้งเตือน UI เมื่อข้อมูลเปลี่ยน)
using System.Globalization; // สำหรับการจัดรูปแบบวันที่/เวลา (เช่น แสดงชื่อเดือนเป็นภาษาอังกฤษ)
using System.Linq; // สำหรับการจัดการ List ข้อมูล (เช่น Where, OrderByDescending, FirstOrDefault)
using System.Runtime.CompilerServices; // สำหรับ CallerMemberName (ช่วยให้ INotifyPropertyChanged ทำงานง่ายขึ้น)
using System.Threading.Tasks; // สำหรับการทำงานแบบ Asynchronous (ไม่ให้ UI ค้าง เช่น ตอนโหลดข้อมูล)

namespace MoodKitApp.Views // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Views ของโปรเจกต์ MoodKitApp
{
    // ประกาศคลาส DailyMoodTrackerPage ซึ่งเป็นหน้าจอ (ContentPage)
    // และ implement INotifyPropertyChanged เพื่อให้ UI อัปเดตเมื่อ Property ในคลาสนี้เปลี่ยน
    public partial class DailyMoodTrackerPage : ContentPage, INotifyPropertyChanged
    {
        // --- ส่วนจัดการการแจ้งเตือน UI (INotifyPropertyChanged) ---
        public event PropertyChangedEventHandler? PropertyChanged; // Event ที่จะถูกเรียกเมื่อ Property เปลี่ยน

        // เมธอดนี้จะถูกเรียกเพื่อแจ้ง UI ว่า Property ชื่อ propertyName ได้เปลี่ยนค่าไปแล้ว
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Property สำหรับเก็บข้อมูลที่จะแสดงบนหน้าจอ ---
        // DailyEntries คือ List ของ MoodRecord ที่จะแสดงใน CollectionView (รายการบันทึกอารมณ์)
        // ObservableCollection จะช่วยให้ UI อัปเดตอัตโนมัติเมื่อมีการเพิ่ม/ลบข้อมูลใน List นี้
        public ObservableCollection<MoodRecord> DailyEntries { get; set; }

        // _isOverlayVisible ใช้เก็บสถานะว่า Overlay (หน้าต่างเล็กๆ ที่มีปุ่ม Edit/Delete) กำลังแสดงอยู่หรือไม่
        private bool _isOverlayVisible;
        // IsOverlayVisible เป็น Property ที่ UI ผูกข้อมูลด้วย (Binding)
        // เมื่อค่าของมันเปลี่ยน OnPropertyChanged จะถูกเรียก เพื่อให้ UI อัปเดต (แสดง/ซ่อน Overlay)
        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set
            {
                if (_isOverlayVisible != value) // เช็คว่าค่าเปลี่ยนจริงหรือไม่
                {
                    _isOverlayVisible = value;
                    OnPropertyChanged(); // แจ้ง UI ว่าค่า IsOverlayVisible เปลี่ยนแล้ว
                }
            }
        }

        // _selectedEntry ใช้เก็บ MoodRecord ที่ผู้ใช้กำลังเลือก (ผ่านการคลิกที่ปุ่มเมนูของรายการนั้นๆ)
        private MoodRecord? _selectedEntry;
        // _dbContext คือตัวแทนของฐานข้อมูล ทำให้เราสามารถอ่าน/เขียนข้อมูลจากฐานข้อมูลได้
        private readonly User _dbContext;
        // _currentUserName เก็บชื่อผู้ใช้ที่กำลัง Login อยู่ เพื่อให้แสดงเฉพาะข้อมูลของคนๆ นั้น
        private readonly string _currentUserName;
        // _focusMoodRecordId (Optional) เก็บ ID ของ MoodRecord ที่ต้องการให้ Focus หรือแสดง Overlay ทันทีที่เปิดหน้านี้
        private readonly int? _focusMoodRecordId;

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อหน้านี้ถูกสร้างขึ้น) ---
        // Constructor หลัก: รับค่า dbContext, currentUserName, และ focusMoodRecordId (ถ้ามี) มาจากหน้าที่เรียกมัน
        public DailyMoodTrackerPage(User dbContext, string currentUserName, int? focusMoodRecordId = null)
        {
            InitializeComponent(); // โหลดส่วนประกอบ UI จากไฟล์ .xaml
            DailyEntries = new ObservableCollection<MoodRecord>(); // สร้าง List ว่างๆ สำหรับเก็บข้อมูลที่จะแสดง
            this.BindingContext = this; // ตั้งค่าให้ XAML สามารถผูกข้อมูลกับ Property ในคลาสนี้ได้ (เช่น IsOverlayVisible, DailyEntries)

            _dbContext = dbContext; // เก็บตัวจัดการฐานข้อมูลที่รับมา
            _currentUserName = currentUserName; // เก็บชื่อผู้ใช้ปัจจุบัน
            _focusMoodRecordId = focusMoodRecordId; // เก็บ ID ที่ต้องการให้ Focus (ถ้ามี)

            // ตั้งค่า Label แสดงเดือนและปีปัจจุบัน (เช่น "May 2025 AD")
            DateLabel.Text = DateTime.Now.ToString("MMMM yyyy 'AD'", new CultureInfo("en-US"));
            IsOverlayVisible = false; // เริ่มต้นให้ Overlay ซ่อนอยู่
        }

        // Constructor สำรอง: ถูกเรียกเมื่อ MAUI สร้างหน้านี้โดยไม่ได้ส่ง Parameters มาให้โดยตรง (เช่น จากการ Navigate ใน XAML)
        // มันจะพยายามดึง DbContext และ UserName ที่จำเป็นเอง
        public DailyMoodTrackerPage() : this(
            // พยายามดึง DbContext ที่ลงทะเบียนไว้ใน MauiProgram.cs
            Application.Current?.Handler?.MauiContext?.Services.GetService<User>()
                ?? throw new InvalidOperationException("DbContext (User) not found. Ensure it's registered in MauiProgram.cs for DailyMoodTrackerPage default constructor."),
            // พยายามดึงชื่อผู้ใช้ที่ Login ล่าสุดจาก Preferences (ที่เก็บข้อมูลเล็กๆน้อยๆของแอป)
            Preferences.Get("LoggedInUserName", "UnknownUser")
        )
        {
            // พิมพ์ Log บอกว่า Constructor นี้ถูกเรียก
            System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage: Default constructor called. Attempting to resolve dependencies.");
            // Constructor นี้จะไปเรียก Constructor หลักด้านบนอีกที โดย _focusMoodRecordId จะเป็น null
        }

        // --- เมธอดที่ทำงานเมื่อหน้าจอถูกแสดงผล (Life Cycle Method) ---
        // OnAppearing จะถูกเรียกทุกครั้งที่หน้านี้ปรากฏขึ้นบนหน้าจอ (เช่น ตอนเปิดหน้านี้ครั้งแรก หรือตอน Pop กลับมาจากหน้าอื่น)
        protected override async void OnAppearing()
        {
            base.OnAppearing(); // เรียกการทำงานพื้นฐานของ OnAppearing
            // พิมพ์ Log บอกว่า OnAppearing ถูกเรียก และกำลังจะโหลดข้อมูล
            System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage: OnAppearing called for user: {_currentUserName}. Focusing ID: {_focusMoodRecordId}");
            await LoadDailyEntriesAsync(); // เรียกเมธอดโหลดข้อมูลรายการบันทึกอารมณ์
        }

       // --- เมธอดสำหรับโหลดข้อมูลรายการบันทึกอารมณ์ ---

        private async Task LoadDailyEntriesAsync()
        {
            System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.LoadDailyEntriesAsync: Started loading.");
            // ตรวจสอบว่า DbContext (ตัวจัดการฐานข้อมูล) พร้อมใช้งานหรือไม่
            if (_dbContext == null)
            {
                System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.LoadDailyEntriesAsync: DbContext is null.");
                await DisplayAlert("Error", "Database context is not available. Please restart the app.", "OK");
                return; // ออกจากเมธอดถ้าไม่มี DbContext
            }
            // ตรวจสอบว่ามีชื่อผู้ใช้ปัจจุบันหรือไม่
            if (string.IsNullOrEmpty(_currentUserName) || _currentUserName == "UnknownUser")
            {
                System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.LoadDailyEntriesAsync: CurrentUserName is not set.");
                await DisplayAlert("Error", "User information is not available. Please log in again.", "OK");
                return; // ออกจากเมธอดถ้าไม่มีชื่อผู้ใช้
            }

            // พิมพ์ Log ว่ากำลังโหลดข้อมูลของใคร
            System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: Loading entries for user '{_currentUserName}'");
            try
            {
                // 1. ดึงข้อมูล MoodRecord จากฐานข้อมูล
                //    - กรอง (Where) เอาเฉพาะรายการที่เป็นของ _currentUserName
                //    - ToListAsync() คือการสั่งให้ดึงข้อมูลทั้งหมดที่ตรงเงื่อนไขออกมาจากฐานข้อมูลแบบไม่รอ (Asynchronous)
                // *** เพิ่ม .AsNoTracking() ตรงนี้ เพื่อให้แน่ใจว่าดึงข้อมูลล่าสุดจาก DB ***
                var entriesFromDb = await _dbContext.MoodRecords
                                                .AsNoTracking()
                                                .Where(mr => mr.UserName == _currentUserName)
                                                .ToListAsync();

                // 2. เรียงลำดับข้อมูลที่ได้มา (ในหน่วยความจำของแอป)
                //    - OrderByDescending(mr => mr.CreatedAt) คือเรียงจากใหม่สุดไปเก่าสุด โดยดูจากเวลาที่สร้าง (CreatedAt)
                var sortedEntries = entriesFromDb
                                        .OrderByDescending(mr => mr.CreatedAt)
                                        .ToList();

                // --- โค้ด DEBUG ที่เคยใส่ไว้ ยังคงอยู่ได้ ---
                System.Diagnostics.Debug.WriteLine("--- ตรวจสอบค่า DiaryName หลังจากโหลด ---");
                if (sortedEntries.Any())
                {
                    foreach (var entry in sortedEntries)
                    {
                        System.Diagnostics.Debug.WriteLine($"MoodRecord ID: {entry.MoodRecordId}, CreatedAt: {entry.CreatedAt.ToString("g")}, DiaryName: '{entry.DiaryName}'");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No entries loaded for this user.");
                }
                System.Diagnostics.Debug.WriteLine("-------------------------------------");
                // --- สิ้นสุดโค้ด DEBUG ---


                // 3. อัปเดต UI (ObservableCollection) บน Main Thread เพื่อความปลอดภัย
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DailyEntries.Clear();
                    foreach (var entry in sortedEntries)
                    {
                        DailyEntries.Add(entry);
                    }
                    System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: {DailyEntries.Count} entries loaded into ObservableCollection.");

                    // ตรวจสอบว่ามีข้อมูลหรือไม่ เพื่อแสดง/ซ่อน Label "NoEntriesLabel" และ CollectionView
                    NoEntriesLabel.IsVisible = !DailyEntries.Any();
                    DailyMoodEntriesCollectionView.IsVisible = DailyEntries.Any();
                    System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: NoEntriesLabel.IsVisible = {NoEntriesLabel.IsVisible}");

                    // *** ลบ Logic ส่วนที่ทำให้ Overlay แสดงขึ้นมาอัตโนมัติออกไป ***
                    // ถ้ามีการระบุ _focusMoodRecordId มา (เช่น ตอนกลับมาจากการแก้ไข)
                    // เรายังสามารถหา _selectedEntry ไว้ได้ เผื่อผู้ใช้กด Menu ทันที
                    if (_focusMoodRecordId.HasValue && DailyEntries.Any())
                    {
                        _selectedEntry = DailyEntries.FirstOrDefault(entry => entry.MoodRecordId == _focusMoodRecordId.Value);
                         // ไม่ต้องเรียก IsOverlayVisible = true; ตรงนี้แล้ว
                        if (_selectedEntry != null)
                        {
                             System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: Focused MoodRecordId: {_selectedEntry.MoodRecordId} found.");
                        }
                        else
                        {
                             System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: Focused MoodRecordId {_focusMoodRecordId.Value} not found in loaded entries.");
                        }
                    }
                    else
                    {
                         _selectedEntry = null; // ถ้าไม่ได้ Focus ID ไหน ก็เคลียร์ _selectedEntry
                    }
                    IsOverlayVisible = false; // *** ต้องมั่นใจว่า Overlay ปิดอยู่เสมอเมื่อโหลดหน้าเสร็จ ***
                    System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.LoadDailyEntriesAsync: IsOverlayVisible set to {IsOverlayVisible} after loading.");

                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading daily entries: {ex.Message}");
                if (ex.InnerException != null) System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                await DisplayAlert("Error", $"Could not load daily entries. {ex.GetType().Name}", "OK");
            }
            System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.LoadDailyEntriesAsync: Exit."); // เพิ่ม Log สิ้นสุด
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่มต่างๆ บนหน้าจอ ---

        // เมื่อคลิกที่ไอคอนเมนู (สามจุด) ของรายการบันทึกอารมณ์
        private void OnEditDeleteClicked(object sender, EventArgs e)
        {
            // ตรวจสอบว่า sender เป็น ImageButton และมี CommandParameter (ซึ่งก็คือ MoodRecord ของรายการนั้น)
            if (sender is ImageButton button && button.CommandParameter is MoodRecord selectedEntry)
            {
                _selectedEntry = selectedEntry; // เก็บ MoodRecord ที่ถูกเลือกไว้
                IsOverlayVisible = true;      // สั่งให้ Overlay (ที่มีปุ่ม Edit/Delete) แสดงขึ้นมา
                System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.OnEditDeleteClicked: Overlay shown for MoodRecordId: {_selectedEntry.MoodRecordId}");
            }
        }

        // เมื่อคลิกปุ่ม "Edit" บน Overlay
        private async void OnEditClicked(object sender, EventArgs e)
        {
            IsOverlayVisible = false; // ซ่อน Overlay ก่อน
            if (_selectedEntry != null) // ถ้ามีรายการที่ถูกเลือกไว้
            {
                System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.OnEditClicked: Navigating to edit MoodRecordId: {_selectedEntry.MoodRecordId}");
                // นำทางไปยังหน้า MoodTrackerPage เพื่อแก้ไข โดยส่ง MoodRecord ที่เลือก, DbContext, และ UserName ปัจจุบันไปด้วย
                await Navigation.PushAsync(new MoodTrackerPage(_selectedEntry, _dbContext, _currentUserName));
            }
        }

        // เมื่อคลิกปุ่ม "Delete" บน Overlay
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (_selectedEntry == null) // ถ้าไม่มีรายการถูกเลือก ก็ไม่ต้องทำอะไร
            {
                System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.OnDeleteClicked: _selectedEntry is null.");
                IsOverlayVisible = false; // เผื่อ Overlay ค้าง ก็ซ่อนไป
                return;
            }

            IsOverlayVisible = false; // ซ่อน Overlay ก่อนแสดง Alert ยืนยัน
            // แสดง Alert ถามผู้ใช้ว่าต้องการลบจริงๆ ใช่ไหม
            bool confirmDelete = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete the entry from {(_selectedEntry.CreatedAt.ToString("g"))}?", "Yes", "No");

            if (confirmDelete) // ถ้าผู้ใช้กด "Yes"
            {
                System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.OnDeleteClicked: Deleting MoodRecordId: {_selectedEntry.MoodRecordId}");
                try
                {
                    // หา MoodRecord ในฐานข้อมูลจาก ID ของ _selectedEntry อีกครั้ง (เพื่อความชัวร์ว่าทำงานกับ Entity ที่ DbContext รู้จัก)
                    var entryToDeleteInDb = await _dbContext.MoodRecords.FindAsync(_selectedEntry.MoodRecordId);

                    if (entryToDeleteInDb != null) // ถ้าเจอรายการในฐานข้อมูล
                    {
                        // ตรวจสอบและลบไฟล์รูปภาพที่เกี่ยวข้อง (ถ้ามี)
                        if (entryToDeleteInDb.HasImage && !string.IsNullOrEmpty(entryToDeleteInDb.ImagePath))
                        {
                            if (File.Exists(entryToDeleteInDb.ImagePath)) // เช็คว่าไฟล์มีอยู่จริง
                            {
                                try
                                {
                                    File.Delete(entryToDeleteInDb.ImagePath); // สั่งลบไฟล์
                                    System.Diagnostics.Debug.WriteLine($"Deleted image file: {entryToDeleteInDb.ImagePath}");
                                }
                                catch (IOException ioEx) // ดักจับปัญหาตอนลบไฟล์ (เช่น ไฟล์ถูกเปิดค้างอยู่)
                                {
                                    System.Diagnostics.Debug.WriteLine($"IO Error deleting image file {entryToDeleteInDb.ImagePath}: {ioEx.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Image file not found, cannot delete: {entryToDeleteInDb.ImagePath}");
                            }
                        }

                        _dbContext.MoodRecords.Remove(entryToDeleteInDb); // สั่งให้ DbContext เตรียมลบรายการนี้
                        int recordsAffected = await _dbContext.SaveChangesAsync(); // สั่งให้บันทึกการเปลี่ยนแปลง (ลบจริง) ลงฐานข้อมูล
                        System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.OnDeleteClicked: Records affected in DB: {recordsAffected}");

                        if (recordsAffected > 0) // ถ้าการลบในฐานข้อมูลสำเร็จ
                        {
                            // ลบรายการออกจาก DailyEntries (ObservableCollection) เพื่อให้ UI อัปเดตทันที
                            // ควรทำบน MainThread เพื่อความปลอดภัย
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                bool removedFromUI = DailyEntries.Remove(_selectedEntry); // ใช้ _selectedEntry ตัวเดิมที่ UI ผูกอยู่
                                System.Diagnostics.Debug.WriteLine($"DailyMoodTrackerPage.OnDeleteClicked: Removed from UI collection: {removedFromUI}");

                                // อัปเดตการแสดงผลของ NoEntriesLabel และ CollectionView อีกครั้ง
                                NoEntriesLabel.IsVisible = !DailyEntries.Any();
                                DailyMoodEntriesCollectionView.IsVisible = DailyEntries.Any();
                            });

                            await DisplayAlert("Deleted", "Entry deleted successfully.", "OK");

                            // แจ้ง HomePage (ถ้ามันอยู่ใน Navigation Stack) ให้โหลดข้อมูลใหม่ด้วย
                            var homePage = Navigation.NavigationStack.OfType<HomePage>().FirstOrDefault();
                            homePage?.RefreshMoodEntries(); // เรียกเมธอด Refresh ของ HomePage (ถ้ามี)
                        }
                        else
                        {
                            await DisplayAlert("Delete Failed", "Could not delete the entry from the database.", "OK");
                        }
                    }
                    else // ถ้าไม่เจอรายการที่จะลบในฐานข้อมูล (อาจจะถูกลบไปแล้วจากที่อื่น)
                    {
                        await DisplayAlert("Not Found", "The entry to delete was not found in the database.", "OK");
                        await LoadDailyEntriesAsync(); // โหลดข้อมูลใหม่เพื่อ Sync UI
                    }
                }
                catch (Exception ex) // ถ้ามีปัญหาอื่นๆ ตอนลบ
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting entry: {ex.Message}");
                    if (ex.InnerException != null) System.Diagnostics.Debug.WriteLine($"Inner ex: {ex.InnerException.Message}");
                    await DisplayAlert("Error", "Could not delete the entry.", "OK");
                }
            }
            _selectedEntry = null; // เคลียร์ _selectedEntry หลังจากทำงานเสร็จ (ไม่ว่าจะลบหรือไม่)
        }

        // เมื่อคลิกปุ่ม "Cancel" บน Overlay
        private void OnCancelClicked(object sender, EventArgs e)
        {
            IsOverlayVisible = false; // ซ่อน Overlay
            _selectedEntry = null;    // เคลียร์รายการที่ถูกเลือก
            System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage.OnCancelClicked: Overlay cancelled.");
        }

        // --- เมธอดสำหรับให้หน้าอื่นเรียกเพื่อ Refresh ข้อมูลในหน้านี้ ---
        public async Task RefreshDailyEntriesAsync()
        {
            System.Diagnostics.Debug.WriteLine("DailyMoodTrackerPage: RefreshDailyEntriesAsync explicitly called.");
            await LoadDailyEntriesAsync(); // เรียกเมธอดโหลดข้อมูลใหม่
        }
    }
}