using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitoringProg
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {

        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.save_ip = richTextBox1.Text;
            Properties.Settings.Default.backup = richTextBox2.Text;
            Properties.Settings.Default.email = textBox1.Text;
            Properties.Settings.Default.checkbox = checkBox1.Checked;
            Properties.Settings.Default.emaillogin = textBox2.Text;
            Properties.Settings.Default.emailpassword = textBox3.Text;
            Properties.Settings.Default.filetype = richTextBox3.Text;
            Properties.Settings.Default.RedSector = (int)numericUpDown1.Value;
            Properties.Settings.Default.YellowSector = (int)numericUpDown2.Value;
            if ((int)numericUpDown2.Value >= (int)numericUpDown1.Value)
            {
                MessageBox.Show("Уровень предупреждения должен быть ниже критического, настройки не будут сохранены!!!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Properties.Settings.Default.Save();
            MessageBox.Show("Перезапустите программу, чтобы изменения вступили в силу!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
