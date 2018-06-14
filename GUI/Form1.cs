using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace GUI
{
    public partial class Form1 : Form
    {
        ConnectToConsole CoonnectLocale;
        bool GlobalConnectStatus;
        public Form1()
        {
            InitializeComponent();
            this.panel2.Hide();
            this.panel4.Hide();
            this.textBox14.Enabled = false;
        }

        private void RemoveTabControl(string Level, string Login, bool test)
        {
            // удаление вкладок которые не доступны для данной роли
            if (Level == "Студент-Аспирант" && test == true)
            {
                this.textBox8.Text = Level;
                this.textBox8.Enabled = false;
            }

            if (Level == "Аспирант" && test == true)
            {
                this.textBox8.Text = Level;
                this.textBox8.Enabled = false;
            }

            if (Level == "ППС (не в УСУ)" && test == true)
            {
                this.textBox8.Text = Level;
                this.textBox8.Enabled = false;
            }

            if (Level == "ППС (в УСУ)" && test == true)
            {
                this.textBox8.Text = Level;
                this.textBox8.Enabled = false;
            }


            if(Level == "Администратор" && test == true)
            {
                // блокировка полей персональных данных
                this.textBox14.Enabled = false;
                this.textBox27.Enabled = false;
                this.textBox26.Enabled = false;
                this.textBox25.Enabled = false;
                this.textBox23.Enabled = false;
                this.textBox24.Enabled = false;
                // отключение кнопки изменить
                this.button14.Enabled = false;
                this.textBox14.Text = Login;
                tabControl1.TabPages.Remove(tabPage37);
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage5);
            }

            if (Level != "Студ. ИК" && test == true)
            {
                tabControl1.TabPages.Remove(tabPage6);
            }

            if (Level != "ИК ППС" && test == true)
            {
                tabControl1.TabPages.Remove(tabPage7);
            }

            if (Level != "Аудиторы" && test == true)
            {
                tabControl1.TabPages.Remove(tabPage8);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Кнопка обзора
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Файл ключа (*.pemx) | *.pemx";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    this.textBox1.Text = openFileDialog.FileName;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //Поле ввода-вывода пути
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // панель 1 - на ней вход
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            // панель 2
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Кенопка входа
            bool StatusLoaad = false;
            string PersonalData = "";
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("Пожалуйста выберите ключ", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    CoonnectLocale = new ConnectToConsole();
                    GlobalConnectStatus = true;
                }
                catch (Exception ex)
                {
                    GlobalConnectStatus = false;
                }

                if (GlobalConnectStatus) // если подключение успешно установленно
                {
                    // читаем файл путь которого получили при помощи openFileDialog (из textBox1)
                    String line, BufferLine = "";
                    StreamReader Reader = new StreamReader(this.textBox1.Text);

                    line = Reader.ReadLine();

                    while (line != null)
                    {
                        BufferLine += line;
                        line = Reader.ReadLine();
                    }

                    Reader.Close();

                    // приводим полученный текст к кодировке UTF8
                    byte[] msg = Encoding.UTF8.GetBytes(BufferLine);
                    int len = msg.Length;

                    string ReceiveMessage = CoonnectLocale.SendMessage("keys:" + len.ToString());

                    if (ReceiveMessage == "ok")
                    {
                        PersonalData = CoonnectLocale.SendMessage(BufferLine);
                        StatusLoaad = true;
                    }
                }
                else
                {
                    MessageBox.Show("Консоль не запущенна", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

            // если все проверки прошли удачно отображаем оболочку
            if(StatusLoaad)
            {
                string[] PersonalArray = Regex.Split(PersonalData, ":");
                RemoveTabControl(PersonalArray[1], PersonalArray[0], true);
                this.panel2.Show();
            }


        }

        private void button4_Click(object sender, EventArgs e)
        {
            // кнопка для отправки формы востановления ключа
            this.panel4.Show();
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            // панель востановления
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            // кнопка для скрытия формы отправки
            this.panel4.Hide();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            // кнопка востановления ключа у администратора
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Файл ключа pemx|*.pemx";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("123");
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // действия при нажатии кнопки для отправки формы (востановить)
            MessageBox.Show("Заявка отправлина. Для получения нового ключа обратитесь к администратору.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void button10_Click(object sender, EventArgs e)
        {

            string NewUser = string.Format("NewUser:Surname:{0}:Name:{1}:SecondName:{2}:Serial:{3}:Number:{4}", this.textBox7.Text, 
                                                                       this.textBox9.Text, this.textBox10.Text, this.textBox12.Text, this.textBox11.Text);

            // приводим полученный текст к кодировке UTF8
            byte[] msg = Encoding.UTF8.GetBytes(NewUser);
            int len = msg.Length;

            string ReceiveMessage = CoonnectLocale.SendMessage("NewUser:" + len.ToString());

            //if (ReceiveMessage == "ok")
            //{
                ReceiveMessage = CoonnectLocale.SendMessage(NewUser);
                byte[] key_bytes = new byte[Convert.ToInt32(ReceiveMessage)];

                ReceiveMessage = CoonnectLocale.SendMessage("ok");

                // кнопка создания пользователя
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Файл ключа pemx|*.pemx";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        sw.WriteLine(ReceiveMessage);
                    }
                }

            //}
            //else
            //{
            //    MessageBox.Show("Упссс... Что-то пошло не так...", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

        }

        private void tabPage38_Click(object sender, EventArgs e)
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox18_Enter(object sender, EventArgs e)
        {

        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "Студент-аспирант";
        }

        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "Аспирант";
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "ППС (не в УСУ)";
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "ППС (в УСУ)";
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "Администратор";
        }

        private void radioButton21_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "Студент ИК";
        }

        private void radioButton22_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "ИК ППС";
        }

        private void radioButton23_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox8.Text = "Аудитор";
        }

        private void tabPage18_Click(object sender, EventArgs e)
        {

        }

        private void tabPage21_Click(object sender, EventArgs e)
        {

        }

        private void groupBox16_Enter(object sender, EventArgs e)
        {

        }
    }
}
