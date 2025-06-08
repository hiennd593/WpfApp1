// Mô tả: Lớp xử lý chỉnh sửa thông tin submission của sinh viên
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WpfApp1
{
    /// <summary>
    /// Cửa sổ chỉnh sửa thông tin submission của sinh viên
    /// </summary>
    public partial class EditSubmission : Window
    {
        // Các biến lưu trữ thông tin ban đầu của submission
        private int _submissionId; // ID của submission cần chỉnh sửa
        private string _originalFilePath; // Đường dẫn file gốc
        private string _newFilePath; // Đường dẫn file mới tạm thời khi upload
        private string _newFileName; // Tên file mới tạm thời khi upload
        private int _originalStudentId; // Student ID ban đầu
        private string _originalStudentName; // Student Name ban đầu
        private string _originalClassName; // Class Name ban đầu
        private string _originalSubjectName; // Subject Name ban đầu
        private string _originalSubmissionTime; // Submission Time ban đầu
        private string _originalStatus; // Status ban đầu

        /// <summary>
        /// Khởi tạo cửa sổ chỉnh sửa submission
        /// </summary>
        /// <param name="submissionId">ID của submission cần chỉnh sửa</param>
        public EditSubmission(int submissionId)
        {
            InitializeComponent();
            _submissionId = submissionId;

            // Lấy dữ liệu ban đầu từ cơ sở dữ liệu
            var submission = ElGamal.GetSubmissions().FirstOrDefault(s => s.Id == submissionId);
            var student = ElGamal.GetStudent(submission.StudentId);
            if (submission == null || student == null)
            {
                MessageBox.Show("Submission or Student not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            // Lưu dữ liệu ban đầu
            _originalFilePath = submission.FilePath;
            _originalStudentId = submission.StudentId;
            _originalStudentName = student.Name;
            _originalClassName = submission.ClassName;
            _originalSubjectName = submission.SubjectName;
            _originalSubmissionTime = submission.SubmissionTime;
            _originalStatus = submission.Status;

            // Hiển thị dữ liệu ban đầu trong form
            StudentIdTextBox.Text = _originalStudentId.ToString();
            StudentNameTextBox.Text = _originalStudentName ?? "";
            ClassNameTextBox.Text = _originalClassName ?? "";
            SubjectNameTextBox.Text = _originalSubjectName ?? "";
            FileNameTextBlock.Text = submission.FileName;
            SubmissionTimeTextBox.Text = _originalSubmissionTime;
            StatusComboBox.SelectedItem = StatusComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == _originalStatus) ?? StatusComboBox.Items[0];
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút Upload
        /// </summary>
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            // Mở hộp thoại chọn file
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*",
                Title = "Select a new file"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                // Lưu tạm thời thông tin file mới
                _newFilePath = openFileDialog.FileName;
                _newFileName = Path.GetFileName(_newFilePath);
                FileNameTextBlock.Text = _newFileName; // Cập nhật hiển thị tên file mới
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi người dùng nhấn nút Save
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lấy dữ liệu từ form và kiểm tra giá trị
                int studentId = int.TryParse(StudentIdTextBox.Text, out int parsedStudentId) ? parsedStudentId : _originalStudentId;
                string studentName = string.IsNullOrWhiteSpace(StudentNameTextBox.Text) ? _originalStudentName : StudentNameTextBox.Text;
                string className = string.IsNullOrWhiteSpace(ClassNameTextBox.Text) ? _originalClassName : ClassNameTextBox.Text;
                string subjectName = string.IsNullOrWhiteSpace(SubjectNameTextBox.Text) ? _originalSubjectName : SubjectNameTextBox.Text;
                string fileName = FileNameTextBlock.Text;
                string submissionTime = _originalSubmissionTime; // Giữ nguyên Submission Time
                string status = StatusComboBox.SelectedItem as string ?? _originalStatus;
                string filePath = _originalFilePath;

                // Xử lý file mới nếu có upload
                if (!string.IsNullOrEmpty(_newFilePath))
                {
                    string outputPath = Path.Combine(Path.GetDirectoryName(_originalFilePath) ?? "", _newFileName);

                    try
                    {
                        // Xóa file cũ nếu tồn tại
                        if (File.Exists(_originalFilePath))
                        {
                            File.Delete(_originalFilePath);
                        }

                        // Sao chép file mới vào cùng vị trí
                        File.Copy(_newFilePath, outputPath, true);
                        filePath = outputPath; // Cập nhật đường dẫn file
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error replacing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Cập nhật thông tin vào cơ sở dữ liệu
                ElGamal.UpdateSubmission(_submissionId, studentId, studentName, className, subjectName, fileName, submissionTime, status, filePath);

                MessageBox.Show("Submission updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Đóng cửa sổ sau khi lưu thành công
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving submission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
