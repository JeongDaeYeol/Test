using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace WindowsFormsApp2
{
    internal class Class1
    {
        private const int BAUDRATE = 9600;
        private const int DATABITS = 8;
        private const StopBits STOPBITS = StopBits.One;
        private const Parity PARITY = Parity.None;
        private const int READTIME = 500;

        private int m_portNo;
        private SerialPort m_sp;
        private bool m_connected;

        private Thread m_thread;
        private bool m_runState;
        private bool m_disposed;
        private bool m_kill;

        private string m_code;
        private DateTime m_time;
        private object m_lock;
        //private byte[] m_trgCmdDatas;
        


        private string m_strStart;
        private string m_strDelimieter;

        public Class1()
        {
            m_portNo = 0;
            m_connected = false;
            m_sp = null;

            m_runState = false;
            m_kill = false;
            m_code = "";

            m_lock = new object();
        }


        #region Connect
        public bool Connect(int port, int baudrate)
        {
            bool ret = false;
            ret = Connect(port,
                           baudrate,
                           DATABITS,
                           STOPBITS,
                           PARITY,
                           READTIME);

            return ret;
        }

        public bool Connect(int port,
                            int baud,
                            int dataBits,
                            StopBits stopBits,
                            Parity parity,
                            int readTime)
        {
            bool ret = false;

            //이미 연결되어있으면 바로 리턴
            if (m_connected == true) 
                return true;

            try
            {
                //시리얼포트 객체 생성
                if (m_sp == null)
                    m_sp = new SerialPort();
                else
                    m_sp.Close();

                //파라미터 설정
                m_sp.PortName = "COM" + Convert.ToString(port);
                m_sp.BaudRate = baud;
                m_sp.DataBits = dataBits;
                m_sp.StopBits = stopBits;
                m_sp.Parity = parity;
                m_sp.ReadTimeout = readTime;
                m_sp.DtrEnable = true; //직렬통신 on
                m_sp.NewLine = "\r";

                //m_strDelimieter가 빈문자열이 아니면
                //시리얼포트 NewLine에 넣어라
                if (string.IsNullOrEmpty(m_strDelimieter) == false)
                {
                    m_sp.NewLine = m_strDelimieter;
                }

                //포트 open
                //시리얼통신이 직열포트가 열려있으면
                if (m_sp.IsOpen == true) 
                {
                    m_sp.Close();
                }
                m_sp.Open();

                m_connected = true;
                m_portNo = port;

                m_code = "";
                m_time = DateTime.Now;
                if (StartThread() == false)
                {
                    throw new ApplicationException();
                }

                ret = true;

            }
            catch
            {
                Disconnect();
                ret = false;
            }
            return ret;
        }

        public void Disconnect()
        {
            KillThread();

            if (m_sp != null)
            {
                m_sp.Close();
                m_sp.Dispose();
            }
            m_sp = null;
            m_connected = false;
        }

        #endregion


        #region Read, Thread
        //입력 데이터 읽음
        private int ReadLine(out string buf)
        {
            buf = "";

            //connect안되어있으면 False
            if (m_connected == false)
            {
                return -1;
            }

            int ret = 0;

            try
            {
                buf = "";
                buf = m_sp.ReadLine(); //시리얼포트에서 입력버퍼 읽어옴
                ret = buf.Length;//읽어온 데이터 길이 ret에 저장

                m_sp.DiscardInBuffer(); // 수신버퍼 삭제

            }
            catch (TimeoutException e)
            {
                if (buf == "") // 입력데이터가 없으면 데이터 길이 0
                {
                    ret = 0;
                }
                else // 다시 입력버퍼 읽어오고 데이터 길이 저장
                {
                    string str = "";
                    str = m_sp.ReadLine();
                    buf += str;
                    ret = buf.Length;
                }
            }
            catch(InvalidOperationException e)
            {
                ret = -1;
            }
            return ret;
        }

        private void Func()
        {
            m_runState = true;

            string newCode = "";
            int r = 0;
            while(true)
            {
                if (m_kill == true)
                    break;

                newCode = "";
                r = ReadLine(out newCode);
                if (r > 0)
                {
                    //입력 데이터가 있으면 "" 초기화함
                    if (string.IsNullOrEmpty(m_strStart) == false)
                        newCode = newCode.Replace(m_strStart, "");

                    bool lockTaken = false;
                    if (m_lock != null)
                    {
                        Monitor.Enter(m_lock, ref lockTaken);
                    }

                    m_code = newCode;
                    m_time = DateTime.Now;

                    if ((m_lock != null) && (lockTaken == true))
                    {
                        Monitor.Exit(m_lock);
                    }

                }
                else if (r < 0)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            m_runState = false;
        }

        private void KillThread()
        {
            if (m_runState == false)
            {
                return;
            }

            m_kill = true;
            Thread.Sleep(150);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (m_runState == false)
                    break;
                if (sw.Elapsed.TotalMilliseconds > 2000)
                    break;

                Thread.Sleep(200);

            }
            sw.Stop();

            if (m_thread != null)
                m_thread.Abort();

            m_thread = null;
            m_runState = false;

            return;
        }

        private bool StartThread()
        {
            if (m_runState == true)
            {
                return true;
            }

            bool ret = false;
            try
            {
                m_thread = new Thread(Func);
                m_thread.IsBackground = true;
                m_thread.Start();
                ret = true;

                Thread.Sleep(100);

            }
            catch (Exception ex)
            {
                ret = false;

            }
            return ret;

        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                Disconnect();
                KillThread();

                m_disposed = true;
            }
        }

        public bool PopCode(out string code, out DateTime time)
        {
            code = "";
            time = DateTime.Now;

            //m_code가 비어있으면 popcode매서드 false
            if (m_code == "")
            {
                code = "";
                time = DateTime.Now;
                return false;
            }

            bool lockTaken = false;
            if (m_lock != null)
            {
                Monitor.Enter(m_lock, ref lockTaken);
            }

            code = m_code;
            time = m_time;

            m_code = String.Empty;

            if (m_lock != null && lockTaken == true)
            {
                Monitor.Exit(m_lock);
            }

            return true;
        }


        public void Init()
        {
            m_code = "";
            m_time = DateTime.Now;
        }

    }
}
