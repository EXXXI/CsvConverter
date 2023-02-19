using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvConverter
{
    internal class Skill
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 0;

        public Skill(string name, int level)
        {
            Name = name;
            Level = level;
        }

        public string Description
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name) || Level == 0)
                {
                    return string.Empty;
                }

                return Name + "Lv" + Level;
            }
        }
    }
}
