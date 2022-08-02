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
using System.IO.Ports;
using CsvHelper;

namespace motorMeasurementControl
{
    public partial class Form1 : Form
    {
        byte[] data = new byte[1];

        string com;
        SerialPort serial;
        int delay = 100;

        public Form1()
        {
            
            InitializeComponent();

            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            com = comboBox1.SelectedItem.ToString();
            serial = new SerialPort(com, 115200);
            connect.Enabled = true;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = trackBar1.Value.ToString();

            byte[] data = new byte[1];

            //byte[] Numbers = BitConverter.GetBytes(trackBar1.Value);

            byte pwm = Convert.ToByte(trackBar1.Value);

            data[0] = pwm;

            serial.Write(data, 0, 1);
        }

        private void connect_Click(object sender, EventArgs e)
        {
            try
            {
                if (connect.Text == "Connect")
                {
                    serial.Open();
                    connect.Text = "Disconnect";
                    
                }
                else //button3.text == "Disconnect";
                {
                    trackBar1.Value = 0;
                    trackBar1.Enabled = false;
                    manual.Enabled = false;
                    automatic.Enabled = false;
                    start.Enabled = false;
                    stop.Enabled = false;
                    manual.BackColor = Control.DefaultBackColor;
                    automatic.BackColor = Control.DefaultBackColor;
                    serial.Close();
                    
                    connect.Text = "Connect";
                }

                manual.Enabled = true;
                automatic.Enabled = true;
            }
            catch 
            {
                MessageBox.Show("No serial port detected / Serial port not available.");
            }
        }

        private void manual_Click(object sender, EventArgs e)
        {
            automatic.Enabled = false;
            start.Enabled = true;
            stop.Enabled = true;
            textBox1.Enabled = true;
            button1.Enabled = true;
            manual.BackColor = Color.Green;
        }

        async void start_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;

            start.Enabled = false ;

            serial.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);

            if (manual.BackColor == Color.Green)
            {
                trackBar1.Enabled = true;

            }
            else if (automatic.BackColor == Color.Green)
            {
                

                for (int i = 0; i < 100; i++)
                {
                    if (automatic.BackColor == Color.Green)
                    {
                        if (textBox2.Text.Length > 0)
                        {
                            delay = int.Parse(textBox2.Text);
                        }

                        textBox1.Text = i.ToString();
                        byte[] data = new byte[1];
                        data[0] = Convert.ToByte(i);
                        serial.Write(data, 0, 1);

                        await Task.Delay(delay);
                    }
                }

                for (int j = 100; j >= 0; j--)
                {
                    if (automatic.BackColor == Color.Green)
                    {
                        if (textBox2.Text.Length > 0)
                        {
                            delay = int.Parse(textBox2.Text);
                        }
                        textBox1.Text = j.ToString();
                        byte[] data = new byte[1]; 
                        data[0] = Convert.ToByte(j);
                        serial.Write(data, 0, 1);

                        await Task.Delay(delay);
                    }
                }

                automatic.BackColor = Control.DefaultBackColor;
            } 
        }

        private void automatic_Click(object sender, EventArgs e)
        {
            automatic.BackColor = Color.Green;
            manual.Enabled = false;
            start.Enabled = true;
            stop.Enabled = true;
            textBox2.Enabled = true;
        }

        private void stop_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[1];
            data[0] = Convert.ToByte(0);
            serial.Write(data, 0, 1);

            serial.DataReceived -= serial_DataReceived;

            trackBar1.Value = 0;
            textBox1.Text = trackBar1.Value.ToString();

            trackBar1.Enabled = false;
            start.Enabled = false;
            button1.Enabled = false;

            automatic.BackColor = Control.DefaultBackColor;
            manual.BackColor = Control.DefaultBackColor;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            trackBar1.Value = int.Parse(textBox1.Text);

            if (trackBar1.Enabled == true)
            {
                byte[] data = new byte[1];
                data[0] = Convert.ToByte(int.Parse(textBox1.Text));
                serial.Write(data, 0, 1);
            }
            else
            {
                byte[] data = new byte[1];
                data[0] = Convert.ToByte(int.Parse(textBox1.Text));
                serial.Write(data, 0, 1);
            }
            
        }

        void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // this the read buffer
            byte[] buff = new byte[30000];
            int readByteCount = serial.BaseStream.Read(buff, 0, serial.BytesToRead);
            // you can specify other encodings, or use default
            string response = System.Text.Encoding.UTF8.GetString(buff);

            // you need to implement AppendToFile ;)
            //
            AppendToFile(String.Format("response :{0}", response));

            // Or, just send sp.ReadExisting();
            if (serial.ReadExisting() != null)
            {
                AppendToFile(serial.ReadExisting());
            }
        }

        private void AppendToFile(string toAppend)
        {
            string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string myFile = "\\motor.txt";
            File.AppendAllText(myDocumentsPath+myFile, toAppend + Environment.NewLine);
        }

       /* private void LogTxt()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filename = "\\motor.txt";
            // This text is added only once to the file.
            if (!File.Exists(path + filename))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path+filename))
                {
                    sw.Write("Hello ");
                    sw.Write("And ");
                    sw.WriteLine("Welcome");
                }
            }

            using (StreamWriter sw = File.AppendText(path+filename))
            {
                serial.ReadExisting();
            }
        }*/
    }
}
