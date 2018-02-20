using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TimechartReader
{
    public class StringFile
    {
        public ushort ItemCount;
        public ushort ItemLength;
        public string[] Items;

        public StringFile(BinaryReader reader)
        {
            ItemCount = reader.ReadUInt16();
            ItemLength = reader.ReadUInt16();
            Items = Enumerable.Range(0, ItemCount).Select(i => Encoding.ASCII.GetString(reader.ReadBytes(ItemLength)).TrimEnd()).ToArray();
        }

        public StringFile(BinaryReader reader, ushort itemLength)
        {
            ItemCount = reader.ReadUInt16();
            ItemLength = itemLength;
            Items = Enumerable.Range(0, ItemCount).Select(i => Encoding.ASCII.GetString(reader.ReadBytes(ItemLength)).TrimEnd()).ToArray();
        }
    }

    public class VersionedStringFile : StringFile
    {
        public string Version;

        public VersionedStringFile(BinaryReader reader) 
            : this(reader, Encoding.ASCII.GetString(reader.ReadBytes(4))) 
        {
        }

        public VersionedStringFile(BinaryReader reader, string version) : base(reader)
        {
            Version = version;
        }
    }

    public class SUBCODE : StringFile { public SUBCODE(BinaryReader reader) : base(reader) { } }

    public class SUBNAME : VersionedStringFile { public SUBNAME(BinaryReader reader) : base(reader) { } }

    public class CLASSx : StringFile { public CLASSx(BinaryReader reader, ushort itemLength) : base(reader, itemLength) { } }

    public class CLASS : StringFile { public CLASS(BinaryReader reader) : base(reader, reader.ReadUInt16()) { } }

    public class CHOICEx
    {
        public class EntryType
        {
            public string Surname;
            public string GivenName;
            public ushort[] Subjects;
            public char Gender;
            public ushort Class;
            public ushort House;
            public string Code;
            public ushort Tutor;
            public ushort Room;
            public ushort[] Unk = new ushort[2];

            public EntryType(BinaryReader reader, int subjectCount, int enrolmentNumberLength)
            {
                Surname = Encoding.ASCII.GetString(reader.ReadBytes(30)).TrimEnd();
                GivenName = Encoding.ASCII.GetString(reader.ReadBytes(30)).TrimEnd();
                Subjects = Enumerable.Range(0, subjectCount).Select(i => (int)reader.ReadUInt16()).Where(s => s > 0).Select(i => (ushort)i).ToArray();
                Gender = (char)reader.ReadByte();
                Class = reader.ReadUInt16();
                House = reader.ReadUInt16();
                Code = Encoding.ASCII.GetString(reader.ReadBytes(enrolmentNumberLength)).TrimEnd();
                Tutor = reader.ReadUInt16();
                Room = reader.ReadUInt16();
                Unk[0] = reader.ReadUInt16();
                Unk[1] = reader.ReadUInt16();
            }
        }

        public string Version;
        public ushort EntryCount;
        public ushort SubjectCount;
        public ushort CodeLength;
        public ushort EntrySize;
        public EntryType[] Entries;

        public CHOICEx(BinaryReader reader)
        {
            Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
            EntryCount = reader.ReadUInt16();
            SubjectCount = reader.ReadUInt16();
            CodeLength = reader.ReadUInt16();
            EntrySize = reader.ReadUInt16();
            reader.ReadBytes(EntrySize - 12);
            Entries = Enumerable.Range(0, EntryCount).Select(i => new EntryType(reader, SubjectCount, CodeLength)).ToArray();
        }
    }

    public class ROOMS : StringFile { public ROOMS(BinaryReader reader) : base(reader) { } }

    public class ROOMNAME : StringFile { public ROOMNAME(BinaryReader reader) : base(reader, 25) { } }

    public class HOUSE : StringFile { public HOUSE(BinaryReader reader) : base(reader, 10) { } }

    public class TECODE : StringFile { public TECODE(BinaryReader reader) : base(reader) { } }

    public class TENAME : StringFile { public TENAME(BinaryReader reader) : base(reader, 25) { } }

    public class GROUP
    {
        public class EntryType
        {
            public ushort NameLength;
            public string Name;
            public ushort Unk1;
            public ushort Unk2;
            public ushort Unk3;
            public ushort Unk4;
            public string Unk5;

            public EntryType(BinaryReader reader)
            {
                NameLength = reader.ReadUInt16();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(NameLength));
                Unk1 = reader.ReadUInt16();
                Unk2 = reader.ReadUInt16();
                Unk3 = reader.ReadUInt16();
                Unk4 = reader.ReadUInt16();
                Unk5 = Encoding.ASCII.GetString(reader.ReadBytes(20)).TrimEnd();
            }
        }

        public string Version;
        public ushort EntryCount;
        public EntryType[] Items;
        public ushort Unk1;
        public ushort Unk2;
        public ushort Unk3;

        public GROUP(BinaryReader reader)
        {
            Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
            EntryCount = reader.ReadUInt16();
            Items = Enumerable.Range(0, EntryCount).Select(i => new EntryType(reader)).ToArray();
            Unk1 = reader.ReadUInt16();
            Unk2 = reader.ReadUInt16();
            Unk3 = reader.ReadUInt16();
        }
    }

    public class FACULTY
    {
        public class Entry
        {
            public string Name;
            public List<int> Unk1 = new List<int>();
        }

        public ushort EntryCount;
        public Entry[] Items;

        public FACULTY(BinaryReader reader)
        {
            EntryCount = reader.ReadUInt16();
            
            // Rest of file is lines of names or numbers
            byte[] data = new byte[(int)reader.BaseStream.Length];
            reader.Read(data, 0, data.Length);
            string[] lines = new String(data.Where(c => c != '\r').Select(c => (char)c).ToArray()).Split('\n');
            List<Entry> entries = new List<Entry>();
            Entry currententry = null;

            foreach (string line in lines)
            {
                if (line.All(c => c >= '0' && c <= '9'))
                {
                    currententry.Unk1.Add(Int32.Parse(line));
                }
                else
                {
                    currententry = new Entry { Name = line };
                    entries.Add(currententry);
                }
            }

            Items = entries.ToArray();
        }
    }

    public class TTABLECLS
    {
        public class DayEntry : List<SlotEntry>
        {
            public DayEntry(BinaryReader reader, int day, int slots)
            {
                for (int i = 0; i < slots; i++)
                {
                    this.Add(new SlotEntry(reader, day, i));
                }
            }
        }

        public class SlotEntry
        {
            public ushort NameLength;
            public string Name;
            public char Number;
            public double Unk2;
            public TimeSpan StartTime;
            public TimeSpan EndTime;

            public SlotEntry(BinaryReader reader, int day, int slot)
            {
                NameLength = reader.ReadUInt16();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(NameLength));
                Number = (char)reader.ReadByte();
                Unk2 = reader.ReadDouble();
                double startval = reader.ReadDouble();
                double endval = reader.ReadDouble();
                StartTime = TimeSpan.FromDays(startval);
                EndTime = TimeSpan.FromDays(endval);
            }

            public override string ToString()
            {
                return String.Format("{0} ({1}-{2})", this.Name, this.StartTime, this.EndTime);
            }
        }

        public ushort[] Unk1;
        public ushort[] Unk2;
        public ushort[] Unk3;
        public DayEntry[] Entries;

        public TTABLECLS(BinaryReader reader, int days, int slots)
        {
            // Skip the leading empty space
            List<ushort> unk = new List<ushort>();
            ushort[] unk2;

            do
            {
                unk2 = Enumerable.Range(0, days).Select(i => reader.ReadUInt16()).ToArray();
                if (unk2.All(v => v == 0))
                {
                    unk.AddRange(unk2);
                }
            }
            while (unk2.All(v => v == 0));

            Unk1 = unk.ToArray();
            Unk2 = unk2;
            Unk3 = Enumerable.Range(0, 12).Select(i => reader.ReadUInt16()).ToArray();

            Entries = new DayEntry[days];

            for (int i = 0; i < days; i++)
                Entries[i] = new DayEntry(reader, i, slots);
        }
    }

    public class TTABLETTW
    {
        public class DayEntry : List<SlotEntry> 
        {
            public DayEntry(BinaryReader reader, int day, int slots, int years, int levels)
                : base(Enumerable.Range(0, slots).Select(i => new SlotEntry(reader, day, i, years, levels))) 
            {
            }
        }

        public class SlotEntry : List<YearEntry> 
        {
            public SlotEntry(BinaryReader reader, int day, int slot, int years, int levels) 
                : base(Enumerable.Range(0, years).Select(i => new YearEntry(reader, day, slot, i, levels))) 
            {
            }
        }

        public class YearEntry : List<LevelEntry> 
        {
            public YearEntry(BinaryReader reader, int day, int slot, int year, int levels) 
                : base(Enumerable.Range(0, levels).Select(i => new LevelEntry(reader, day, slot, year, i))) 
            {
            }
        }

        public class LevelEntry
        {
            public int Day;
            public int Slot;
            public int Year;
            public int Level;
            public ushort Subject;
            public ushort Teacher;
            public ushort Room;
            public ushort Flags;

            public LevelEntry(BinaryReader reader, int day, int slot, int year, int level)
            {
                Day = day;
                Slot = slot;
                Year = year;
                Level = level;
                Subject = reader.ReadUInt16();
                Teacher = reader.ReadUInt16();
                Room = reader.ReadUInt16();
                Flags = reader.ReadUInt16();
            }

            public override string ToString()
            {
                return String.Format("D{0:D3}S{1:D3}Y{2:D3}L{3:D3}: {4,5} {5,5} {6,5} {7,5}", Day, Slot, Year, Level, Subject, Teacher, Room, Flags);
            }
        }

        public byte Weeks;
        public byte Days;
        public byte Slots;
        public byte Years;
        public ushort Levels;
        public byte[] YearLevels;
        public byte[] YearUnk1;
        public byte NameLength;
        public string Name;
        public ushort[] DaySlotUnk1;
        public byte[] DaySlotUnk2;
        public byte[] Unk1;
        public DayEntry[] Entries;

        public TTABLETTW(BinaryReader reader)
        {
            Weeks = reader.ReadByte();
            Days = reader.ReadByte();
            Slots = reader.ReadByte();
            Years = reader.ReadByte();
            Levels = reader.ReadUInt16();
            YearLevels = reader.ReadBytes(Years);
            YearUnk1 = reader.ReadBytes(Years);
            NameLength = reader.ReadByte();
            Name = Encoding.ASCII.GetString(reader.ReadBytes(NameLength));
            DaySlotUnk1 = Enumerable.Range(0, Days * Slots * 2).Select(i => reader.ReadUInt16()).ToArray();
            DaySlotUnk2 = reader.ReadBytes(Days * Slots);

            // Skip the unknown space between the above unknown bitmaps and the table
            int length = (int)reader.BaseStream.Length;
            int pos = 6 + Years * 2 + 1 + NameLength + Days * Slots * 5;
            int tbllen = Days * Slots * Years * Levels * 8;
            Unk1 = reader.ReadBytes(length - (pos + tbllen));

            Entries = Enumerable.Range(0, Days).Select(i => new DayEntry(reader, i, Slots, Years, Levels)).ToArray();
        }
    }

    public class TTABLENAM
    {
        public string[] Year;
        public string[] Slot;
        public string[] Day;

        public TTABLENAM(TextReader reader, int years, int slots, int days)
        {
            Year = Enumerable.Range(0, years).Select(i => reader.ReadLine()).ToArray();
            Slot = Enumerable.Range(0, slots).Select(i => reader.ReadLine()).ToArray();
            Day = Enumerable.Range(0, days).Select(i => reader.ReadLine()).ToArray();
        }
    }
}
