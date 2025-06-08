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

        private void InitializeDatabase()
        {
            // Đảm bảo cơ sở dữ liệu và bảng được tạo khi ứng dụng khởi động
            using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new System.Data.SQLite.SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS Students (Id INTEGER PRIMARY KEY, Name TEXT, PublicKeyP TEXT, PublicKeyG TEXT, PublicKeyH TEXT)", conn);
                command.ExecuteNonQuery();

                command = new System.Data.SQLite.SQLiteCommand(
                    "CREATE TABLE IF NOT EXISTS Submissions (Id INTEGER PRIMARY KEY AUTOINCREMENT, StudentId INTEGER, FileName TEXT, Hash TEXT, SignatureR TEXT, SignatureS TEXT, SubmissionTime TEXT, Status TEXT, FilePath TEXT, ClassName TEXT, SubjectName TEXT)", conn);
                command.ExecuteNonQuery();

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