using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;

namespace MoodKitApp.Controls
{
    public partial class ReactionPicker : ContentView
    {
        private const double Radius = 90;
        private const double StartAngleDegrees = -90;
        private const double centerY = 0.4;

        private List<ImageButton> _emojiButtons;

        public event EventHandler<string>? ReactionSelected;
        public event EventHandler? CloseRequested;

        public ReactionPicker()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("ReactionPicker Constructor Called");

            _emojiButtons = new List<ImageButton>
            {
                Emoji1, Emoji2, Emoji3, Emoji4, Emoji5
            };

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