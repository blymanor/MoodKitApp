// MoodKitApp/Controls/ReactionPicker.xaml.cs
// นี่คือไฟล์โค้ด C# สำหรับ ReactionPicker ซึ่งเป็น Control ที่คุณสร้างเอง (ContentView)
// เพื่อให้ผู้ใช้สามารถเลือกอิโมจิแสดงอารมณ์ได้

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;     // Library หลักของ MAUI สำหรับ UI
using Microsoft.Maui.Layouts;     // Library สำหรับจัดการ Layout (อาจจะไม่ได้ใช้โดยตรงในโค้ดนี้ แต่มีประโยชน์)
using System;                       // Library พื้นฐาน C#
using System.Collections.Generic;   // สำหรับการใช้ List

namespace MoodKitApp.Controls // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Controls
{
    // ประกาศคลาส ReactionPicker ซึ่งสืบทอดมาจาก ContentView (เป็น Container สำหรับ UI อื่นๆ)
    public partial class ReactionPicker : ContentView
    {
        // --- ค่าคงที่ (Constants) สำหรับการคำนวณตำแหน่ง (แต่ยังไม่ได้ใช้คำนวณจริงใน PositionEmojis ปัจจุบัน) ---
        private const double Radius = 90; // รัศมีของวงกลม/ส่วนโค้งที่ต้องการให้อิโมจิอยู่
        private const double StartAngleDegrees = -90; // มุมเริ่มต้นสำหรับการวางอิโมจิตัวแรก ( -90 คือด้านบนสุด)
        private const double centerY = 0.4; // อาจจะเกี่ยวกับจุดศูนย์กลางแนวตั้ง (ยังไม่ชัดเจนว่าใช้อย่างไร)

        // _emojiButtons: List สำหรับเก็บ ImageButton ของอิโมจิทั้ง 5 อัน (Emoji1, Emoji2,... จาก XAML)
        private List<ImageButton> _emojiButtons;

        // --- Events (เหตุการณ์) ที่ Control นี้สามารถส่งออกไปให้หน้าที่เรียกใช้มัน (เช่น HomePage) ---
        // ReactionSelected: จะถูกเรียกเมื่อผู้ใช้คลิกเลือกอิโมจิ โดยจะส่งชื่อของอิโมจิ (string) ออกไปด้วย
        public event EventHandler<string>? ReactionSelected;
        // CloseRequested: จะถูกเรียกเมื่อผู้ใช้กดปุ่มปิด Picker (ถ้ามีปุ่มปิดใน XAML ของ Picker นี้)
        public event EventHandler? CloseRequested;

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อ ReactionPicker นี้ถูกสร้างขึ้น) ---
        public ReactionPicker()
        {
            InitializeComponent(); // โหลด UI จากไฟล์ ReactionPicker.xaml
            System.Diagnostics.Debug.WriteLine("ReactionPicker Constructor Called"); // Log สำหรับ Debug

            // สร้าง List ของ ImageButton โดยดึงมาจาก x:Name ที่ตั้งไว้ใน XAML
            // (ต้องแน่ใจว่าใน ReactionPicker.xaml มี ImageButton ชื่อ Emoji1, Emoji2, ..., Emoji5)
            _emojiButtons = new List<ImageButton>
            {
                Emoji1, Emoji2, Emoji3, Emoji4, Emoji5
            };

            // เมื่อขนาดของ ReactionContainer (Layout หลักใน XAML ของ Picker นี้) เปลี่ยนแปลง
            // ให้เรียกเมธอด PositionEmojis() เพื่อจัดตำแหน่งอิโมจิใหม่
            // (ReactionContainer คือ x:Name ของ Layout ที่ครอบปุ่มอิโมจิใน ReactionPicker.xaml)
            ReactionContainer.SizeChanged += (s, e) => PositionEmojis();
        }

        // --- เมธอดสำหรับ "พยายาม" จัดตำแหน่งอิโมจิ ---
        // !!! ข้อสังเกต: โค้ดปัจจุบันในเมธอดนี้ยังไม่ได้ทำการจัดตำแหน่งอิโมจิจริงๆ !!!
        // มันแค่ตั้งค่า Scale และ Opacity ของปุ่มเท่านั้น
        private void PositionEmojis()
        {
            System.Diagnostics.Debug.WriteLine("--- ReactionPicker Layout Start ---");
            System.Diagnostics.Debug.WriteLine($"Container Width: {ReactionContainer.Width}, Height: {ReactionContainer.Height}");
            System.Diagnostics.Debug.WriteLine($"Emoji Count: {_emojiButtons.Count}");

            // ตรวจสอบเบื้องต้นว่ามีปุ่มครบ 5 ปุ่ม และ ReactionContainer มีขนาดที่ถูกต้อง (ไม่เป็น 0)
            if (_emojiButtons.Count != 5 || ReactionContainer.Width <= 0 || ReactionContainer.Height <= 0)
            {
                System.Diagnostics.Debug.WriteLine("--- ReactionPicker Layout End (Early Return - Incorrect Count or Zero Container) ---");
                return; // ถ้าเงื่อนไขไม่ถูกต้อง ก็ไม่ต้องทำอะไรต่อ
            }

            // โค้ดส่วนนี้แค่ตั้งค่า Scale (ขนาด) ให้เป็น 1 (ขนาดปกติ)
            // และ Opacity (ความโปร่งใส) ให้เป็น 1 (ไม่โปร่งใส)
            // *** ยังไม่มีการคำนวณ X, Y เพื่อวางปุ่มในแนวโค้ง ***
            foreach (var button in _emojiButtons)
            {
                button.Scale = 1;
                button.Opacity = 1;
            }

            System.Diagnostics.Debug.WriteLine("--- ReactionPicker Layout End ---");
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Close" (ถ้ามีปุ่มชื่อ CloseButton ใน XAML) ---
        private void CloseButton_Clicked(object sender, EventArgs e)
        {
            // ส่ง Event 'CloseRequested' ออกไป (เพื่อให้ HomePage รู้ว่าต้องปิด Picker นี้)
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกที่ปุ่มอิโมจิใดๆ ---
        private void EmojiButton_Clicked(object sender, EventArgs e)
        {
            // ตรวจสอบว่าสิ่งที่ถูกคลิกคือ ImageButton และมี CommandParameter (ซึ่งเราตั้งไว้ใน XAML ให้เป็นชื่ออิโมจิ)
            if (sender is ImageButton button && button.CommandParameter is string emojiName)
            {
                // ส่ง Event 'ReactionSelected' ออกไป พร้อมกับชื่ออิโมจิที่ถูกเลือก (emojiName)
                ReactionSelected?.Invoke(this, emojiName);
            }
        }
    }
}