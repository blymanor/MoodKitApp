// MoodKitApp/Data/User.cs
// ไฟล์นี้เปรียบเสมือน "ผู้จัดการฐานข้อมูล" ของแอปพลิเคชัน
// มันใช้ Entity Framework Core (EF Core) ซึ่งเป็นเครื่องมือที่ช่วยให้เราคุยกับฐานข้อมูลได้ง่ายๆ ด้วย C#

// --- ส่วนที่โปรแกรมเรียกใช้ Library ต่างๆ ---
using Microsoft.EntityFrameworkCore; // Library หลักของ EF Core
using MoodKitApp.Model;              // เรียกใช้ Model 'DataUser' (โครงสร้างข้อมูลผู้ใช้)
using MoodKitApp.Models;             // **เพิ่ม:** เรียกใช้ Model 'MoodRecord' (โครงสร้างข้อมูลบันทึกอารมณ์)

namespace MoodKitApp.Data // จัดกลุ่มไฟล์นี้ให้อยู่ในหมวด Data
{
    // ประกาศคลาส User ซึ่งสืบทอดมาจาก DbContext
    // DbContext เป็นคลาสหลักของ EF Core ที่ทำหน้าที่เป็นตัวแทนของ Session การเชื่อมต่อกับฐานข้อมูล
    // และให้เราสามารถ Query (ค้นหา) และ Save (บันทึก) ข้อมูลได้
    // (หมายเหตุ: ชื่อคลาส 'User' สำหรับ DbContext อาจจะทำให้สับสนกับ Model 'DataUser' ได้
    //  ในโปรเจกต์ที่ใหญ่ขึ้น อาจจะตั้งชื่อเป็น MoodKitDbContext หรือ AppDbContext จะสื่อความหมายกว่า)
    public class User : DbContext
    {
        // --- Constructor (เมธอดที่ถูกเรียกเมื่อมีการสร้าง Object User (DbContext) นี้ขึ้นมา) ---
        // Constructor นี้รับ 'options' ซึ่งเป็นการตั้งค่าต่างๆ สำหรับ DbContext (เช่น จะใช้ฐานข้อมูลอะไร, Connection String คืออะไร)
        // ค่า options นี้จะถูกส่งมาจากตอนที่เราลงทะเบียน DbContext ใน MauiProgram.cs
        public User(DbContextOptions<User> options) : base(options)
        {
            // base(options) คือการส่ง options นี้ไปให้ Constructor ของคลาสแม่ (DbContext) จัดการต่อ
        }

        // --- DbSet (ตัวแทนของตารางในฐานข้อมูล) ---
        // DbSet<T> เปรียบเสมือน "ประตู" ที่เปิดเข้าไปยังตารางข้อมูลชนิด T ในฐานข้อมูล
        // เราจะใช้ DbSet เหล่านี้ในการเพิ่ม, ลบ, แก้ไข, หรือค้นหาข้อมูลในตารางนั้นๆ

        // Users: เป็น DbSet สำหรับจัดการข้อมูลในตาราง "Users" (ซึ่งเก็บข้อมูล DataUser)
        public DbSet<DataUser> Users { get; set; }
        // MoodRecords: เป็น DbSet สำหรับจัดการข้อมูลในตาราง "MoodRecords" (ซึ่งเก็บข้อมูล MoodRecord)
        public DbSet<MoodRecord> MoodRecords { get; set; } // **เพิ่ม DbSet สำหรับ MoodRecord**

        // --- OnModelCreating (เมธอดสำหรับตั้งค่า Model และความสัมพันธ์ต่างๆ ของตาราง) ---
        // เมธอดนี้จะถูกเรียกโดย EF Core ตอนที่มันกำลังสร้าง Model ของฐานข้อมูลเป็นครั้งแรก
        // เราใช้เมธอดนี้เพื่อกำหนดรายละเอียดต่างๆ ของตารางและคอลัมน์ (เช่น ชื่อตาราง, Primary Key, Foreign Key, Index, ข้อจำกัดต่างๆ)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- การตั้งค่าสำหรับ Entity (ตาราง) DataUser ---
            modelBuilder.Entity<DataUser>()          // บอก EF Core ว่าเรากำลังจะตั้งค่าสำหรับ Model DataUser
                .ToTable("Users");                   // กำหนดให้ตารางในฐานข้อมูลที่จะเก็บ DataUser ชื่อว่า "Users"

            modelBuilder.Entity<DataUser>()
                .HasKey(u => u.UserName);            // กำหนดให้ Property 'UserName' เป็น Primary Key (กุญแจหลัก) ของตาราง Users
                                                     // (หมายความว่า UserName ของแต่ละคนต้องไม่ซ้ำกัน)

            modelBuilder.Entity<DataUser>()
                .Property(u => u.Password).IsRequired(); // กำหนดให้คอลัมน์ Password ต้องมีข้อมูลเสมอ (ห้ามเป็น null)

            modelBuilder.Entity<DataUser>()
                .Property(u => u.Email).IsRequired();    // กำหนดให้คอลัมน์ Email ต้องมีข้อมูลเสมอ

            // (แนะนำ) สร้าง Index (ดัชนี) ให้กับคอลัมน์ Email และกำหนดให้เป็น Unique (ห้ามซ้ำ)
            // Index ช่วยให้การค้นหาข้อมูลด้วย Email เร็วขึ้น
            // IsUnique() ช่วยป้องกันไม่ให้มีผู้ใช้หลายคนใช้อีเมลเดียวกันลงทะเบียน
            modelBuilder.Entity<DataUser>()
                .HasIndex(u => u.Email).IsUnique();


            // --- การตั้งค่าสำหรับ Entity (ตาราง) MoodRecord ---
            modelBuilder.Entity<MoodRecord>()         // บอก EF Core ว่าเรากำลังจะตั้งค่าสำหรับ Model MoodRecord
                .ToTable("MoodRecords");              // กำหนดให้ตารางในฐานข้อมูลที่จะเก็บ MoodRecord ชื่อว่า "MoodRecords"

            modelBuilder.Entity<MoodRecord>()
                .HasKey(mr => mr.MoodRecordId);       // กำหนดให้ Property 'MoodRecordId' เป็น Primary Key ของตาราง MoodRecords



            // --- กำหนดคุณสมบัติเพิ่มเติมให้กับคอลัมน์ในตาราง MoodRecords ---
            modelBuilder.Entity<MoodRecord>()
                .Property(mr => mr.MoodEmojiSource).IsRequired(); // คอลัมน์ MoodEmojiSource ต้องมีข้อมูลเสมอ

            modelBuilder.Entity<MoodRecord>()
                .Property(mr => mr.FeelingLabel).HasMaxLength(100); // คอลัมน์ FeelingLabel เก็บข้อความได้สูงสุด 100 ตัวอักษร

            modelBuilder.Entity<MoodRecord>()
                .Property(mr => mr.Description).HasMaxLength(1000); // คอลัมน์ Description เก็บข้อความได้สูงสุด 1000 ตัวอักษร

            modelBuilder.Entity<MoodRecord>()
                .Property(mr => mr.ImagePath).HasMaxLength(500);   // คอลัมน์ ImagePath เก็บข้อความ (ที่อยู่ไฟล์รูป) ได้สูงสุด 500 ตัวอักษร
        }
    }
}