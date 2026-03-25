// MoodKitApp/MainPage.xaml.cs
// นี่คือไฟล์โค้ด C# ที่ทำงานกับหน้าจอ MainPage.xaml
// หน้านี้เป็นหน้าแรกที่ผู้ใช้จะเห็นเมื่อเปิดแอป (ถ้ายังไม่ได้ Login)
// ผู้ใช้สามารถกรอก Username/Password เพื่อเข้าสู่ระบบ หรือกดปุ่มเพื่อไปหน้าลงทะเบียนได้

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;     // Library หลักของ MAUI สำหรับ UI
using MoodKitApp.Views;          // เรียกใช้หน้าจออื่นๆ เช่น HomePage และ SignupPage
using System;                       // Library พื้นฐาน C#
using System.Threading.Tasks;       // สำหรับการทำงานแบบ Asynchronous (ไม่ให้ UI ค้าง)
using MoodKitApp.Data;           // เรียกใช้ 'User' (ตัวจัดการฐานข้อมูล DbContext)
using MoodKitApp.Model;          // เรียกใช้ Model 'DataUser' (โครงสร้างข้อมูลผู้ใช้)
using Microsoft.EntityFrameworkCore; // Library สำหรับทำงานกับฐานข้อมูล (เช่น FindAsync)
using BCrypt.Net;                 // Library ภายนอกสำหรับ Hashing และ Verify รหัสผ่าน

