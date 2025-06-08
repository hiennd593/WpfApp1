using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;


namespace WpfApp1
{
    public partial class ReceiverWindow : MahApps.Metro.Controls.MetroWindow
    {
        private List<object> _submissionsDisplayList;
        private CollectionViewSource _viewSource;

        public ReceiverWindow()
        {
            InitializeComponent();
            LoadSubmissions();
        }

        private void LoadSubmissions()
        {
            var submissions = ElGamal.GetSubmissions();
            _submissionsDisplayList = submissions.Select(s =>
            {
                var student = ElGamal.GetStudent(s.StudentId);
                return new
                {
                    s.Id,
                    s.StudentId,
                    StudentName = student?.Name ?? "Unknown",
                    s.ClassName,
                    s.SubjectName,
                    s.FileName,
                    s.SubmissionTime,
                    s.Status,
                    s.FilePath
                };
            }).Cast<object>().ToList();

            _viewSource = new CollectionViewSource { Source = _submissionsDisplayList };
            SubmissionsGrid.ItemsSource = _viewSource.View;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSubmissions();
            SearchTextBox.Text = string.Empty;
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_viewSource == null) return;

            var filterText = SearchTextBox.Text.ToLower();
            var selectedStatus = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            _viewSource.View.Filter = item =>
            {
                var submission = item as dynamic;
                if (submission == null) return false;

                bool matchesSearch = string.IsNullOrEmpty(filterText) ||
                    submission.StudentName.ToLower().Contains(filterText);

                bool matchesStatus = selectedStatus == "All" ||
                    submission.Status == selectedStatus;

                return matchesSearch && matchesStatus;
            };
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = "SubmissionsReport.pdf"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    GeneratePdfReport(saveFileDialog.FileName);
                    MessageBox.Show("Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GeneratePdfReport(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                Paragraph title = new Paragraph("Submissions Report", FontFactory.GetFont("Arial", 16, Font.BOLD));
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);
                document.Add(new Paragraph("Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                document.Add(new Paragraph("\n"));

                var submissions = ElGamal.GetSubmissions();
                int total = submissions.Count;
                int pending = submissions.Count(s => s.Status == "Pending");
                int valid = submissions.Count(s => s.Status == "Valid");
                int invalid = submissions.Count(s => s.Status.StartsWith("Invalid"));

                Paragraph stats = new Paragraph($"Total Submissions: {total}\n" +
                                               $"Pending: {pending}\n" +
                                               $"Valid: {valid}\n" +
                                               $"Invalid: {invalid}\n");
                stats.SpacingAfter = 20f;
                document.Add(stats);

                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1, 1, 2, 2, 2, 2, 2, 1 });

                string[] headers = { "Sub. ID", "Student ID", "Student Name", "Class Name", "Subject Name", "File Name", "Submission Time", "Status" };
                foreach (var header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, FontFactory.GetFont("Arial", 10, Font.BOLD)));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                }

                foreach (var submission in submissions)
                {
                    var student = ElGamal.GetStudent(submission.StudentId);
                    table.AddCell(new Phrase(submission.Id.ToString(), FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.StudentId.ToString(), FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(student?.Name ?? "Unknown", FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.ClassName ?? "N/A", FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.SubjectName ?? "N/A", FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.FileName, FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.SubmissionTime, FontFactory.GetFont("Arial", 10)));
                    table.AddCell(new Phrase(submission.Status, FontFactory.GetFont("Arial", 10)));
                }

                document.Add(table);
                document.Close();
                writer.Close();
            }
        }

        private void VerifySubmission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int submissionId)
            {
                var submission = ElGamal.GetSubmissions().FirstOrDefault(s => s.Id == submissionId);
                if (submission == null)
                {
                    MessageBox.Show("Submission not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var student = ElGamal.GetStudent(submission.StudentId);
                if (student == null)
                {
                    MessageBox.Show("Student not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!BigInteger.TryParse(student.PublicKeyP, out BigInteger p) ||
                    !BigInteger.TryParse(student.PublicKeyG, out BigInteger g) ||
                    !BigInteger.TryParse(student.PublicKeyH, out BigInteger h))
                {
                    MessageBox.Show("Invalid public key format!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var publicKey = (p, g, h);
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "All files (*.*)|*.*",
                    FileName = submission.FileName
                };
                saveFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Verified_file";
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (ElGamal.VerifyAndExtractFile(submission.FilePath, publicKey, saveFileDialog.FileName, out string errorMessage))
                        {
                            ElGamal.UpdateSubmissionStatus(submissionId, "Valid");
                            MessageBox.Show("Submission is VALID!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            ElGamal.UpdateSubmissionStatus(submissionId, "Invalid: " + errorMessage);
                            MessageBox.Show($"Submission is INVALID: {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        LoadSubmissions();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExtractFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int submissionId)
            {
                var submission = ElGamal.GetSubmissions().FirstOrDefault(s => s.Id == submissionId);
                if (submission == null)
                {
                    MessageBox.Show("Submission not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var student = ElGamal.GetStudent(submission.StudentId);
                if (student == null)
                {
                    MessageBox.Show("Student not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "All files (*.*)|*.*",
                    FileName = submission.FileName
                };
                saveFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Unverified_file";
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (ElGamal.ExtractFileOnly(submission.FilePath, saveFileDialog.FileName, out string errorMessage))
                        {
                            MessageBox.Show("The submission was exported without authentication!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Submission is INVALID: {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteSubmission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int submissionId)
            {
                var submission = ElGamal.GetSubmissions().FirstOrDefault(s => s.Id == submissionId);
                if (submission == null)
                {
                    MessageBox.Show("Submission not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                var result = MessageBox.Show($"Are you sure you want to delete submission ID {submissionId} (File: {submission.FileName})?",
                                             "Confirm Delete",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Xóa file .signed nếu tồn tại
                        if (File.Exists(submission.FilePath))
                        {
                            File.Delete(submission.FilePath);
                        }

                        // Xóa bản ghi khỏi cơ sở dữ liệu
                        ElGamal.DeleteSubmission(submissionId);
                        MessageBox.Show("Submission deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadSubmissions(); // Làm mới DataGrid
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting submission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EditSubmission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int submissionId)
            {
                EditSubmission editWindow = new EditSubmission(submissionId); // Pass the required parameter
                editWindow.Show();
            }
        }
    }
}