using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimechartReader
{
    [System.Diagnostics.DebuggerDisplay("{StudentID}:{Day}:{PeriodID}:{Level}:{Class}:{Room}:{Teacher1}")]
    public class StudentClassSlot
    {
        public string StudentID { get; set; }
        public string Subject { get; set; }
        public string Class { get; set; }
        public int Day { get; set; }
        public string DayName { get; set; }
        public string PeriodID { get; set; }
        public string PeriodName { get; set; }
        public string Teacher1 { get; set; }
        public string Teacher2 { get; set; }
        public string Room { get; set; }
        public int Level { get; set; }
        public Student StudentObj { get; set; }
        public Subject SubjectObj { get; set; }
        public TimetableSlot SlotObj { get; set; }
    }
}
