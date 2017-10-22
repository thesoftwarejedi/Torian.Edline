using System;
using System.Collections.Generic;
using System.Text;

namespace Torian.Edline
{
    public class StudentNotFoundException : Exception
    {
        public string StudentName { get; set; }
        public override string Message => $"Cannot find {StudentName}.";
    }
}
