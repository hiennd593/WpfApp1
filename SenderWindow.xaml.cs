
using System.IO;
using System.Numerics;
using System.Windows;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class SenderWindow : MahApps.Metro.Controls.MetroWindow
    {
        private (BigInteger p, BigInteger g, BigInteger h, BigInteger x)? privateKey;

        public SenderWindow()
        {
            InitializeComponent();
        }

        private void GenerateKeys_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentIdText.Text) || string.IsNullOrWhiteSpace(StudentNameText.Text) ||
                string.IsNullOrWhiteSpace(ClassNameText.Text) || string.IsNullOrWhiteSpace(SubjectNameText.Text))
            {
                MessageBox.Show("Please enter Student ID, Name, Class Name, and Subject Name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            privateKey = ElGamal.GenerateKeys();
            PublicKeyText.Text = $"p: {privateKey.Value.p}, g: {privateKey.Value.g}, h: {privateKey.Value.h}";
            try
            {
                File.WriteAllText($"student_{StudentIdText.Text}_public_key.pub", PublicKeyText.Text);
                SubmissionResultText.Text = $"Keys generated and public key saved to student_{StudentIdText.Text}_public_key.pub";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving public key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SignFile_Click(object sender, RoutedEventArgs e)
        {
            if (privateKey == null)
            {
                MessageBox.Show("Please generate keys first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(StudentIdText.Text) || string.IsNullOrWhiteSpace(StudentNameText.Text) ||
                string.IsNullOrWhiteSpace(ClassNameText.Text) || string.IsNullOrWhiteSpace(SubjectNameText.Text))
            {
                MessageBox.Show("Please enter Student ID, Name, Class Name, and Subject Name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(StudentIdText.Text, out int studentId))
            {
                MessageBox.Show("Invalid Student ID!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*"
            };
            openFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Submissions";
            if (openFileDialog.ShowDialog() == true)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Signed files (*.signed)|*.signed",
                    FileName = $"submission_{StudentIdText.Text}.signed"
                };
                saveFileDialog.InitialDirectory = "C:\\Users\\Nitro\\Desktop\\Signed_file";
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        ElGamal.SignAndSaveFile(openFileDialog.FileName, privateKey, saveFileDialog.FileName, studentId, StudentNameText.Text, ClassNameText.Text, SubjectNameText.Text);
                        SubmissionResultText.Text = "Assignment signed and submitted successfully!";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}