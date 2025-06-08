using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        /// <summary>
        /// Khởi tạo cơ sở dữ liệu và tạo các bảng cần thiết nếu chưa tồn tại.
        /// Hàm này sẽ được gọi khi ứng dụng khởi động để đảm bảo mọi thứ sẵn sàng.
        /// </summary>
        private void InitializeDatabase()
        {
            // Tạo kết nối đến file cơ sở dữ liệu SQLite submissions.db
            using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                // Mở kết nối tới cơ sở dữ liệu
                conn.Open();

                // Tạo câu lệnh SQL để tạo bảng Students nếu chưa tồn tại
                // Bảng Students gồm các trường: Id (khóa chính), Name, PublicKeyP, PublicKeyG, PublicKeyH
                var command = new System.Data.SQLite.SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS Students (Id INTEGER PRIMARY KEY, Name TEXT, PublicKeyP TEXT, PublicKeyG TEXT, PublicKeyH TEXT)", conn);
                // Thực thi câu lệnh tạo bảng Students
                command.ExecuteNonQuery();

                // Tạo câu lệnh SQL để tạo bảng Submissions nếu chưa tồn tại
                // Bảng Submissions gồm các trường: Id (khóa chính tự tăng), StudentId (liên kết với Students), FileName, Hash, SignatureR, SignatureS, SubmissionTime, Status, FilePath, ClassName, SubjectName
                command = new System.Data.SQLite.SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS Submissions (Id INTEGER PRIMARY KEY AUTOINCREMENT, StudentId INTEGER, FileName TEXT, Hash TEXT, SignatureR TEXT, SignatureS TEXT, SubmissionTime TEXT, Status TEXT, FilePath TEXT, ClassName TEXT, SubjectName TEXT)", conn);
                // Thực thi câu lệnh tạo bảng Submissions
                command.ExecuteNonQuery();

                // Đóng kết nối tới cơ sở dữ liệu
                conn.Close();
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var senderWindow = new SenderWindow();
            senderWindow.Show();
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            var receiverWindow = new ReceiverWindow();
            receiverWindow.Show();
        }
    }
}