using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EndoscopyAI.ViewModels.SubViewModels;
using EndoscopyAI.Services;
using System.Runtime.ExceptionServices;
using System.Diagnostics;

namespace EndoscopyAI.Tests
{
    class PatientDataGeneration
    {
        // 用于添加，以及删除患者信息
        private IPatientInformation patientInformation = new PatientInformation();

        public void RandomGeneratePatientData()
        {
            // 随机生成患者信息
            int nums = 100;
            for (int i = 0; i < nums; i++)
            {
                Patient patient = new Patient();
                patient.Gender = RandomGender();
                patient.Name = RandomName(patient.Gender);
                patient.Age = RandomAge();
                patient.Contact = RandomPhoneNumber();
                patient.NumberID = RandonNumberID(patient.Gender, patient.Age);
                patient.VisitDate = RandomVisitDate();
                patient.VisitTime = RandomVisitTime(patient.VisitDate);

                patientInformation.AddPatientInformation(patient);
            }
        }

        public void DeleteAllPatientInformation()
        {
            patientInformation.DeleteAllPatientInformation();
        }

        private string RandomGender()
        {
            int random = new Random().Next(0, 2);
            if (random == 0)
            {
                return "男";
            }
            else
            {
                return "女";
            }
        }

        private int RandomAge()
        {
            return new Random().Next(5, 90);
        }

        private string RandomName(string gender)
        {
            // 常见姓氏
            string[] surnames = {"李", "王", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴",
                                 "徐", "孙", "胡", "朱", "高", "林", "何", "郭", "马", "罗"};

            // 男性名字常用字
            string[] maleNames = {"伟", "强", "勇", "军", "磊", "洋", "超", "健", "明", "涛",
                                 "鹏", "华", "平", "鑫", "波", "斌", "宇", "浩", "杰", "帆",
                                 "峰", "亮", "凯", "晨", "龙", "阳", "剑", "锋", "林", "东"};

            // 女性名字常用字
            string[] femaleNames = {"芳", "娜", "敏", "静", "秀", "颖", "琳", "婷", "莹", "雪",
                                   "娟", "燕", "艳", "怡", "洁", "梅", "倩", "玉", "英", "慧",
                                   "莉", "丹", "萍", "玲", "琴", "君", "瑶", "悦", "欣", "薇"};

            var random = new Random();
            string surname = surnames[random.Next(surnames.Length)];

            // 80%单名，20%双名
            if (random.Next(10) < 8)
            {
                return surname + (gender == "男"
                    ? maleNames[random.Next(maleNames.Length)]
                    : femaleNames[random.Next(femaleNames.Length)]);
            }
            else
            {
                string namePart1 = (gender == "男"
                    ? maleNames[random.Next(maleNames.Length)]
                    : femaleNames[random.Next(femaleNames.Length)]);

                string namePart2 = (gender == "男"
                    ? maleNames[random.Next(maleNames.Length)]
                    : femaleNames[random.Next(femaleNames.Length)]);

                return surname + namePart1 + namePart2;
            }
        }

        private string RandomPhoneNumber()
        {
            var random = new Random();
            // 手机号前缀(中国大陆常见号段)
            string[] prefixes = {"130", "131", "132", "133", "134", "135", "136", "137", "138", "139",
                                "150", "151", "152", "153", "155", "156", "157", "158", "159",
                                "180", "181", "182", "183", "184", "185", "186", "187", "188", "189"};

            string phone = prefixes[random.Next(prefixes.Length)];

            // 生成后8位
            for (int i = 0; i < 8; i++)
            {
                phone += random.Next(10);
            }

            return phone;
        }

        private string RandonNumberID(string gender, int age)
        {
            var random = new Random();
            StringBuilder idCard = new StringBuilder();

            // 1. 区域码(6位) - 使用真实的中国行政区划代码
            string[] areaCodes = {
                "110101", "110105", "110108", // 北京
                "310104", "310105", "310106", // 上海
                "440103", "440104", "440106", // 广州
                "440304", "440305", "440306", // 深圳
                "330102", "330103", "330104", // 杭州
                "320102", "320104", "320105", // 南京
                "420102", "420103", "420104", // 武汉
                "510104", "510105", "510106"  // 成都
            };
            idCard.Append(areaCodes[random.Next(areaCodes.Length)]);

            // 2. 出生年月日(8位)
            int currentYear = DateTime.Now.Year;
            int birthYear = currentYear - age;
            int birthMonth = random.Next(1, 13);

            // 计算当月天数
            int maxDay = DateTime.DaysInMonth(birthYear, birthMonth);
            int birthDay = random.Next(1, maxDay + 1);

            idCard.Append($"{birthYear:0000}{birthMonth:00}{birthDay:00}");

            // 3. 顺序码(3位)
            int sequence = random.Next(100, 1000);

            // 性别处理(奇数为男，偶数为女)
            if (gender == "男" && sequence % 2 == 0) sequence++;
            if (gender == "女" && sequence % 2 == 1) sequence--;

            idCard.Append(sequence);

            string first17 = idCard.ToString();
            int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            char[] checkCodes = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                sum += (first17[i] - '0') * weights[i];
            }

            // 4. 校验码(1位)
            idCard.Append(checkCodes[sum % 11]);

            return idCard.ToString();
        }

        private string RandomVisitDate()
        {
            var random = new Random();
            DateTime today = DateTime.Today;

            // 随机生成最近3年的日期
            int daysAgo = random.Next(0, 8);
            DateTime visitDate = today.AddDays(-daysAgo);
            return visitDate.ToString("yyyy-MM-dd");
        }

        private string RandomVisitTime(string visitDate)
        {
            DateTime date = DateTime.ParseExact(visitDate, "yyyy-MM-dd", null);
            Random random = new Random();
            int hour = random.Next(8, 18);    // 8-18 小时
            int minute = random.Next(0, 60);  // 0-59 分钟
            int second = random.Next(0, 60);  // 0-59 秒

            DateTime randomDateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, second);
            string formattedDateTime = randomDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return formattedDateTime;
        }
    }
}
