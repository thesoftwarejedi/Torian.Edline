using System;
using System.Collections.Generic;
using System.Text;

namespace Torian.Edline
{
    public class LookupGradesResponse
    {
        public IEnumerable<ClassGrade> Grades { get; set; }
        public string StudentName { get; set; }
    }

    public class ClassGrade
    {
        public string ClassName { get; set; }
        public string Grade { get; set; }
    }
}
