// MoodKitApp/Views/SignupPage.xaml.cs
// นี่คือไฟล์โค้ด C# ที่ทำงานคู่กับหน้าจอ SignupPage.xaml
// หน้าที่หลักของหน้านี้คือให้ผู้ใช้ใหม่สามารถสร้างบัญชีเพื่อเข้าใช้งานแอปพลิเคชันได้

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.Maui.Controls;         // Library หลักของ MAUI สำหรับ UI (เช่น ContentPage, Button, Entry)
using System;                           // Library พื้นฐาน C# (เช่น EventArgs, String, Exception)
using System.Net.Mail;                // Library สำหรับทำงานกับ Email Address (เช่น ตรวจสอบรูปแบบอีเมล)
using System.Threading.Tasks;         // สำหรับการทำงานแบบ Asynchronous (เช่น await DisplayAlert, await _dbContext.SaveChangesAsync())
using Microsoft.EntityFrameworkCore;    // Library สำหรับทำงานกับฐานข้อมูล (เช่น DbUpdateException, AnyAsync)
using BCrypt.Net;                     // Library ภายนอกสำหรับ Hashing รหัสผ่าน (เพิ่มความปลอดภัย)
using MoodKitApp.Model;              // เรียกใช้ Model 'DataUser' (โครงสร้างข้อมูลผู้ใช้)
using MoodKitApp.Data;               // เรียกใช้ 'User' (ตัวจัดการฐานข้อมูล DbContext)

namespace MoodKitApp.Views // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Views
{
    // ประกาศคลาส SignupPage ซึ่งเป็นหน้าจอ (ContentPage)
    public partial class SignupPage : ContentPage
    {
        // _dbContext: ตัวแปรสำหรับเก็บ instance ของ User (DbContext) ซึ่งเป็นตัวเชื่อมต่อกับฐานข้อมูล
        // readonly หมายถึงค่านี้จะถูกกำหนดแค่ครั้งเดียวตอนสร้าง Object (ใน Constructor)
        private readonly User _dbContext;

        // --- Constructor (เมธอดที่ถูกเรียกเมื่อหน้านี้ถูกสร้างขึ้น) ---
        // Constructor นี้รับ User (DbContext) เข้ามาผ่าน Dependency Injection (DI)
        // หมายความว่าตอนที่ MAUI สร้างหน้านี้ขึ้นมา มันจะหา DbContext ที่ลงทะเบียนไว้ใน MauiProgram.cs มาให้โดยอัตโนมัติ
        public SignupPage(User dbContext)
        {
            InitializeComponent();  // โหลดส่วนประกอบ UI จากไฟล์ SignupPage.xaml
            _dbContext = dbContext; // กำหนดค่า DbContext ที่ได้รับมาให้กับตัวแปร _dbContext ของคลาสนี้
        }

        // --- เมธอดที่ทำงานเมื่อผู้ใช้คลิกปุ่ม "Done" (หรือปุ่มลงทะเบียน) ---
        // (ชื่อเมธอดนี้มาจาก x:Name ของปุ่มใน XAML และ Event 'Clicked')
        private async void DoneButton_Clicked(object sender, EventArgs e)
        {
            // --- 1. ตรวจสอบข้อมูลเบื้องต้น (Input Validation) ---
            // เช็คว่าช่องกรอก Username, Email, Password, Confirm Password มีข้อความหรือไม่
            // string.IsNullOrWhiteSpace(...) จะเป็น true ถ้าช่องนั้นว่างเปล่า หรือมีแต่ spacebar
            if (string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                // ถ้ามีช่องใดช่องหนึ่งว่าง ให้แสดง Alert (Pop-up) แจ้งเตือน
                await DisplayAlert("Missing Information", "Please fill in all fields.", "OK"); // <-- แก้ไขตรงนี้
                return; // หยุดการทำงานของเมธอดนี้ ไม่ทำขั้นตอนต่อไป
            }

            // เช็คว่า Password กับ Confirm Password ที่ผู้ใช้กรอก ตรงกันหรือไม่
            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await DisplayAlert("Passwords Do Not Match", "Password and confirm password do not match.", "OK"); // <-- แก้ไขตรงนี้
                PasswordEntry.Text = string.Empty;          // ล้างช่อง Password
                ConfirmPasswordEntry.Text = string.Empty;   // ล้างช่อง Confirm Password
                PasswordEntry.Focus();                      // ให้ Cursor ไปอยู่ที่ช่อง Password เพื่อให้ผู้ใช้กรอกใหม่
                return; // หยุดการทำงาน
            }

