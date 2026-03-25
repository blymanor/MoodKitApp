// MoodKitApp/Model/MoodRecord.cs
// นี่คือ "พิมพ์เขียว" หรือ "โครงสร้าง" สำหรับเก็บข้อมูลการบันทึกอารมณ์แต่ละครั้ง
// เราเรียกมันว่า Model

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;         // Library หลักของ MAUI (อาจจะไม่จำเป็นต้องใช้โดยตรงใน Model นี้ นอกจาก ImageSource ถ้ายังใช้อยู่)
using System;                           // Library พื้นฐาน C# (DateTimeOffset, string, int, bool)
using System.ComponentModel;            // สำหรับ INotifyPropertyChanged (แจ้งเตือน UI เมื่อข้อมูลเปลี่ยน)
using System.Runtime.CompilerServices;    // สำหรับ CallerMemberName (ช่วยให้ INotifyPropertyChanged ทำงานง่ายขึ้น)
using System.ComponentModel.DataAnnotations; // สำหรับ Data Annotations (บอก EF Core เกี่ยวกับคุณสมบัติของ Property เช่น [Key])
using System.ComponentModel.DataAnnotations.Schema; // สำหรับ Data Annotations ที่เกี่ยวกับ Schema ฐานข้อมูล (เช่น [DatabaseGenerated])

namespace MoodKitApp.Models // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Models
{
    // ประกาศคลาส MoodRecord
    // implement INotifyPropertyChanged เพื่อให้ UI (หน้าจอ) สามารถอัปเดตตัวเองได้อัตโนมัติเมื่อค่า Property ในนี้เปลี่ยน
    public class MoodRecord : INotifyPropertyChanged
    {
        // --- กลไกการแจ้งเตือน UI (INotifyPropertyChanged) ---
        // PropertyChanged: Event ที่จะถูก "ยิง" เมื่อค่าของ Property ใดๆ ในคลาสนี้เปลี่ยนไป
        public event PropertyChangedEventHandler? PropertyChanged;

        // OnPropertyChanged: เมธอดที่จะ "ยิง" Event PropertyChanged
        // [CallerMemberName] จะช่วยให้ C# รู้โดยอัตโนมัติว่า Property ชื่ออะไรที่กำลังเรียกเมธอดนี้
        // ทำให้เราไม่ต้องพิมพ์ชื่อ Property เอง ลดโอกาสพิมพ์ผิด
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Properties (คุณสมบัติ/ข้อมูล) ของ MoodRecord ที่จะถูกเก็บลงฐานข้อมูล ---

        [Key] // บอก EF Core ว่า Property นี้คือ "กุญแจหลัก" (Primary Key) ของตารางนี้
              //  (แต่ละ MoodRecord จะมีค่า MoodRecordId ที่ไม่ซ้ำกัน)
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // บอก EF Core ให้ฐานข้อมูลสร้างค่า ID นี้ให้โดยอัตโนมัติเมื่อมีการเพิ่มข้อมูลใหม่
        public int MoodRecordId { get; set; } // ID ของการบันทึกอารมณ์แต่ละครั้ง (เป็นตัวเลข)

        public string? UserName { get; set; }     // ชื่อผู้ใช้ (Username) ที่สร้างการบันทึกนี้ (เครื่องหมาย ? บอกว่าค่านี้อาจจะเป็น null ได้)
        public DateTimeOffset CreatedAt { get; set; } // วันที่และเวลาที่สร้างการบันทึกนี้ (เก็บรวม Timezone offset)
        public DateTimeOffset? UpdatedAt { get; set; } // วันที่และเวลาที่แก้ไขการบันทึกนี้ล่าสุด (เครื่องหมาย ? บอกว่าอาจจะยังไม่เคยถูกแก้ไขเลยก็ได้)

        public string? DiaryName { get; set; }       // ชื่อไดอารี่ (อาจจะมาจากที่ผู้ใช้ตั้ง)
        public string MoodEmojiSource { get; set; }   // ชื่อไฟล์ของรูปอิโมจิที่เลือก (เช่น "happy.png", "sad.png")
        public string FeelingLabel { get; set; }      // ข้อความสั้นๆ ที่ผู้ใช้อธิบายความรู้สึก (เช่น "มีความสุข", "เศร้า")
        public string Description { get; set; }       // รายละเอียดเพิ่มเติมที่ผู้ใช้บันทึก

