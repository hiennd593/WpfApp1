// Thư viện sử dụng cho thao tác file
using System.IO;
// Thư viện hỗ trợ số lớn cho thuật toán ElGamal
using System.Numerics;
// Thư viện giao diện WPF
using System.Windows;
// Thư viện hỗ trợ các hộp thoại mở/lưu file
using Microsoft.Win32;

namespace WpfApp1
{
    // SenderWindow kế thừa từ MahApps.Metro.Controls.MetroWindow để có giao diện hiện đại
    public partial class SenderWindow : MahApps.Metro.Controls.MetroWindow
    {
        // Lưu trữ khóa riêng tư dưới dạng tuple (p, g, h, x) của ElGamal
        private (BigInteger p, BigInteger g, BigInteger h, BigInteger x)? privateKey;

        // Hàm khởi tạo cửa sổ SenderWindow
        public SenderWindow()
        {
            // Khởi tạo các thành phần giao diện từ XAML
            InitializeComponent();
        }

        // Sự kiện khi nhấn nút "Generate Keys" để sinh khóa ElGamal
        private void GenerateKeys_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra các trường thông tin bắt buộc đã được nhập chưa
            if (string.IsNullOrWhiteSpace(StudentIdText.Text) || string.IsNullOrWhiteSpace(StudentNameText.Text) ||
                string.IsNullOrWhiteSpace(ClassNameText.Text) || string.IsNullOrWhiteSpace(SubjectNameText.Text))
            {
                // Hiển thị thông báo lỗi nếu thiếu thông tin
                MessageBox.Show("Please enter Student ID, Name, Class Name, and Subject Name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Gọi hàm sinh khóa ElGamal, trả về tuple chứa các giá trị p, g, h, x
            privateKey = ElGamal.GenerateKeys();
            // Hiển thị khóa công khai lên giao diện
            PublicKeyText.Text = $"p: {privateKey.Value.p}, g: {privateKey.Value.g}, h: {privateKey.Value.h}";
            try
            {
                // Lưu khóa công khai vào file với tên chứa mã sinh viên
                File.WriteAllText($"student_{StudentIdText.Text}_public_key.pub", PublicKeyText.Text);
                // Thông báo thành công trên giao diện
                SubmissionResultText.Text = $"Keys generated and public key saved to student_{StudentIdText.Text}_public_key.pub";
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khi ghi file
                MessageBox.Show($"Error saving public key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sự kiện khi nhấn nút "Sign File" để ký file bài nộp
        private void SignFile_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra đã sinh khóa chưa
            if (privateKey == null)
            {
                MessageBox.Show("Please generate keys first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Kiểm tra các trường thông tin bắt buộc đã được nhập chưa
            if (string.IsNullOrWhiteSpace(StudentIdText.Text) || string.IsNullOrWhiteSpace(StudentNameText.Text) ||
                string.IsNullOrWhiteSpace(ClassNameText.Text) || string.IsNullOrWhiteSpace(SubjectNameText.Text))
            {
                MessageBox.Show("Please enter Student ID, Name, Class Name, and Subject Name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Kiểm tra mã sinh viên có hợp lệ không (phải là số nguyên)
            if (!int.TryParse(StudentIdText.Text, out int studentId))
            {
                MessageBox.Show("Invalid Student ID!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Mở hộp thoại chọn file bài nộp
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*" // Cho phép chọn mọi loại file
            };
            openFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Submissions"; // Thư mục mặc định
            if (openFileDialog.ShowDialog() == true)
            {
                // Nếu người dùng chọn file, mở hộp thoại lưu file đã ký
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Signed files (*.signed)|*.signed", // Chỉ lưu định dạng .signed
                    FileName = $"submission_{StudentIdText.Text}.signed" // Tên file mặc định
                };
                saveFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Signed_file"; // Thư mục mặc định
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // Gọi hàm ký file và lưu file đã ký
                        ElGamal.SignAndSaveFile(openFileDialog.FileName, privateKey, saveFileDialog.FileName, studentId, StudentNameText.Text, ClassNameText.Text, SubjectNameText.Text);
                        // Thông báo thành công trên giao diện
                        SubmissionResultText.Text = "Assignment signed and submitted successfully!";
                    }
                    catch (Exception ex)
                    {
                        // Xử lý lỗi khi ký hoặc lưu file
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}