            // --- 2. ตรวจสอบรูปแบบอีเมล ---
            try
            {
                var trimmedEmail = EmailEntry.Text.Trim(); // .Trim() เพื่อตัดช่องว่างหน้า-หลังอีเมล
                var mailAddress = new MailAddress(trimmedEmail); // ลองสร้าง Object MailAddress จากอีเมลที่กรอก
                                                                 // ถ้าอีเมลรูปแบบไม่ถูกต้อง ตรงนี้จะเกิด FormatException
                if (mailAddress.Address != trimmedEmail) // เช็คอีกครั้งว่าหลังจาก MailAddress จัดการแล้ว มันยังเหมือนเดิมไหม
                {
                    // (ส่วนนี้อาจจะไม่ค่อยจำเป็นถ้า MailAddress(trimmedEmail) ผ่านแล้ว)
                    throw new FormatException("Invalid email format character"); // <-- แก้ไขข้อความ Error ภายใน (ถ้ามี)
                }
            }
            catch (FormatException ex) // ถ้าเกิด FormatException (รูปแบบอีเมลผิด)
            {
                System.Diagnostics.Debug.WriteLine($"Email Format Error: {ex.Message}"); // Log Error สำหรับ Debug
                await DisplayAlert("Invalid Email Format", "Please enter a valid email address.", "OK"); // <-- แก้ไขตรงนี้
                EmailEntry.Focus(); // ให้ Cursor ไปอยู่ที่ช่อง Email
                return; // หยุดการทำงาน
            }

            // --- 3. ตรวจสอบความยาว/รูปแบบของชื่อผู้ใช้ (Username) ---
            var trimmedUsername = UsernameEntry.Text.Trim(); // ตัดช่องว่างหน้า-หลังชื่อผู้ใช้
            if (trimmedUsername.Length < 3) // เช็คว่าชื่อผู้ใช้สั้นกว่า 3 ตัวอักษรหรือไม่
            {
                await DisplayAlert("Username Too Short", "Username must be at least 3 characters long.", "OK"); // <-- แก้ไขตรงนี้
                UsernameEntry.Focus(); // ให้ Cursor ไปอยู่ที่ช่อง Username
                return; // หยุดการทำงาน
            }
            // (ตรงนี้สามารถเพิ่มการตรวจสอบอื่นๆ สำหรับชื่อผู้ใช้ได้ เช่น ห้ามมีอักขระพิเศษ)

            // --- 4. ตรวจสอบความซับซ้อนของรหัสผ่าน (Password) ---
            if (PasswordEntry.Text.Length < 6) // เช็คว่ารหัสผ่านสั้นกว่า 6 ตัวอักษรหรือไม่
            {
                await DisplayAlert("Password Too Short", "Password must be at least 6 characters long.", "OK"); // <-- แก้ไขตรงนี้
                PasswordEntry.Text = string.Empty;          // ล้างช่อง Password
                ConfirmPasswordEntry.Text = string.Empty;   // ล้างช่อง Confirm Password
                PasswordEntry.Focus();                      // ให้ Cursor ไปอยู่ที่ช่อง Password
                return; // หยุดการทำงาน
            }
            // (ตรงนี้สามารถเพิ่มการตรวจสอบอื่นๆ สำหรับรหัสผ่านได้ เช่น ต้องมีตัวพิมพ์ใหญ่/เล็ก/ตัวเลข/อักขระพิเศษ)

