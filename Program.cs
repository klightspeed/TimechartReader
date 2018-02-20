using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace TimechartReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string tchartdatadir = args.Length == 0 ? @".\DATA" : args[0];
            string timetableName = args.Length <= 1 ? "TTABLE" : args[1];

            int schoolid = 0;
            if (args.Length >= 3)
            {
                Int32.TryParse(args[2], out schoolid);
            }

            Timechart tc = new Timechart(tchartdatadir, timetableName);

            var subjects = tc.Subjects.OrderBy(s => s.Code);

            List<StudentClassSlot> slots = new List<StudentClassSlot>();

            foreach (Student student in tc.Students)
            {
                foreach (Subject subject in student.Subjects)
                {
                    foreach (TimetableSlot slot in subject.Slots)
                    {
                        slots.Add(new StudentClassSlot
                        {
                            Subject = subject.Name,
                            Class = subject.Code,
                            StudentID = student.Code,
                            Day = slot.DayNumber,
                            DayName = slot.Day,
                            PeriodID = slot.Period.ID,
                            PeriodName = slot.Period.Name,
                            Room = slot.Room?.Code ?? "",
                            Teacher1 = slot.Teacher?.Code ?? "",
                            Level = slot.Level,
                            StudentObj = student,
                            SubjectObj = subject,
                            SlotObj = slot
                        });
                    }
                }
            }

            var slotgroups = slots.GroupBy(s => s.StudentID + "::" + s.Day + "::" + s.PeriodID).OrderBy(g => g.Key).Select(g => g.OrderBy(s => s.Level).ToList()).ToList();
            var slotmulti = slotgroups.Where(g => g.Count > 1).ToList();
            slots = slotgroups.Select(g => g.First()).ToList();

            string connstring = System.Configuration.ConfigurationManager.ConnectionStrings["StagingDB"]?.ConnectionString;

            if (connstring != null && schoolid != 0)
            {
                using (SqlConnection conn = new SqlConnection(connstring))
                {
                    conn.Open();

                    using (SqlTransaction txn = conn.BeginTransaction())
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = txn;
                            cmd.CommandText = "DELETE FROM Staging.TBL_STU_CLASS WHERE Schoolid = @SchoolID AND Period not IN (SELECT periodvalue FROM vw_TBL_SCH_PERTIMES_HOMEROOMS WHERE  vw_TBL_SCH_PERTIMES_HOMEROOMS.SchoolId = @SchoolID)";
                            cmd.Parameters.AddWithValue("@SchoolID", schoolid);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = txn;
                            cmd.CommandText = "INSERT INTO Staging.TBL_STU_CLASS (SchoolID, StudentID, Class, Subject, FullClass, TTPeriod, Day, Period, TeacherID, Room, TChartStudentID) VALUES (@SchoolID, @StudentID, @Class, @Subject, @FullClass, @TTPeriod, @Day, @Period, @TeacherID, @Room, @TChartStudentID)";
                            cmd.Parameters.AddWithValue("@SchoolID", schoolid);
                            SqlParameter pstudentid = cmd.Parameters.Add("@StudentID", SqlDbType.VarChar);
                            SqlParameter pclass = cmd.Parameters.Add("@Class", SqlDbType.VarChar);
                            SqlParameter psubject = cmd.Parameters.Add("@Subject", SqlDbType.VarChar);
                            SqlParameter pfullclass = cmd.Parameters.Add("@FullClass", SqlDbType.VarChar);
                            SqlParameter pweek = cmd.Parameters.Add("@TTPeriod", SqlDbType.Int);
                            SqlParameter pday = cmd.Parameters.Add("@Day", SqlDbType.Int);
                            SqlParameter pperiod = cmd.Parameters.Add("@Period", SqlDbType.Int);
                            SqlParameter pteacher = cmd.Parameters.Add("@TeacherID", SqlDbType.VarChar);
                            SqlParameter proom = cmd.Parameters.Add("@Room", SqlDbType.VarChar);
                            SqlParameter ptcstudentid = cmd.Parameters.Add("@TChartStudentID", SqlDbType.VarChar);

                            for (int i = 0; i < slots.Count; i++)
                            {
                                var slot = slots[i];
                                int periodnum;
                                if (Int32.TryParse(slot.PeriodID, out periodnum))
                                {
                                    pstudentid.Value = slot.StudentID;
                                    pclass.Value = slot.Class;
                                    psubject.Value = slot.Subject;
                                    pfullclass.Value = slot.Class;
                                    pweek.Value = slot.Day / 5 + 1;
                                    pday.Value = slot.Day % 5 + 1;
                                    pperiod.Value = periodnum;
                                    pteacher.Value = slot.Teacher1;
                                    proom.Value = slot.Room;
                                    ptcstudentid.Value = slot.StudentID;
                                    cmd.ExecuteNonQuery();
                                }

                                if (i % 50 == 0)
                                {
                                    Console.Write("\n{0:d6} ", i);
                                }
                                Console.Write(".");
                            }
                        }

                        txn.Commit();
                    }
                }
            }
        }
    }
}
