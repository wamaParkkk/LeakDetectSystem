using Automation.BDaq;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LeakDetectSystem
{
    enum DigitalValue
    {
        Off = 0,
        On = 1
    }

    enum DO_NAME
    {        
        SignalRed = 0,
        SignalGreen = 1,
        Buzzer = 2,
        SignalYellow = 3        
    }
    
    public partial class MainForm : Form
    {
        string filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\"));

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        bool bBuzzerUsed = false;
        bool bBuzzerClick = false;
        private Label[] m_InputBox;
        public bool[] bInputData = new bool[16];

        public MainForm()
        {
            InitializeComponent();

            // IO init
            instantDiCtrl1.SelectedDevice = new DeviceInformation("USB-4750,BID#0");
            instantDoCtrl1.SelectedDevice = new DeviceInformation("USB-4750,BID#0");            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            m_InputBox = new Label[18] {
                labelInput1, labelInput2, labelInput3, labelInput4, labelInput5, labelInput6, labelInput7, labelInput8,
                labelInput9, labelInput10, labelInput11, labelInput12, labelInput13, labelInput14, labelInput15, labelInput16,
                labelInput17, labelInput18
            };

            PARAMETER_LOAD();

            displayTimer.Enabled = true;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            displayTimer.Enabled = false;

            Dispose();            
            Application.ExitThread();
            Environment.Exit(0);
        }

        private void PARAMETER_LOAD()
        {
            StringBuilder sbOption = new StringBuilder();
            GetPrivateProfileString("Setting", "Buzzer", "", sbOption, sbOption.Capacity, string.Format("{0}{1}", filePath, "Option.ini"));
            bBuzzerUsed = Convert.ToBoolean(sbOption.ToString());

            checkBoxBuzzerUsed.Checked = bBuzzerUsed;
        }

        private void displayTimer_Tick(object sender, EventArgs e)
        {
            byte[] portData = new byte[8];

            // IO read
            instantDiCtrl1.Read(0, 8, portData);

            for (int nNum = 0; nNum < 2; nNum++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int nflag = (portData[nNum] >> i) & 0x1;

                    if (nflag == 0)
                        bInputData[(nNum * 8) + i] = true;                        
                    else
                        bInputData[(nNum * 8) + i] = false;                                            
                }                
            }   
            
            for (int i = 0; i < 16; i++)
            {
                if (bInputData[i])
                {
                    if ((i >= 0) && (i <= 13))
                        m_InputBox[i].BackColor = Color.Silver;

                    // Slot no 15, 16은 DI15채널 공유
                    if (i == 14)
                    {
                        if (bInputData[14])
                        {
                            m_InputBox[14].BackColor = Color.Silver;
                            m_InputBox[15].BackColor = Color.Silver;
                        }                            
                    }

                    // Slot no 17, 18은 DI16채널 공유
                    if (i == 15)
                    {
                        if (bInputData[15])
                        {
                            m_InputBox[16].BackColor = Color.Silver;
                            m_InputBox[17].BackColor = Color.Silver;
                        }                            
                    }                                       
                }
                else
                {
                    if ((i >= 0) && (i <= 13))
                        m_InputBox[i].BackColor = Color.Red;
                    
                    if (i == 14)
                    {
                        m_InputBox[14].BackColor = Color.Red;
                        m_InputBox[15].BackColor = Color.Red;
                    }

                    if (i == 15)
                    {
                        m_InputBox[16].BackColor = Color.Red;
                        m_InputBox[17].BackColor = Color.Red;
                    }                                     
                }
            }

            if ((!bInputData[0]) || (!bInputData[1]) || (!bInputData[2]) || (!bInputData[3]) || (!bInputData[4]) || (!bInputData[5]) ||
                (!bInputData[6]) || (!bInputData[7]) || (!bInputData[8]) || (!bInputData[9]) || (!bInputData[10]) || (!bInputData[11]) ||
                (!bInputData[12]) || (!bInputData[13]) || (!bInputData[14]) || (!bInputData[15]))
            {
                // IO write
                instantDoCtrl1.WriteBit(0, (int)DO_NAME.SignalRed, (byte)DigitalValue.On);                                
                instantDoCtrl1.WriteBit(0, (int)DO_NAME.SignalGreen, (byte)DigitalValue.Off);

                if ((bBuzzerUsed) && (!bBuzzerClick))
                    instantDoCtrl1.WriteBit(0, (int)DO_NAME.Buzzer, (byte)DigitalValue.On);                

                // 창 띄우고, 메세지 Show
                if (WindowState != FormWindowState.Normal)
                {
                    WindowState = FormWindowState.Normal;
                    Top = 300;
                    Left = 300;
                }

                if (panelMsg.Visible)
                    panelMsg.Visible = false;
                else
                    panelMsg.Visible = true;
            }
            else
            {                
                if (panelMsg.Visible != false)
                    panelMsg.Visible = false;

                instantDoCtrl1.WriteBit(0, (int)DO_NAME.SignalRed, (byte)DigitalValue.Off);                
                instantDoCtrl1.WriteBit(0, (int)DO_NAME.SignalGreen, (byte)DigitalValue.On);

                instantDoCtrl1.WriteBit(0, (int)DO_NAME.Buzzer, (byte)DigitalValue.Off);

                if (bBuzzerClick != false)
                    bBuzzerClick = false;
            }
        }

        private void checkBoxBuzzerUsed_Click(object sender, EventArgs e)
        {
            if (checkBoxBuzzerUsed.Checked)
                bBuzzerUsed = true;
            else
                bBuzzerUsed = false;

            WritePrivateProfileString("Setting", "Buzzer", bBuzzerUsed.ToString(), string.Format("{0}{1}", filePath, "Option.ini"));
        }

        private void btnBuzzerOff_Click(object sender, EventArgs e)
        {
            instantDoCtrl1.WriteBit(0, (int)DO_NAME.Buzzer, (byte)DigitalValue.Off);
            bBuzzerClick = true;
        }
    }
}
