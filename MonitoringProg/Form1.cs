using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net.Mail;
using System.Net;
using System.Net.Sockets;

namespace MonitoringProg
{
    public partial class Form1 : Form
    {
        Ping ping = new Ping();
        PingReply pingReply = null;
        List<string> IPs = new List<string>();
        List<string> Backups = new List<string>();
        List<string> FileType = new List<string>();
        Settings set = new Settings();
        DateTime CheckDate = DateTime.Now;
        DateTime DateNextDay = DateTime.Now.AddDays(1);
        Thread ThIp;
        Thread ThBackup;
        Thread InternetSocket;

        int count_on = 0;
        int count_off = 0;
        int countip = 0;
        int countbackup = 0;
        int countfiletype = 0;
        int sendmail = 0;
        int errormail = 0;
        int countb_grey = 0;
        int countb_green = 0;
        int countb_red = 0;
        int countb_yellow = 0;


        bool IsBackupTrue = true;
        bool IsCheckedTimer2 = false;


        public Form1()
        {
            InitializeComponent();
            set.richTextBox1.Text = Properties.Settings.Default.save_ip;
            set.richTextBox2.Text = Properties.Settings.Default.backup;
            set.richTextBox3.Text = Properties.Settings.Default.filetype;
            set.textBox1.Text = Properties.Settings.Default.email;
            set.checkBox1.Checked = Properties.Settings.Default.checkbox;
            set.textBox2.Text = Properties.Settings.Default.emaillogin;
            set.textBox3.Text = Properties.Settings.Default.emailpassword;
            set.numericUpDown1.Value = Properties.Settings.Default.RedSector;
            set.numericUpDown2.Value = Properties.Settings.Default.YellowSector;
            button3.Enabled = false;
            LoadData();
        }

        public void LoadData()
        {
            foreach (string ip in set.richTextBox1.Lines)
            {
                if (!string.IsNullOrEmpty(ip) && ip != " ")
                {
                    IPs.Add(ip);
                    countip++;
                }

            }
            foreach (string backup in set.richTextBox2.Lines)
            {
                if (!string.IsNullOrEmpty(backup) && backup != " ")
                {
                    Backups.Add(backup);
                    countbackup++;
                }

            }

            foreach (string file in set.richTextBox3.Lines)
            {
                if (!string.IsNullOrEmpty(file) && file != " ")
                {
                    FileType.Add("*" + file);
                    countfiletype++;
                }

            }

            if (countip < 1 && countbackup < 1)
            {
                LiveLogs("Нет доступных ip и бэкапов, добавьте через настройки!");
            }
            else
            {
                LiveLogs($"Загружено: {countip} ip адресов и {countbackup} путей хранений бэкапов!");
                StartCheck();
            }
            LiveLogs("Запуск программы...");
            label12.Text = "Работает";
            label12.ForeColor = Color.Green;
            label4.Text = countip.ToString();
        }

        private void StartCheck()
        {
            if (countip > 0)
            {
                ThIp = new Thread(CheckIpStatus)
                {
                    IsBackground = true
                };
                ThIp.Start();
                timer2.Enabled = true;
            }
            if (countbackup > 0 && countfiletype > 0)
            {
                ThBackup = new Thread(BackupStatus)
                {
                    IsBackground = true
                };
                ThBackup.Start();
                timer1.Enabled = true;
            }
        }

        private void CheckIpStatus()
        {
            bool IsChecked = false;

                foreach (string ip in IPs)
                {
                    Thread.Sleep(100);
                    DateTime date = DateTime.Now;
                    try
                    {
                        pingReply = ping.Send(ip, 15000);
                        if (pingReply.Status == IPStatus.Success)
                        {
                            IsChecked = true;
                            RefreshStatus(ip, IsChecked);
                        }
                        else if (pingReply.Status == IPStatus.TimedOut)
                        {
                            IsChecked = false;
                            RefreshStatus(ip, IsChecked);
                            if (ip != "8.8.8.8" && date > CheckDate)
                            {
                                CheckDate = CheckDate.AddMinutes(30);
                                SendMail($"Проблемы с сервером {ip} !!!");
                                LiveLogs($"От сервера {ip} не получен ответ!");
                                Logger.WriteLogs($"От сервера {ip} не получен ответ.");
                            }
                        }
                    }
                    catch (PingException e)
                    {
                        IsChecked = false;
                        RefreshStatus(ip, IsChecked);
                        LiveLogs($"Ошибка с адресом {ip}");
                        Logger.WriteErrors(e.Message);
                    }
                }
            IsCheckedTimer2 = true;
        }

  

