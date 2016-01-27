using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TimechartReader
{
    public class Timechart
    {
        protected string DataDirectory;
        public IList<Class> Classes { get; protected set; }
        public IList<Student> Students { get; protected set; }
        public IList<Subject> Subjects { get; protected set; }
        public IList<Room> Rooms { get; protected set; }
        public IList<Teacher> Teachers { get; protected set; }
        public IList<House> Houses { get; protected set; }
        public IList<Group> Groups { get; protected set; }
        public IList<Faculty> Faculties { get; protected set; }
        public IList<string> TimetableYears { get; protected set; }
        public IList<string> TimetableDays { get; protected set; }
        public IList<string> TimetableSlots { get; protected set; }
        public IList<TimetableSlot> Timetable { get; protected set; }
        public IList<TimetablePeriod> Periods { get; protected set; }

        public Timechart(string dataDirectory, string timetableName)
        {
            DataDirectory = dataDirectory;
            Subjects = GetSubjects();
            Teachers = GetTeachers();
            Rooms = GetRooms();
            Houses = GetHouses();
            Faculties = GetFaculties();
            Groups = GetGroups();
            Classes = GetClasses();
            TTABLENAM ttnam;
            Timetable = GetTimetable(timetableName, Subjects, Teachers, Rooms, out ttnam);
            TimetableYears = ttnam.Year.ToList();
            TimetableDays = ttnam.Day.ToList();
            TimetableSlots = ttnam.Slot.ToList();
            Periods = GetPeriods(timetableName, ttnam.Day.Length, ttnam.Slot.Length);

            Students = GetStudents(Subjects, Teachers, Rooms, Houses, Faculties, TimetableYears);
            SetSubjectInfo(Subjects, Timetable);
        }

        protected T ReadFile<T>(string filename, Func<BinaryReader, T> sink)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(Path.Combine(DataDirectory, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return sink(reader);
            }
        }

        protected T ReadFile<T>(string filename, Func<TextReader, T> sink)
        {
            using (TextReader reader = new StreamReader(File.Open(Path.Combine(DataDirectory, filename), FileMode.Open)))
            {
                return sink(reader);
            }
        }

        protected IList<TimetableSlot> GetTimetable(string timetableName, IList<Subject> subjects, IList<Teacher> teachers, IList<Room> rooms, out TTABLENAM ttnam)
        {
            TTABLETTW timetable = ReadFile(timetableName + ".TTW", reader => new TimechartReader.TTABLETTW(reader));
            TTABLENAM ttablename = ReadFile(timetableName + ".NAM", reader => new TimechartReader.TTABLENAM(reader, timetable.Years, timetable.Slots, timetable.Days));
            ttnam = ttablename;
            return timetable.Entries.SelectMany((td, dn) =>
                td.SelectMany((ts, sn) =>
                    ts.SelectMany((ty, yn) =>
                        ty.Select((tl, ln) =>
                            new TimetableSlot
                            {
                                Day = ttablename.Day[dn],
                                Slot = ttablename.Slot[sn],
                                Year = ttablename.Year[yn],
                                Level = ln + 1,
                                Subject = tl.Subject > 0 ? subjects[tl.Subject - 1] : null,
                                Teacher = tl.Teacher > 0 ? teachers[tl.Teacher - 1] : null,
                                Room = tl.Room > 0 ? rooms[tl.Room - 1] : null,
                                Flags = tl.Flags
                            }
                        )
                    )
                )
            ).Where(s => s.Subject != null || s.Teacher != null || s.Room != null).ToList();
        }

        protected IList<Subject> GetSubjects()
        {
            SUBNAME subnames = ReadFile("SUBNAME.DAT", reader => new SUBNAME(reader));
            SUBCODE subcodes = ReadFile("SUBCODE.DAT", reader => new SUBCODE(reader));
            return subcodes.Items.Zip(subnames.Items, (c, n) => new Subject { Code = c, Name = n }).ToList();
        }

        protected IList<Teacher> GetTeachers()
        {
            TENAME tenames = ReadFile("TENAME.DAT", reader => new TENAME(reader));
            TECODE tecodes = ReadFile("TECODE.DAT", reader => new TECODE(reader));
            return tecodes.Items.Zip(tenames.Items, (c, n) => new Teacher { Code = c, Name = n }).ToList();
        }

        protected IList<Room> GetRooms()
        {
            ROOMS rooms = ReadFile("ROOMS.DAT", reader => new ROOMS(reader));
            ROOMNAME roomnames = ReadFile("ROOMNAME.DAT", reader => new ROOMNAME(reader));
            return rooms.Items.Zip(roomnames.Items, (c, n) => new Room { Code = c, Name = n }).ToList();
        }

        protected IList<House> GetHouses()
        {
            HOUSE houses = ReadFile("HOUSE.DAT", reader => new HOUSE(reader));
            return houses.Items.Select(v => new House { Name = v }).ToList();
        }

        protected IList<Group> GetGroups()
        {
            GROUP groups = ReadFile("GROUP.DAT", reader => new GROUP(reader));
            return groups.Items.Select(g => new Group { Name = g.Name, Desc = g.Unk5 }).ToList();
        }

        protected IList<Faculty> GetFaculties()
        {
            FACULTY faculties = ReadFile("FACULTY.DAT", reader => new FACULTY(reader));
            return faculties.Items.Select(f => new Faculty { Name = f.Name }).ToList();
        }

        protected IList<Class> GetClasses(int len)
        {
            CLASSx classes = ReadFile(String.Format("CLASS{0}.DAT", len), reader => new CLASSx(reader, (ushort)len));
            return classes.Items.Select(v => new Class { Code = v }).ToList();
        }

        protected IList<TimetablePeriod> GetPeriods(string ttablename, int days, int slots)
        {
            TTABLECLS periods = ReadFile(String.Format("{0}.CLS", ttablename), reader => new TTABLECLS(reader, days, slots));
            return periods.Entries.SelectMany((se, sn) => se.Select((de, dn) => new TimetablePeriod { PeriodNum = sn + 1, DayNum = dn + 1, Name = de.Name, StartTime = de.StartTime, EndTime = de.EndTime, Data = de })).ToList();
        }

        protected IList<Class> GetClasses()
        {
            CLASS classes = ReadFile("CLASS.DAT", reader => new CLASS(reader));
            return classes.Items.Select(v => new Class { Code = v }).ToList();
        }

        protected IList<Student> GetStudents(IList<Subject> subjects, IList<Teacher> teachers, IList<Room> rooms, IList<House> houses, IList<Faculty> faculties, string year, int timetableYear)
        {
            IList<Class> classes = GetClasses();
            string choicename = String.Format("CHOICE{0}.ST", timetableYear);
            if (File.Exists(Path.Combine(DataDirectory, choicename)))
            {
                CHOICEx students = ReadFile(choicename, reader => new CHOICEx(reader));
                return students.Entries.Select(v =>
                    new Student
                    {
                        Surname = v.Surname,
                        GivenName = v.GivenName,
                        Gender = new String(new char[] { v.Gender }),
                        Code = v.Code,
                        Subjects = v.Subjects.Select(subj => subjects[subj - 1]).ToArray(),
                        Class = v.Class > 0 ? classes[v.Class - 1] : null,
                        Tutor = v.Tutor > 0 ? teachers[v.Tutor - 1] : null,
                        House = v.House > 0 ? houses[v.House - 1] : null,
                        Room = v.Room > 0 ? rooms[v.Room - 1] : null,
                        Year = year,
                    }
                ).ToList();
            }
            else
            {
                return new List<Student>();
            }
        }

        protected IList<Student> GetStudents(IList<Subject> subjects, IList<Teacher> teachers, IList<Room> rooms, IList<House> houses, IList<Faculty> faculties, IList<string> years)
        {
            return years.SelectMany((y,i) => GetStudents(subjects, teachers, rooms, houses, faculties, years[i], i + 1)).ToList();
        }

        protected void SetSubjectInfo(IList<Subject> subjects, IList<TimetableSlot> timetable)
        {
            foreach (TimetableSlot slot in Timetable)
            {
                if (slot.Subject != null)
                {
                    if (slot.Teacher != null && !slot.Subject.Teachers.Contains(slot.Teacher))
                    {
                        slot.Subject.Teachers.Add(slot.Teacher);
                    }
                    if (slot.Room != null && !slot.Subject.Rooms.Contains(slot.Room))
                    {
                        slot.Subject.Rooms.Add(slot.Room);
                    }
                    if (!slot.Subject.Years.Contains(slot.Year))
                    {
                        slot.Subject.Years.Add(slot.Year);
                    }
                }
            }
        }
    }
}
