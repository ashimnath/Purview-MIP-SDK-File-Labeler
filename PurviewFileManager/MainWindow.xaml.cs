using System.Text;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;

namespace PurviewFileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Title = "Purview File Manager";

            var test = typeof(IFileHandler);

            //MessageBox.Show($"MIP SDK loaded successfully\n\n{test.FullName}","MIP Test");
            txtContent.Text = $"MIP SDK loaded successfully\n\n{test.FullName}";
        }
        private void btnBrowse_Click(
        object sender,
        RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            dialog.Filter =
                "All Files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                txtFilePath.Text =
                    dialog.FileName;

                txtStatus.Text =
                    "File selected.";
            }
        }

        private readonly MipService _mipService =
        new();
        private async void btnLoadLabels_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mipService.CreateAuthDelegate();

                _mipService.CreateMipContext();

                _mipService.CreateFileProfile();

                _mipService.CreateFileEngine();

                var labels =
                    await _mipService.GetLabelsAsync();

                cmbLabels.ItemsSource =
                    labels;

                txtStatus.Text =
                    $"{labels.Count} labels loaded.";

                //MessageBox.Show($"{labels.Count} labels loaded.");
                txtContent.Text = ($"{labels.Count} labels loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString());
            }
        }

        private void btnReadFile_Click(object sender,RoutedEventArgs e)
        {
            try
            {
                var contentLabel =_mipService.ReadLabel(txtFilePath.Text);
                string labelName =contentLabel?.Label?.Name ??"No Label";
                bool protectedState =contentLabel?.IsProtectionAppliedFromLabel?? false;

                txtStatus.Text =$"Label: {labelName} | Protected: {protectedState}";

                string fileContent =_mipService.ReadFileContent(txtFilePath.Text);

                txtContent.Text =
                $@"File Information
                ==============================

                File:
                {txtFilePath.Text}

                Label:
                {labelName}

                Protected:
                {protectedState}

                ==============================
                Content
                ==============================

            { fileContent}";
            }
            catch (Exception ex)
            {
                txtStatus.Text =
                    "Unable to read file.";

                txtContent.Text =
            $@"File Information
            ==============================

            Error reading file

            Reason:
            {ex.Message}";
            }
        }


        private void btnApplyLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFilePath.Text))
                {
                    MessageBox.Show("Please select a file.");

                    return;
                }

                if (cmbLabels.SelectedItem == null)
                {
                    MessageBox.Show("Please select a label.");

                    return;
                }

                var selectedLabel =(LabelInfo)cmbLabels.SelectedItem;

                bool result = _mipService.ApplyLabel(txtFilePath.Text,selectedLabel.Id);

                if (result)
                {
                    txtStatus.Text =$"Label applied: {selectedLabel.Name}";

                    //MessageBox.Show($"Label applied successfully:\n{selectedLabel.Name}");
                }
                else
                {
                    //MessageBox.Show("No changes were made.");
                    txtStatus.Text = ("No changes were made.");
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text ="Apply label failed.";

                txtContent.Text =ex.ToString();
            }
        }
        private async void MainWindow_Loaded(object sender,RoutedEventArgs e)
        {
            try
            {
                var mipService = new MipService();

                string user =await mipService.SignInAsync();

                //MessageBox.Show($"Signed in successfully\n\n{user}", "Authentication");
                txtStatus.Text = $"Signed in successfully\n\n{user}" + "Authentication";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void btnRemoveLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    txtFilePath.Text))
                {
                    MessageBox.Show("Please select a file.");

                    return;
                }

                bool result =_mipService.RemoveLabel(txtFilePath.Text);

                if (result)
                {
                    txtStatus.Text ="Label removed successfully.";

                    //MessageBox.Show("Label removed successfully.");
                }
                else
                {
                    //MessageBox.Show("No changes were made.");
                    txtStatus.Text = ("No changes were made.");
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text ="Remove label failed.";

                txtContent.Text =
            $@"Remove Label

Status:
Failed

Reason:
{ex.Message}

Note:
This version of the SDK does not appear to support removing labels by assigning a null label.";
            }
        }
    }
}