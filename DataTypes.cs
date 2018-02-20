using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimechartReader
{
    public class Class
    {
        public string Code;

        public override string ToString()
        {
            return Code;
        }
    }

    public class Student
    {
        public string Surname;
        public string GivenName;
        public Subject[] Subjects;
        public string Gender;
        public Class Class;
        public Group Group;
        public Room Room;
        public string Code;
        public Teacher Tutor;
        public House House;
        public Faculty Faculty;
        public string Year;

        public override string ToString()
        {
            return GivenName + " " + Surname;
        }
    }

    public class Subject
    {
        public string Code;
        public string Name;
        public List<string> Years = new List<string>();
        public List<Teacher> Teachers = new List<Teacher>();
        public List<Room> Rooms = new List<Room>();
        public List<TimetableSlot> Slots = new List<TimetableSlot>();

        public override string ToString()
        {
            return Code + " :: yr" + String.Join("/", Years) + " :: " + String.Join(" and ", Teachers.Select(t => t.Name));
        }
    }

    public class Group
    {
        public string Name;
        public string Desc;

        public override string ToString()
        {
            return Name;
        }
    }

    public class Teacher
    {
        public string Code;
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }

    public class Room
    {
        public string Code;
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }

    public class House
    {
        public string Name;

        public override string ToString()
        {
            return Name;
        }
    }

    public class Faculty
    {
        public string Name;
    }

    public class TimetablePeriod
    {
        public string ID;
        public string Name;
        public TimeSpan StartTime;
        public TimeSpan EndTime;
    }

    public class TimetableSlot
    {
        public int DayNumber;
        public string Day;
        public string Slot;
        public string Year;
        public TimetablePeriod Period;
        public int Level;
        public Subject Subject;
        public Teacher Teacher;
        public Room Room;
        public int Flags;

        public override string ToString()
        {
            return String.Format("{0}:{1} ({8}-{9}):{2}:{3} S:{4} T:{5} R:{6} F:{7:X4}", 
                Day, Slot, Year, Level, 
                Subject == null ? "---" : Subject.Code, 
                Teacher == null ? "---" : Teacher.Code, 
                Room == null ? "---" : Room.Code, Flags,
                Period.StartTime, Period.EndTime);
        }
    }
}