            // (เสริม) ส่วนนี้เป็นการ Comment ไว้ว่าอาจจะแสดง ActivityIndicator (วงกลมหมุนๆ)
            // หรือทำให้ปุ่ม "Done" กดไม่ได้ชั่วคราว ขณะกำลังประมวลผล
            // LoadingIndicator.IsVisible = true;
            // DoneButton.IsEnabled = false;

            // --- ส่วนที่เริ่มทำงานกับฐานข้อมูล (ครอบด้วย try-catch เพื่อดักจับข้อผิดพลาด) ---
            try
            {
                // 5. ตรวจสอบว่าชื่อผู้ใช้ (UserName) นี้มีคนใช้แล้วหรือยังในฐานข้อมูล
                //    _dbContext.Users คือตาราง Users ในฐานข้อมูล
                //    .AnyAsync(...) เป็นการเช็คว่า "มีอย่างน้อยหนึ่งรายการหรือไม่" ที่ตรงตามเงื่อนไข
                //    u => u.UserName == trimmedUsername คือเงื่อนไขว่า UserName ในฐานข้อมูล ตรงกับชื่อที่ผู้ใช้กรอกมา
                bool usernameExists = await _dbContext.Users.AnyAsync(u => u.UserName == trimmedUsername);
                if (usernameExists) // ถ้าชื่อผู้ใช้นี้มีคนใช้แล้ว
                {
                    await DisplayAlert("Username Taken", "This username is already taken. Please choose another.", "OK"); // <-- แก้ไขตรงนี้
                    UsernameEntry.Focus(); // ให้ Cursor ไปอยู่ที่ช่อง Username
                    return; // หยุดการทำงาน
                }

                // 6. ตรวจสอบว่าอีเมล (Email) นี้มีคนใช้แล้วหรือยัง (ถ้าอีเมลต้องไม่ซ้ำกันในระบบ)
                var trimmedEmailForDb = EmailEntry.Text.Trim(); // อีเมลที่ตัดช่องว่างแล้วสำหรับเช็คใน DB
                bool emailExists = await _dbContext.Users.AnyAsync(u => u.Email == trimmedEmailForDb);
                if (emailExists) // ถ้าอีเมลนี้มีคนใช้แล้ว
                {
                    await DisplayAlert("Email Taken", "This email is already registered. Please use another email.", "OK"); // <-- แก้ไขตรงนี้
                    EmailEntry.Focus(); // ให้ Cursor ไปอยู่ที่ช่อง Email
                    return; // หยุดการทำงาน
                }

                // 7. Hashing รหัสผ่าน (ทำให้รหัสผ่านปลอดภัย ไม่เก็บเป็นข้อความธรรมดา)
                //    BCrypt.Net.BCrypt.HashPassword(...) เป็นการนำรหัสผ่านที่ผู้ใช้กรอก (PasswordEntry.Text)
                //    ไปผ่านกระบวนการ Hash เพื่อให้ได้ค่าที่เข้ารหัสแล้ว
                //    Task.Run(...) ใช้เพื่อให้การ Hashing (ซึ่งอาจจะใช้เวลาเล็กน้อย) ไปทำงานบน Background Thread
                //    เพื่อไม่ให้ UI (หน้าจอ) ของแอปค้าง
                string passwordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(PasswordEntry.Text));

                // 8. สร้าง Object (ข้อมูล) ของผู้ใช้ใหม่ เพื่อเตรียมบันทึกลงฐานข้อมูล
                var newUser = new DataUser // DataUser คือ Model (โครงสร้างข้อมูล) ของผู้ใช้
                {
                    UserName = trimmedUsername,     // ชื่อผู้ใช้ที่ผ่านการตรวจสอบแล้ว
                    Password = passwordHash,        // **สำคัญมาก: เก็บ Hashed Password (รหัสผ่านที่เข้ารหัสแล้ว) เท่านั้น**
                    Email = trimmedEmailForDb       // อีเมลที่ผ่านการตรวจสอบแล้ว
                    // (ถ้ามี Property อื่นๆ เช่น วันที่สร้างบัญชี ก็สามารถกำหนดค่าตรงนี้ได้)
                    // CreatedDate = DateTime.UtcNow;
                };

