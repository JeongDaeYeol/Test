using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private Class1 m_bcdRdr;
        private Thread m_thread;
        private bool m_runState;
        private bool m_kill;
        private bool m_concheck;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_bcdRdr = new Class1();
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            int port = 0;
            port = Convert.ToInt32(textBox1.Text);
            int baudrate = Convert.ToInt32(textBox2.Text);
            if (m_bcdRdr.Connect(port, baudrate) == true)
            {
                MessageBox.Show("Connect");
                m_concheck = true;
                button1.BackColor = Color.Lime;
            }
            else
            {
                MessageBox.Show("Connect Failed");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            m_bcdRdr.Disconnect();
            if (button1.BackColor == Color.Lime)
            {
                MessageBox.Show("Disconnect");
                button1.BackColor = Color.Gainsboro;
                button3.BackColor = Color.Gainsboro;
                m_concheck = false;
            }
            
        }


        private void Func()
        {
            Action<string> appMsg = AppendMessage;

            m_runState = true;
            m_bcdRdr.Init();

            string code = "";
            DateTime time;
            string temp = "";
            while (true)
            {
                if (m_kill == true)
                    break;

                if (m_bcdRdr.PopCode(out code, out time) == true)
                {
                    temp = "";
                    temp = "[";
                    temp = String.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}",
                            time.Year,
                            time.Minute,
                            time.Day,
                            time.Hour,
                            time.Minute,
                            time.Second);
                    temp += "]";
                    temp += code;
                    temp += "\n\r";
                    Invoke(appMsg, temp);

                    m_runState = true;
                }

                Thread.Sleep(200);
            }

            m_runState = false;
            m_kill = false;
            m_bcdRdr.Init();

        }

        private void AppendMessage(string msg)
        {
            textBox3.AppendText(msg + "\n");
        }

        private bool StartThread()
        {
            if (m_runState == true)
                return true;

            m_thread = new Thread(Func);
            m_thread.Start();

            return true;
        }

        private void KillThread()
        {
            if (m_runState ==false)
            {
                return;
            }

            m_kill=true;
            Thread.Sleep(50);

            if (m_thread != null)
                m_thread.Join();
            m_thread = null;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            KillThread();
            button3.BackColor = Color.Gainsboro;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (m_concheck == true)
            {
                button3.BackColor = Color.Lime;
                StartThread();
            }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillThread();
            if (m_bcdRdr != null)
                m_bcdRdr.Disconnect();
        }
    }
}