        private void RefreshStatus(string ip, bool IsChecked)
        {
            Icon img;
            DateTime date = DateTime.Now;
            if (IsChecked)
               img = Properties.Resources.check;
            else
                img = Properties.Resources.delete;


            Action action = () =>
            {
                dataGridView1.Rows.Add(ip, img, date.ToString(), IsChecked.ToString());
            };

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Value.ToString().Equals(ip))
                    action = () =>
                    {
                        row.SetValues(ip, img, date.ToString(), IsChecked.ToString());
                    };

            }

            invokeEx(action);
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ThBackup.IsAlive)
                ThBackup.Abort();
            if (ThIp.IsAlive)
                ThIp.Abort();
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //notifyIcon1.BalloonTipTitle = "Some Title";
            //notifyIcon1.BalloonTipText = "Some Notification";
            ToolTip t = new ToolTip();
            t.SetToolTip(button2, "Настройки");
            t.SetToolTip(button1, "Отправить тестовое сообщение на почту");
            t.SetToolTip(button3, "Обновить информацию о бэкапах");
            notifyIcon1.Text = "Мониторинг систем КМП№1";
        }

        private void BackupStatus()
        {
                if (DateTime.Now >= DateNextDay)
                {
                    DateNextDay = DateTime.Now.AddDays(1);
                    IsBackupTrue = true;
                   
                }
                if (IsBackupTrue)
                {
                    foreach (string backup in Backups)
                    {
                        Thread.Sleep(150);
                        bool check = false;
                        long sFile;
                        FileInfo temp = null;

                        try
                        {
                            var directory = new DirectoryInfo(backup);
                            foreach(string file in FileType)
                            {
                                var myFile = (from f in directory.GetFiles(file) orderby f.LastWriteTime descending select f).FirstOrDefault();

                                if(myFile != null)
                                {
                                    if (temp == null || myFile.CreationTime > temp.CreationTime)
                                        temp = myFile;
                                }

                            }
                            //var myFile = (from f in directory.GetFiles("*.rar") orderby f.LastWriteTime descending select f).FirstOrDefault();

                            if (temp == null)
                                {
                                    LiveLogs($"Не нашел ни одного файла в директории {backup}");
                                    check = false;
                                    CheckBackupStatus("Ошибка", "Нет бэкапов в директории " + backup, DateTime.Now, "", check);
                                    continue;
                                }
                            

                            sFile = temp.Length / 1024 / 1024;
                            check = true;
                            CheckBackupStatus(temp.Name, backup, temp.CreationTime, sFile.ToString() + " MB", check);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            Logger.WriteErrors($"Ошибка: {e.ToString()}");
                            LiveLogs($"Ошибка: Нет доступа по пути {backup}");
                            check = false;
                            CheckBackupStatus("Ошибка доступа!", backup, DateTime.Now, "-", check);
                        }
                        catch (Exception e)
                        {
                            Logger.WriteErrors($"Ошибка: {e.ToString()}");
                            LiveLogs($"Ошибка: {e}");
                            check = false;
                            CheckBackupStatus("Ошибка", backup, DateTime.Now, "-", check);
                        }
                    }
                    IsBackupTrue = false;
                    Action action = () => 
                    {
                        button3.Enabled = true;
                    };

                    invokeEx(action);
                    Inform();
                        
            }
        }

        private void CheckBackupStatus(string file, string path, DateTime date, string size, bool check)
        {
            DataGridViewCellStyle rowRed = new DataGridViewCellStyle();
            DataGridViewCellStyle rowYellow = new DataGridViewCellStyle();
            DataGridViewCellStyle rowGreen = new DataGridViewCellStyle();
            DataGridViewCellStyle rowGray = new DataGridViewCellStyle();

            rowRed.BackColor = Color.Red;
            rowYellow.BackColor = Color.Yellow;
            rowGreen.BackColor = Color.Green;
            rowGray.BackColor = Color.Gray;

            DateTime dtnow = DateTime.Now;
            DateTime dtToDay = DateTime.Today;
            DateTime WarningDate = DateTime.Today.AddDays(-(int)set.numericUpDown2.Value);
            DateTime VeryWarningDate = DateTime.Today.AddDays(-(int)set.numericUpDown1.Value);

            Action action = () =>
            {
                dataGridView2.Rows.Add(dtnow.ToString(), path, file, date, size);
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Cells[1].Value.ToString().Equals(path))
                    {
                        row.SetValues(dtnow.ToString(), path, file, date, size);
                        if (!check)
                                row.DefaultCellStyle = rowGray;
                        else if (WarningDate > VeryWarningDate && VeryWarningDate > date || size == "0 MB")
                            {
                                row.DefaultCellStyle = rowRed;
                                LiveLogs($"Обратите внимание на бэкап по пути {path} !!!");
                            } 
                        else if (WarningDate > date)
                            {
                                row.DefaultCellStyle = rowYellow;
                                LiveLogs($"Обратите внимание на бэкап по пути {path} !!!");
                            } 
                        else
                            row.DefaultCellStyle = rowGreen;
                    }
                }
                
            };


            invokeEx(action);

        }

        private void Inform()
        {
            count_on = 0;
            count_off = 0;
            countb_grey = 0;
            countb_green = 0;
            countb_red = 0;
            countb_yellow = 0;
            Action action = () =>
            {
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    if (dataGridView1.Rows[i].Cells["Cbool"].Value.ToString() == "True")
                        count_on++;
                    else
                        count_off++;
                }
                for (int b = 0; b < dataGridView2.RowCount; b++)
                {
                    if (dataGridView2.Rows[b].DefaultCellStyle.BackColor.Equals(Color.Gray))
                        countb_grey++;
                    if (dataGridView2.Rows[b].DefaultCellStyle.BackColor.Equals(Color.Red))
                        countb_red++;
                    if (dataGridView2.Rows[b].DefaultCellStyle.BackColor.Equals(Color.Green))
                        countb_green++;
                    if (dataGridView2.Rows[b].DefaultCellStyle.BackColor.Equals(Color.Yellow))
                        countb_yellow++;
                }

                label5.Text = count_on.ToString();
                label5.ForeColor = Color.Green;
                label6.Text = count_off.ToString();
                label6.ForeColor = Color.Red;
                label7.Text = sendmail.ToString();
                label10.Text = errormail.ToString();
                label15.Text = countbackup.ToString();
                label22.Text = countb_grey.ToString();
                label22.ForeColor = Color.Red;
                label14.Text = countb_yellow.ToString();
                label14.ForeColor = Color.IndianRed;
                label20.Text = countb_red.ToString();
                label20.ForeColor = Color.Red;
                label18.Text = countb_green.ToString();
                label18.ForeColor = Color.Green;
            };

            invokeEx(action);

        }

        private void SendMail(string message)
        {
            if (!set.checkBox1.Checked)
                return;
            try
            {
                MailAddress from = new MailAddress(set.textBox2.Text, "Оповещение!");
                MailAddress to = new MailAddress(Properties.Settings.Default.email);
                MailMessage m = new MailMessage(from, to);
                m.Subject = "Оповещение";
                m.Body = message;
                m.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient("smtp.mail.ru", 25);
                smtp.Credentials = new NetworkCredential(set.textBox2.Text, set.textBox3.Text);
                smtp.EnableSsl = true;
                smtp.Send(m);
                sendmail++;
                Logger.WriteLogs($"Сообщение отправлено!");
                LiveLogs("Сообщение отправлено!");
            }
            catch (SmtpException e)
            {
                Logger.WriteErrors($"Проблема с почтой: {e}");
                LiveLogs($"Ошибка при отправке уведомления:  {e}");
                errormail++;
                CheckDate = DateTime.Now;
            }

        }

        private void LiveLogs(string message)
        {
            Action action = () => {
                string date = DateTime.Now.ToString();
                richTextBox1.Text += $"{date}: {message}" + Environment.NewLine;
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            };
            invokeEx(action);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            set.ShowDialog();
            if(ThBackup.IsAlive)
                ThBackup.Abort();
            if (ThIp.IsAlive)
                ThIp.Abort();
            label12.Text = "Остановлена";
            label12.ForeColor = Color.Red;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (!set.checkBox1.Checked)
                MessageBox.Show("В настройка отключена галочка уведомлений!","Информация",MessageBoxButtons.OK,MessageBoxIcon.Information);
            else
                SendMail("Тест!");
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            button3.Enabled = false;
            dataGridView2.Rows.Clear();
            IsBackupTrue = true;
            BackupStatus();
            LiveLogs("Информация о бэкапах обновлена!");
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e) { Inform(); }
        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e) { Inform(); }

        private void Form1_Resize_1(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(1000);
            }
            else if (FormWindowState.Normal == this.WindowState)
            { notifyIcon1.Visible = false; }
        }

        private void notifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void invokeEx(Action action)
        {
                if (InvokeRequired)
                    Invoke(action);
                else
                    action();
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            dataGridView2.ClearSelection();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            BackupStatus();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            CheckIpStatus();
        }

        private void timer2_Tick_1(object sender, EventArgs e)
        {
            if(IsCheckedTimer2)
                CheckIpStatus();
        }
    }
}