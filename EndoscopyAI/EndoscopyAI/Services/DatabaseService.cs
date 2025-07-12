using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
        // 基本信息
        public int ID { get; set; }  // 主键
        public int PatientID { get; set; }  // 病人ID
        public string NumberID { get; set; }        // 身份证号/医保号
        public string Name { get; set; }            // 姓名
        public int Age { get; set; }                // 年龄
        public string Gender { get; set; }          // 性别
        public string Contact { get; set; }         // 联系方式

        // 问诊信息
        public string MedicalDepartment { get; set; }  // 所属科室
        public string MedicalHistory { get; set; }  // 既往病史
        public string VisitTime { get; set; }  // 就诊时间
        public string VisitDate { get; set; }  // 就诊日期

        // 诊断记录
        public string ChiefComplaint { get; set; }  // 主诉
        public string ImagePath { get; set; }  // 图片路径*
        public string AIResult { get; set; }  // 智能诊断结果*
        public float AIConfidenceLevel { get; set; }  // AI置信度*
        public string DiagnosisResult { get; set; }  // 诊断结果
        public string TreatmentPlan { get; set; }  // 治疗方案
        
        // 默认构造函数
        public Patient() 
        { 
            ID = 0;
            PatientID = 0;
            NumberID = "";
            Name = "";
            Age = 0;
            Gender = "";
            Contact = "";
            MedicalDepartment = "消化内科";
            MedicalHistory = "";

            DateTime now = DateTime.Now;
            DateTime date = DateTime.Today;
            VisitTime = now.ToString("yyyy-MM-dd HH:mm:ss");
            VisitDate = date.ToString("yyyy-MM-dd");

            ChiefComplaint = "";
            ImagePath = "";
            AIResult = "";
            AIConfidenceLevel = 0;
            DiagnosisResult = "";
            TreatmentPlan = "";
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

    // 病人信息数据库服务层
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
                // 病人ID自动递增
                int maxId = _db.Patients.Any() ? _db.Patients.Max(p => p.ID) : 0;
                patient.ID = maxId + 1;
                int maxPatientId = _db.Patients.Any() ? _db.Patients.Max(p => p.PatientID) : 0;
                patient.PatientID = maxPatientId + 1;

                _db.Patients.Add(patient);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // 获取所有病人信息
        public List<Patient> GetPatients()
        {
            List<Patient> patients = _db.Patients.OrderBy(p => p.PatientID).ToList();
            return patients;
        }

        // 获取对应名字的所有病人信息
        public List<Patient>? GetPatientsByName(string name)
        {
            List<Patient> patients = _db.Patients.Where(p => p.Name == name).OrderBy(p => p.PatientID).ToList();
            return patients;
        }

        // 获取对应身份证号/医保号的所有病人信息
        public List<Patient>? GetPatientsByNumberID(string numberID)
        {
            List<Patient> patients = _db.Patients.Where(p => p.NumberID == numberID).OrderBy(p => p.PatientID).ToList();
            return patients;
        }

        // 根据身份证号/医保号获取病人信息
        public Patient? GetPatientByNumberID(string numberId)
        {
            var patient = _db.Patients.Where(p => p.NumberID == numberId).FirstOrDefault();
            return patient;
        }

        // 获取最近的病人信息
        public Patient? GetLatestPatient()
        {
            var patient = _db.Patients.OrderByDescending(p => p.PatientID).FirstOrDefault();
            return patient;
        }

        // 按照身份证号获取最近的病人信息
        public Patient? GetLatestPatient(string numberId)
        {
            var patient = _db.Patients.Where(p => p.NumberID == numberId).OrderByDescending(p => p.ID).FirstOrDefault();
            return patient;
        }

        // 修改病人信息
        public void UpdatePatient(Patient updatedPatient)
        {
            try
            {
                var existing = GetLatestPatient(updatedPatient.NumberID);
                if (existing != null)
                {
                    existing.MedicalHistory = updatedPatient.MedicalHistory;
                    existing.ChiefComplaint = updatedPatient.ChiefComplaint;
                    existing.ImagePath = updatedPatient.ImagePath;
                    existing.AIResult = updatedPatient.AIResult;
                    existing.AIConfidenceLevel = updatedPatient.AIConfidenceLevel;
                    existing.DiagnosisResult = updatedPatient.DiagnosisResult;
                    existing.TreatmentPlan = updatedPatient.TreatmentPlan;

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
        public void DeletePatient(int patientID)
        {
            var patient = _db.Patients.FirstOrDefault(p => p.PatientID == patientID);
            if (patient != null)
            {
                _db.Patients.Remove(patient);
                _db.SaveChanges();
                var patients = _db.Patients.OrderBy(p => p.PatientID).ToList();
                for (int i = 0; i < patients.Count; i++)
                {
                    patients[i].PatientID = i + 1;
                }
                _db.SaveChanges();
            }
        }

        // 删除所有病人信息
        public void DeleteAllPatients()
        {
            _db.Patients.RemoveRange(_db.Patients);
            _db.SaveChanges();
        }

        // 获取最近n天内每天的病人数量
        public List<int> GetPatientCountByDay(int days)
        {
            DateTime startDate = DateTime.Today.AddDays(-days + 1);
            var endDate = DateTime.Today;

            var dateList = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i).ToString("yyyy-MM-dd"))
                .ToList();

            // 先获取所有数据到内存中
            var patients = _db.Patients.AsEnumerable()
                .Where(p => {
                    var visitDate = DateTime.Parse(p.VisitDate);
                    return visitDate >= startDate && visitDate <= endDate;
                })
                .ToList();

            // 然后在内存中进行统计
            var dailyCounts = dateList
                .Select(date => patients.Count(p => p.VisitDate == date))
                .ToList();

            return dailyCounts;
        }

    }

    // 医生信息
    public class Doctor
    {
        [Key]
        public string jobNumber { get; set; }  // 工号
        public string name { get; set; }  // 姓名*
        public string password { get; set; }  // 密码

        Doctor()
        {
            jobNumber = "";
            name = "";
            password = "";
        }
    }

    // 数据库上下文，用于建立映射关系
    public class DoctorDbContext : DbContext
    {
        public DbSet<Doctor> Doctors { get; set; } // 映射到数据库表

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // 使用 SQLite 数据库，自动创建数据库文件
            options.UseSqlite("Data Source=doctors.db");
        }
    }

    // 医生信息数据库服务层
    public class DoctorDbService : DbContext
    {
        private readonly DoctorDbContext _db;

        // 构造函数
        public DoctorDbService(DoctorDbContext db)
        {
            _db = db;
            _db.Database.EnsureCreated();  // 自动创建数据库和表
        }

        // 添加医生
        public void AddDoctor(Doctor doctor)
        {
            try
            {
                _db.Doctors.Add(doctor);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // 检查密码是否正确
        public bool CheckPassword(string jobNumber, string password)
        {
            var doctor = _db.Doctors.FirstOrDefault(d => d.jobNumber == jobNumber);
            return doctor != null && doctor.password == password;
        }

        // 根据工号查找医生姓名
        public string? FindDoctorNameByJobNumber(string jobNumber)
        {
            var doctor = _db.Doctors.FirstOrDefault(d => d.jobNumber == jobNumber);
            return doctor?.name;
        }
    }

    // 创建全局可访问的数据库服务实例
    public static class GlobalDbService
    {
        private static readonly PatientDbContext _patientDbContext;
        private static readonly DoctorDbContext _doctorDbContext;

        static GlobalDbService()
        {
            _patientDbContext = new PatientDbContext();
            _patientDbContext.Database.EnsureCreated();
            _doctorDbContext = new DoctorDbContext();
            _doctorDbContext.Database.EnsureCreated();
        }

        public static PatientDbService PatientDbService => new PatientDbService(_patientDbContext);
        public static DoctorDbService DoctorDbService => new DoctorDbService(_doctorDbContext);
    }
}