                // 9. เพิ่มข้อมูลผู้ใช้ใหม่ (`newUser`) เข้าไปใน DbContext (เหมือนเป็นการเตรียมข้อมูลใส่ตะกร้า)
                _dbContext.Users.Add(newUser);
                // สั่งให้ DbContext บันทึกการเปลี่ยนแปลงทั้งหมด (ในที่นี้คือการเพิ่ม newUser) ลงในฐานข้อมูลจริง
                // recordsAffected จะเก็บจำนวนแถวข้อมูลที่ได้รับผลกระทบ (ในที่นี้ควรจะเป็น 1 ถ้าเพิ่มสำเร็จ)
                int recordsAffected = await _dbContext.SaveChangesAsync();

                if (recordsAffected > 0) // ถ้าการบันทึกสำเร็จ (มีข้อมูลถูกเพิ่มเข้าไปในฐานข้อมูล)
                {
                    await DisplayAlert("Registration Successful", "New user registration completed successfully!", "OK"); // <-- แก้ไขตรงนี้

                    // (ทางเลือก) ล้างข้อมูลในช่องกรอกต่างๆ หลังจากลงทะเบียนสำเร็จ
                    UsernameEntry.Text = string.Empty;
                    EmailEntry.Text = string.Empty;
                    PasswordEntry.Text = string.Empty;
                    ConfirmPasswordEntry.Text = string.Empty;

                    // (สำคัญ) เก็บชื่อผู้ใช้ที่ลงทะเบียนสำเร็จไว้ใน Preferences
                    // เพื่อให้หน้าอื่นๆ รู้ว่าใครกำลัง Login อยู่
                    Preferences.Set("LoggedInUserName", newUser.UserName);


                    // 10. นำทางผู้ใช้ไปยังหน้า DiarynamePage (เพื่อให้ผู้ใช้ตั้งชื่อไดอารี่)
                    await Navigation.PushAsync(new DiarynamePage());
                }
                else // ถ้าการบันทึกล้มเหลว (ไม่มีข้อมูลถูกเพิ่ม หรือ recordsAffected เป็น 0)
                {
                     await DisplayAlert("Registration Failed", "Unable to save user data. Please try again.", "OK"); // <-- แก้ไขตรงนี้ (ข้อความสั้นลง)
                }
            }
            catch (DbUpdateException dbEx) // ดักจับข้อผิดพลาดที่เกิดจากฐานข้อมูลโดยเฉพาะ (เช่น UNIQUE constraint error)
            {
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                // InnerException มักจะมีรายละเอียดของปัญหาที่แท้จริงจาก Database Provider
                await DisplayAlert("Database Error", "This username or email may already exist, or there was a problem saving. Please try again.", "OK"); // <-- แก้ไขตรงนี้
            }
            catch (Exception ex) // ดักจับข้อผิดพลาดอื่นๆ ที่อาจจะเกิดขึ้นโดยไม่คาดคิด
            {
                System.Diagnostics.Debug.WriteLine($"Generic Exception: {ex.Message}");
                await DisplayAlert("Unexpected Error", $"An unexpected error occurred: {ex.Message}", "OK"); // <-- แก้ไขตรงนี้
            }
            finally // ส่วนนี้จะทำงานเสมอ ไม่ว่า try จะสำเร็จหรือเกิด Exception
            {
                // (เสริม) ถ้ามีการแสดง ActivityIndicator หรือ disable ปุ่มไว้ ก็ให้คืนค่ากลับตรงนี้
                // LoadingIndicator.IsVisible = false;
                // DoneButton.IsEnabled = true;
            }
        }
    }
}