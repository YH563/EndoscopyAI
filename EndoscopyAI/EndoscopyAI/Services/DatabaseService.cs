using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EndoscopyAI.Services
{
    // 用于保存病人信息
    public class Patient
    {
        public int PatientId { get; set; } // 主键
        public required string Name { get; set; }    // 姓名
        public int Age { get; set; }        // 年龄
        public required string Gender { get; set; }  // 性别
        public required string Contact { get; set; } // 联系方式
        public required string IDNumber { get; set; }// 身份证号/医保号
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
            _db = new PatientDbContext();
            _db.Database.EnsureCreated();  // 自动创建数据库和表
        }

        // 添加病人
        public void AddPatient(Patient patient)
        {
            _db.Patients.Add(patient);
            _db.SaveChanges();
        }

        // 修改病人信息
        public void UpdatePatient(Patient updatedPatient)
        {
            var existing = _db.Patients.Find(updatedPatient.PatientId);
            if (existing != null)
            {
                existing.Name = updatedPatient.Name;
                existing.Age = updatedPatient.Age;
                existing.Gender = updatedPatient.Gender;
                existing.Contact = updatedPatient.Contact;
                existing.IDNumber = updatedPatient.IDNumber;
                _db.SaveChanges();
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

        // 查找所有病人
        public List<Patient> GetAllPatients()
        {
            return _db.Patients.ToList();
        }

        // 根据姓名查找
        public List<Patient> SearchByName(string name)
        {
            return _db.Patients.Where(p => p.Name.Contains(name)).ToList();
        }

        // 根据身份证号查找
        public Patient GetByIdNumber(string idNumber)
        {
            return _db.Patients.FirstOrDefault(p => p.IDNumber == idNumber);
        }
    }
}
