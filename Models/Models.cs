using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PublicKeyP { get; set; }
        public string PublicKeyG { get; set; }
        public string PublicKeyH { get; set; }
    }

    public class Submission
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public string SignatureR { get; set; }
        public string SignatureS { get; set; }
        public string SubmissionTime { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
        public string ClassName { get; set; }    // Thêm trường ClassName
        public string SubjectName { get; set; }  // Thêm trường SubjectName
    }
}