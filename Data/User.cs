using Microsoft.EntityFrameworkCore;
using MoodKitApp.Model;
using MoodKitApp.Models;

namespace MoodKitApp.Data
{
    public class User : DbContext
    {
        public User(DbContextOptions<User> options) : base(options)
        {
        }

        public DbSet<DataUser> Users { get; set; }
        public DbSet<MoodRecord> MoodRecords { get; set; }

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