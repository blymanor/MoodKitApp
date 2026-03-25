using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
// using DotNet.Meteor.HotReload.Plugin; // ถ้าไม่ได้ใช้ ลบออกได้
using System.IO;
using MoodKitApp.Data;
using MoodKitApp.Views;
using Microsoft.EntityFrameworkCore;
using DotNet.Meteor.HotReload.Plugin;


namespace MoodKitApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("PlusJakartaSans.ttf", "PlusJakartaSans");
                fonts.AddFont("PoetsenOne.ttf", "PoetsenOne");
            });

#if DEBUG
        builder.EnableHotReload();
		builder.Logging.AddDebug();
#endif

        // --- ส่วนสำคัญสำหรับการตั้งค่า DbContext และ Database ---

        // 1. กำหนด Path และชื่อไฟล์ฐานข้อมูล SQLite
        // **เปลี่ยนชื่อไฟล์ฐานข้อมูลตรงนี้เพื่อให้สร้างไฟล์ใหม่**
        // string dbName = "MoodKitUserData.db"; // ชื่อเดิม
        string dbName = "MoodKitUserData_v4.db"; // <<<< ลองเปลี่ยนเป็นชื่อใหม่ เช่นเพิ่ม _v2 หรือวันที่
                                                 // หรือจะเปลี่ยน Path ไปยังโฟลเดอร์ย่อยก็ได้ถ้าต้องการ

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, dbName);

        System.Diagnostics.Debug.WriteLine($"[MauiProgram] Using Database Path: {dbPath}"); // เพิ่ม log เพื่อตรวจสอบ

        // 2. ลงทะเบียน DbContext (User) ของคุณกับ Dependency Injection
        builder.Services.AddDbContext<User>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // ลงทะเบียน Pages (เหมือนเดิม)
        builder.Services.AddTransient<SignupPage>();
        builder.Services.AddTransient<MainPage>();
        // (ถ้า MoodTrackerPage และ HomePage ยังไม่ได้ถูกสร้างผ่าน DI โดยตรง อาจจะต้องลงทะเบียนด้วย)
        builder.Services.AddTransient<MoodTrackerPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<DailyMoodTrackerPage>();


        var app = builder.Build();

        // 3. เรียก Database.EnsureCreated() (เหมือนเดิม)
        EnsureDatabaseCreated(app.Services);

        return app;
    }

    private static void EnsureDatabaseCreated(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<User>();
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureDatabaseCreated] Database path from DbContext: {dbContext.Database.GetDbConnection().ConnectionString}");
                bool created = dbContext.Database.EnsureCreated();
                System.Diagnostics.Debug.WriteLine(created ? "[EnsureDatabaseCreated] Database created successfully." : "[EnsureDatabaseCreated] Database already exists or no changes needed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureDatabaseCreated] An error occurred: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureDatabaseCreated] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}