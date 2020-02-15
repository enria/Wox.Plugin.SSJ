using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;

namespace Tip {
	public partial class SettingControl :UserControl {
        Setting _setting;

        public SettingControl(Setting setting) {
            InitializeComponent();
            this._setting = setting;
            DocDir.Text = _setting.DocRoot;
        }

        private void OnSelectDirectoryClick(object sender, RoutedEventArgs e) {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                _setting.DocRoot = folderBrowserDialog.SelectedPath;
                DocDir.Text = _setting.DocRoot;
                _setting.Save();
            }
        }
    }
}
