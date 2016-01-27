using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TimechartReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string tchartdatadir = args.Length == 0 ? @".\DATA" : args[0];
            string timetableName = "TTABLE";
            Timechart tc = new Timechart(tchartdatadir, timetableName);

            foreach (Student student in tc.Students)
            {
                foreach (Subject subject in student.Subjects.Where(s => s.Teachers.Count >= 1))
                {
                    Console.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}", 
                        student.Code,
                        student.GivenName,
                        student.Surname,
                        student.House == null ? "----" : student.House.Name,
                        student.Class == null ? "----" : student.Class.Code,
                        student.Year,
                        subject.Code,
                        subject.Name,
                        String.Join("\t",subject.Teachers.Select(t => t.Code))
                    );
                }
            }
        }
    }
}
