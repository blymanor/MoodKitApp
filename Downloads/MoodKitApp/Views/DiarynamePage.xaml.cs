// MoodKitApp/Views/DiarynamePage.xaml.cs
// นี่คือไฟล์โค้ด C# ที่ทำงานคู่กับหน้าจอ DiarynamePage.xaml
// หน้าที่หลักของหน้านี้คือให้ผู้ใช้ตั้งชื่อไดอารี่ของตนเองหลังจากลงทะเบียนเสร็จ

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls; // Library หลักของ MAUI สำหรับสร้างหน้าจอและ Element ต่างๆ
using System;                   // Library พื้นฐานของ C# (เช่น สำหรับ EventArgs)
using System.Threading.Tasks;   // สำหรับการทำงานแบบ Asynchronous (เช่น ตอนนำทางไปหน้าอื่น)

// เพิ่ม using สำหรับ Preferences
using Microsoft.Maui.Storage; // สำหรับ Preferences

// *** ไม่จำเป็นต้องใช้ DbContext หรือ HomePage ที่นี่แล้วสำหรับการนำทางแบบ PopToRootAsync ***
// using MoodKitApp.Data;
// using MoodKitApp.Views; // ลบการ using Views.HomePage ถ้าไม่มีการอ้างอิงอื่นๆ

namespace MoodKitApp.Views // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Views
{
    // ประกาศคลาส DiarynamePage ซึ่งเป็นหน้าจอ (ContentPage)
    public partial class DiarynamePage : ContentPage
    {
        // ไม่จำเป็นต้องมี field สำหรับ DbContext หรือ HomePage Instance ถ้าใช้ PopToRootAsync
        // private readonly User _dbContext; // ลบบรรทัดนี้ถ้าไม่มีการใช้
        // private HomePage homePageInstance; // ลบบรรทัดนี้ถ้าไม่มีการใช้

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อหน้านี้ถูกสร้างขึ้น) ---
        // Constructor นี้อาจจะไม่ต้องรับ DbContext แล้ว ถ้าไม่ใช้ในการนำทางไป MainPage ด้วย Application.Current.MainPage
        // แต่ถ้า MainPage Constructor ยังรับ DbContext อยู่ และคุณต้องการใช้ Application.Current.MainPage = ...
        // คุณอาจจะต้องหาวิธีรับ DbContext มาในหน้านี้ หรือให้ MainPage Resolve เอง
        // *** สำหรับ PopToRootAsync() Constructor นี้ก็เพียงพอแล้ว ***
        public DiarynamePage()
        {
            InitializeComponent(); // โหลดส่วนประกอบ UI จากไฟล์ DiarynamePage.xaml (เช่น Entry, Button)
            // ถ้า MainPage Constructor รับ DbContext แต่ DiarynamePage Constructor ไม่ได้รับมา
            // และคุณอยากกลับไป MainPage ด้วย Application.Current.MainPage = new NavigationPage(new MainPage(...))
            // คุณจะต้องแก้ไข constructor นี้ด้วย
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Start Journey" ---
        // (ชื่อเมธอดนี้มาจาก x:Name ของปุ่มใน XAML และ Event 'Clicked')
        private async void StartJourneyButton_Clicked(System.Object sender, System.EventArgs e)
        {
            // 1. ตรวจสอบว่าผู้ใช้ได้กรอกชื่อไดอารี่หรือไม่
            if (string.IsNullOrWhiteSpace(DiaryNameEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a diary name.", "OK");
                return; // หยุดการทำงาน
            }

            // 2. ตรวจสอบว่าชื่อไดอารี่ที่กรอกมีความยาวอย่างน้อย 3 ตัวอักษรหรือไม่
            if (DiaryNameEntry.Text.Length < 3)
            {
                await DisplayAlert("Error", "Diary name must be at least 3 characters long.", "OK");
                return; // หยุดการทำงาน
            }

            // 3. บันทึกชื่อไดอารี่ที่ผู้ใช้กรอกลง Preferences
            Preferences.Set("DiaryName", DiaryNameEntry.Text.Trim()); // เพิ่ม .Trim() เพื่อตัดช่องว่างหน้า-หลัง

            // 4. นำทางกลับไปยังหน้าแรกสุด (Root Page) ของ Stack การนำทาง
            //    ซึ่งโดยทั่วไปคือ MainPage ในสถานการณ์นี้ (Signup -> Diaryname ถูก Push มาจาก MainPage)
            await Navigation.PopToRootAsync(); // <--- เปลี่ยนจาก PushAsync(HomePage) มาเป็น PopToRootAsync()

            // ถ้าคุณต้องการให้ MainPage เป็นหน้าหลักใหม่จริงๆ และลบ Stack เก่าทิ้งทั้งหมด
            // และ MainPage Constructor ยังรับ DbContext อยู่ และ DiarynamePage ไม่ได้รับ DbContext มา
            // อาจจะต้องพิจารณาการแก้ไข Dependency Injection หรือการนำทางแบบ Shell

            // *** การใช้ PopToRootAsync() เป็นวิธีที่ตรงไปตรงมาที่สุด ถ้า MainPage คือ Root เดิม ***
        }
    }
}