        // _rating: ตัวแปร "หลังบ้าน" สำหรับเก็บค่าคะแนนดาวจริงๆ (เป็น private)
        private int _rating;
        // Rating: Property "หน้าบ้าน" ที่ UI และโค้ดส่วนอื่นใช้เข้าถึงค่าคะแนนดาว
        public int Rating
        {
            get => _rating; // เมื่อมีการอ่านค่า Rating จะคืนค่า _rating
            set // เมื่อมีการกำหนดค่าใหม่ให้กับ Rating
            {
                if (_rating != value) // เช็คว่าค่าใหม่ไม่เหมือนค่าเดิม (เพื่อไม่ให้ทำงานซ้ำซ้อนถ้าค่าไม่เปลี่ยน)
                {
                    _rating = value;                // กำหนดค่าใหม่ให้กับ _rating
                    OnPropertyChanged();            // แจ้ง UI ว่าค่า "Rating" เปลี่ยนแล้ว (เพื่อให้ UI ที่ผูกกับ Rating อัปเดต)

                    // แจ้ง UI ว่า Properties อื่นๆ ที่ขึ้นอยู่กับ Rating (เช่น ดาวที่จะแสดง) ก็อาจจะต้องอัปเดตด้วย
                    OnPropertyChanged(nameof(Star1Source));
                    OnPropertyChanged(nameof(Star2Source));
                    OnPropertyChanged(nameof(Star3Source));
                    OnPropertyChanged(nameof(ShowStar1));
                    OnPropertyChanged(nameof(ShowStar2));
                    OnPropertyChanged(nameof(ShowStar3));
                }
            }
        }

        public bool HasImage { get; set; }      // บอกว่าการบันทึกนี้มีการแนบรูปภาพหรือไม่ (true = มี, false = ไม่มี)
        public string? ImagePath { get; set; }   // ที่อยู่ (Path) ของไฟล์รูปภาพที่แนบ (ถ้ามี)

        // --- Properties ช่วย (Helper Properties) สำหรับการแสดงผลบน UI (ไม่ถูกเก็บลงฐานข้อมูล) ---
        // [NotMapped] บอก EF Core ว่า "ไม่ต้องสร้างคอลัมน์สำหรับ Property นี้ในฐานข้อมูลนะ"
        // เพราะ Property เหล่านี้คำนวณค่ามาจาก Property อื่น (Rating) และใช้สำหรับแสดงผลเท่านั้น

        [NotMapped] // ดาวดวงที่ 1 จะแสดง (true) ถ้า Rating >= 1
        public bool ShowStar1 => Rating >= 1;
        [NotMapped] // ดาวดวงที่ 2 จะแสดง (true) ถ้า Rating >= 2
        public bool ShowStar2 => Rating >= 2;
        [NotMapped] // ดาวดวงที่ 3 จะแสดง (true) ถ้า Rating >= 3
        public bool ShowStar3 => Rating >= 3;

        [NotMapped] // ชื่อไฟล์รูปดาวดวงที่ 1 (ถ้า Rating >= 1 ให้ใช้ "star.png" (ดาวเต็ม) ไม่งั้นใช้ "star_outline.png" (ดาวโปร่ง))
        public string Star1Source => Rating >= 1 ? "star.png" : "star_outline.png";
        [NotMapped]
        public string Star2Source => Rating >= 2 ? "star.png" : "star_outline.png";
        [NotMapped]
        public string Star3Source => Rating >= 3 ? "star.png" : "star_outline.png";

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อมีการสร้าง Object MoodRecord ใหม่) ---
        public MoodRecord()
        {
            // กำหนดค่าเริ่มต้นให้กับบาง Property เมื่อ MoodRecord ถูกสร้างขึ้นใหม่ (ยังไม่ได้มาจากฐานข้อมูล)
            MoodEmojiSource = string.Empty; // เริ่มต้นให้อิโมจิเป็นค่าว่าง
            FeelingLabel = string.Empty;    // เริ่มต้นให้ความรู้สึกเป็นค่าว่าง
            Description = string.Empty;     // เริ่มต้นให้รายละเอียดเป็นค่าว่าง
            CreatedAt = DateTimeOffset.UtcNow; // ตั้งเวลาที่สร้างเป็นเวลาปัจจุบัน (UTC - เวลาสากลเชิงพิกัด)
                                               // UpdatedAt จะเป็น null ในตอนแรก
        }
    }
}