namespace MoodKitApp // จัดกลุ่มไฟล์นี้ให้อยู่ใน Namespace หลักของโปรเจกต์
{
    // ประกาศคลาส MainPage ซึ่งเป็นหน้าจอ (ContentPage)
    public partial class MainPage : ContentPage
    {
        // _dbContext: ตัวแปรสำหรับเก็บ instance ของ User (DbContext) ซึ่งเป็นตัวเชื่อมต่อกับฐานข้อมูล
        private readonly User _dbContext;

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อหน้านี้ถูกสร้างขึ้น) ---
        // Constructor นี้รับ User (DbContext) เข้ามาผ่าน Dependency Injection (DI)
        // หมายความว่าตอนที่ MAUI สร้างหน้านี้ขึ้นมา มันจะหา DbContext ที่ลงทะเบียนไว้ใน MauiProgram.cs มาให้
        public MainPage(User dbContext)
        {
            InitializeComponent();  // โหลดส่วนประกอบ UI จากไฟล์ MainPage.xaml (เช่น ช่องกรอก Username, Password, ปุ่มต่างๆ)
            _dbContext = dbContext; // กำหนดค่า DbContext ที่ได้รับมา
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Sign In" ---
        private async void SignInButton_Clicked(object sender, EventArgs e)
        {
            // ดึงข้อความที่ผู้ใช้กรอกในช่อง Username และ Password
            // .Trim() เพื่อตัดช่องว่างหน้า-หลังออก
            // ?? string.Empty เพื่อป้องกันกรณีที่ Text เป็น null (ให้เป็น string ว่างแทน)
            string enteredUsername = UsernameEntry.Text?.Trim() ?? string.Empty;
            string enteredPassword = PasswordEntry.Text ?? string.Empty;

            // ตรวจสอบว่าผู้ใช้กรอกข้อมูลครบทั้งสองช่องหรือไม่
            if (string.IsNullOrWhiteSpace(enteredUsername) || string.IsNullOrWhiteSpace(enteredPassword))
            {
                // ถ้าไม่ครบ ให้แสดง Alert (Pop-up) แจ้งเตือน
                await DisplayAlert("Missing Information", "Please enter username and password.", "OK"); // <-- แก้ไขตรงนี้
                return; // หยุดการทำงานของเมธอดนี้
            }

            // (เสริม) ส่วนนี้ Comment ไว้ว่าอาจจะแสดง ActivityIndicator (วงกลมหมุนๆ)
            // หรือทำให้ปุ่ม Sign In กดไม่ได้ชั่วคราว ขณะกำลังตรวจสอบข้อมูล
            // LoadingIndicator.IsVisible = true;
            // SignInButton.IsEnabled = false;

            // --- ส่วนที่เริ่มทำงานกับฐานข้อมูลและตรวจสอบรหัสผ่าน (ครอบด้วย try-catch) ---
            try
            {
                // ค้นหาผู้ใช้ในฐานข้อมูลจาก "ชื่อผู้ใช้" ที่กรอกเข้ามา
                // _dbContext.Users คือตาราง Users ในฐานข้อมูล
                // .FindAsync(enteredUsername) เป็นการค้นหาข้อมูลโดยใช้ Primary Key (ซึ่งในที่นี้คือ UserName)
                DataUser? userInDb = await _dbContext.Users.FindAsync(enteredUsername);

                if (userInDb != null) // ถ้าเจอผู้ใช้ที่มีชื่อนี้ในระบบ
                {
                    // --- พบ User -> ตรวจสอบรหัสผ่าน ---
                    // userInDb.Password ที่เก็บในฐานข้อมูลควรจะเป็น "Hashed Password" (รหัสผ่านที่เข้ารหัสแล้ว)
                    bool isPasswordCorrect = false; // ตัวแปรเก็บผลการตรวจสอบรหัสผ่าน
                    try
                    {
                        // ใช้ BCrypt.Net.BCrypt.Verify เพื่อเปรียบเทียบรหัสผ่านที่ผู้ใช้กรอก (enteredPassword)
                        // กับ Hashed Password ที่เก็บไว้ในฐานข้อมูล (userInDb.Password)
                        // Task.Run(...) ใช้เพื่อให้การ Verify (ซึ่งอาจใช้เวลาเล็กน้อย) ทำงานบน Background Thread ไม่ให้ UI ค้าง
                        isPasswordCorrect = await Task.Run(() => BCrypt.Net.BCrypt.Verify(enteredPassword, userInDb.Password));
                    }
                    catch (BCrypt.Net.SaltParseException ex) // ดักจับกรณีรูปแบบ Hashed Password ใน DB ไม่ถูกต้อง
                    {
                        System.Diagnostics.Debug.WriteLine($"BCrypt SaltParseException: {ex.Message}");
                        await DisplayAlert("Error", "Incorrect password format stored. Please contact administrator.", "OK"); // <-- แก้ไขตรงนี้
                        return; // หยุดการทำงาน เพราะข้อมูลใน DB อาจมีปัญหา
                    }
                    catch (Exception ex) // ดักจับข้อผิดพลาดอื่นๆ ที่อาจเกิดจาก BCrypt.Verify
                    {
                        System.Diagnostics.Debug.WriteLine($"BCrypt.Verify Exception: {ex.Message}");
                        await DisplayAlert("Error", "Unable to verify password at this time.", "OK"); // <-- แก้ไขตรงนี้
                        return; // หยุดการทำงาน
                    }

                    if (isPasswordCorrect) // ถ้ารหัสผ่านถูกต้อง
                    {
                        // --- เข้าสู่ระบบสำเร็จ ---
                        // เก็บชื่อผู้ใช้ที่ Login สำเร็จไว้ใน Preferences
                        // เพื่อให้หน้าอื่นๆ ในแอปทราบว่าใครกำลังใช้งานอยู่
                        Preferences.Set("LoggedInUserName", userInDb.UserName);
                        // (อาจจะเก็บข้อมูลอื่นๆ เช่น Email หรือ DiaryName ถ้าจำเป็น)
                        // Preferences.Set("UserEmail", userInDb.Email);
                        // Preferences.Set("DiaryName", ...); // ถ้าชื่อไดอารี่ผูกกับ User โดยตรง อาจจะต้องโหลด/ตั้งค่าตรงนี้

                        await DisplayAlert("Login Successful", $"Welcome, {userInDb.UserName}!", "OK"); // <-- แก้ไขตรงนี้
                        // นำทางผู้ใช้ไปยังหน้า HomePage
                        // (ถ้า HomePage มี Constructor ที่รับ dependencies และไม่ได้ Resolve เอง, ต้องส่งค่าที่จำเป็นไปด้วย)
                        // แต่จากโค้ด HomePage ล่าสุด มัน resolve DbContext และดึง UserName จาก Preferences เอง
                        // await Navigation.PushAsync(new HomePage());

                        // **ปรับปรุงการนำทาง: ตั้ง HomePage เป็นหน้าหลักใหม่ เพื่อไม่ให้ผู้ใช้กด Back กลับมาหน้า Login ได้**
                        if (Application.Current != null)
                        {
                            // ถ้า HomePage มีการลงทะเบียน DI และสามารถ Resolve Dependencies เองได้:
                            var homePageInstance = Application.Current.Handler?.MauiContext?.Services.GetService<HomePage>();
                            if (homePageInstance != null)
                            {
                                Application.Current.MainPage = new NavigationPage(homePageInstance);
                            }
                            else
                            {
                                // Fallback ถ้า GetService คืนค่า null (ไม่ควรเกิดถ้า DI ถูกต้อง)
                                Application.Current.MainPage = new NavigationPage(new HomePage());
                            }
                        }


                        // (ทางเลือก) ล้างค่าในช่องกรอก Username และ Password หลังจาก Login สำเร็จ
                        UsernameEntry.Text = string.Empty;
                        PasswordEntry.Text = string.Empty;
                    }
                    else // ถ้ารหัสผ่านไม่ถูกต้อง
                    {
                        await DisplayAlert("Login Failed", "Incorrect password.", "OK"); // <-- แก้ไขตรงนี้
                        PasswordEntry.Text = string.Empty; // ล้างเฉพาะช่องรหัสผ่าน
                        PasswordEntry.Focus();             // ให้ Cursor ไปอยู่ที่ช่องรหัสผ่าน
                    }
                }
                else // ถ้าไม่พบชื่อผู้ใช้นี้ในระบบ
                {
                    await DisplayAlert("Login Failed", "User not found.", "OK"); // <-- แก้ไขตรงนี้
                    UsernameEntry.Focus(); // ให้ Cursor ไปอยู่ที่ช่อง Username
                }
            }
            catch (Exception ex) // ดักจับข้อผิดพลาดอื่นๆ ที่ไม่คาดคิด
            {
                System.Diagnostics.Debug.WriteLine($"Generic Exception: {ex.Message}");
                await DisplayAlert("Unexpected Error", "An unexpected error occurred. Please try again.", "OK"); // <-- แก้ไขตรงนี้
            }
            finally // ส่วนนี้จะทำงานเสมอ ไม่ว่า try จะสำเร็จหรือเกิด Exception
            {
                // (เสริม) ถ้ามีการแสดง ActivityIndicator หรือ disable ปุ่มไว้ ก็ให้คืนค่ากลับตรงนี้
                // LoadingIndicator.IsVisible = false;
                // SignInButton.IsEnabled = true;
            }
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Sign Up" ---
        private async void SignUpButton_Clicked(object sender, EventArgs e)
        {
            // แสดง Alert ถามผู้ใช้ว่าต้องการสร้างบัญชีใหม่หรือไม่
            bool isSignUp = await DisplayAlert("Sign Up", // <-- แก้ไขตรงนี้
                                             "Do you want to create a new user account?", // <-- แก้ไขตรงนี้
                                             "Yes", "No"); // <-- แก้ไขตรงนี้

            if (isSignUp) // ถ้าผู้ใช้กด "ใช่"
            {
                try
                {
                    // นำทางไปยังหน้า SignupPage
                    // โดยส่ง _dbContext (ตัวเชื่อมฐานข้อมูล) ไปให้ SignupPage ด้วย (เพราะ Constructor ของ SignupPage รับค่านี้)
                    await Navigation.PushAsync(new SignupPage(_dbContext));

                    // ส่วนที่ Comment ไว้เป็นอีกวิธีในการสร้าง Instance ของ SignupPage
                    // ถ้า SignupPage ถูกลงทะเบียนใน Dependency Injection และสามารถ Resolve Dependencies ของตัวเองได้
                }
                catch (Exception ex) // ถ้ามีปัญหาตอนเปิดหน้า SignupPage
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation to SignupPage Error: {ex.Message}");
                    await DisplayAlert("Error", "Unable to open registration page.", "OK"); // <-- แก้ไขตรงนี้
                }
            }
        }
    }
}