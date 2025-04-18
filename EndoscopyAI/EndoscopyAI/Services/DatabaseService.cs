using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EndoscopyAI.Services
{
    // 用于保存病人信息
    public class Patient
    {
        [Key]
        public string? NumberID { get; set; }        // 身份证号/医保号
        public string? Name { get; set; }            // 姓名
        public int? Age { get; set; }                // 年龄
        public string? Gender { get; set; }          // 性别
        public string? Contact { get; set; }         // 联系方式
        public string? DiagnosisResult { get; set; } // 诊断结果
        public double? ConfidenceLevel { get; set; } // 置信度

        // 默认构造函数
        public Patient()
        {
            this.Name = "Unkown";
            this.Age = 0;
            this.Gender = "Unkown";
            this.Contact = "Unkown";
            this.NumberID = "Unkown";
            this.DiagnosisResult = "Unkown";
            this.ConfidenceLevel = 0.0;
        }
    }

    // 数据库上下文，用于建立映射关系
    public class PatientDbContext : DbContext
    {
        public DbSet<Patient> Patients { get; set; } // 映射到数据库表

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // 使用 SQLite 数据库，自动创建数据库文件
            options.UseSqlite("Data Source=patients.db");
        }
    }

    // 数据库服务层
    public class PatientDbService
    {
        private readonly PatientDbContext _db;

        // 构造函数
        public PatientDbService(PatientDbContext db)
        {
            _db = db;
            _db.Database.EnsureCreated();  // 自动创建数据库和表
        }

        // 添加病人
        public void AddPatient(Patient patient)
        {
            try
            {
                _db.Patients.Add(patient);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // 根据身份证号或者医保号查找病人信息
        public Patient? FindPatientByIdNumber(Patient patient)
        {
            var existing = _db.Patients.Find(patient.NumberID);
            if (existing != null)
                return existing;
            else return null;
        }

        // 修改病人信息
        public void UpdatePatient(Patient updatedPatient)
        {
            try
            {
                var existing = FindPatientByIdNumber(updatedPatient);
                if (existing != null)
                {
                    existing.Name = updatedPatient.Name;
                    existing.Age = updatedPatient.Age;
                    existing.Gender = updatedPatient.Gender;
                    existing.Contact = updatedPatient.Contact;
                    existing.NumberID = updatedPatient.NumberID;
                    _db.SaveChanges();
                }
                else
                {
                    AddPatient(updatedPatient);
                }
            }
            catch (DbUpdateException ex)
            {
                // 捕获数据库更新异常
                Console.WriteLine($"数据库更新失败: {ex.InnerException?.Message}");
                throw;
            }
        }

        // 删除病人
        public void DeletePatient(int patientId)
        {
            var patient = _db.Patients.Find(patientId);
            if (patient != null)
            {
                _db.Patients.Remove(patient);
                _db.SaveChanges();
            }
        }

    }

    // 创建全局可访问的数据库服务实例
    public static class GlobalDbService
    {
        private static readonly PatientDbContext _dbContext;

        static GlobalDbService()
        {
            _dbContext = new PatientDbContext();
            _dbContext.Database.EnsureCreated();
        }

        public static PatientDbService DbService => new PatientDbService(_dbContext);
    }
}
