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
using System.Threading;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using CsvHelper.Configuration;

//Basically we send PWM values to the serial port (Teensy) via trackbar value.
//Read the data Teensy writes to serial (strain gauges, pwm, timestamp).
//Save data to .csv, save chart as .png
//Applied test program will run for 8 hours, each section for 1 hour (cycles), normal (1400-1700ms PWM) and high (1200-2000ms PWM) load alternating

namespace motorMeasurementControl
{
    public partial class Form1 : Form
    {
        string com; // serial port
        SerialPort serial; // serial port instance
        int delay = 1; //for automatic ramp

        bool newFile = true; //if we start a new file (to write csv header), in serial_DataReceived, changed in stop_click

        int fileNumber = 1; // for creating new filename (only while application is running, restart -> 1 again), in serial_DataReceived, changed in stop_click

        List<Data> dataList; // to store .csv data for drawing chart

        string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); //to save to Documents folder
        string myFile; //for individual filenames (only if application was not closed)

        //read data for chart
        Series elso1 = new Series();
        Series elso2 = new Series();
        Series elso3 = new Series();
        Series elso4 = new Series();
        Series thrust = new Series();
        Series oldalso1 = new Series();
        Series oldalso2 = new Series();
        Series pwm = new Series();

        //counter for .csv rows to not read from beginning again
        int k = 0;

        int tick = 0; //To control test cycles

        int sawtoothtick = 0; // To control sawtooth period cycles
                
        public Form1()
        {
            InitializeComponent();

            //list available COM ports on device
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //bind dataList read from .csv to chart
            chart.DataSource = dataList;

            chart.Series.Clear();//clear default series of chart control
            chart.ChartAreas.Clear();
            //add series for data to draw, customize display
            chart.Series.Add(elso1);
            chart.Series.Add(elso2);
            chart.Series.Add(elso3);
            chart.Series.Add(elso4);
            chart.Series.Add(thrust);
            chart.Series.Add(oldalso1);
            chart.Series.Add(oldalso2);
            chart.Series.Add(pwm);

            //Create chart areas
            ChartArea pullGraph = new ChartArea("Pull");
            ChartArea thrustGraph = new ChartArea("Thrust");
            ChartArea torqueGraph = new ChartArea("Torque");
            ChartArea pwmGraph = new ChartArea("PWM");

            chart.ChartAreas.Add(pullGraph);
            chart.ChartAreas.Add(thrustGraph);
            chart.ChartAreas.Add(torqueGraph);
            chart.ChartAreas.Add(pwmGraph);

            elso1.Name = "Elso1 [N]";
            elso2.Name = "Elso2 [N]";
            elso3.Name = "Elso3 [N]";
            elso4.Name = "Elso4 [N]";
            oldalso1.Name = "Oldalso1 [Nm]";
            oldalso2.Name = "Oldalso2 [Nm]";
            pwm.Name = "PWM [microsec]";
            thrust.Name = "Thrust [N]";

            elso1.ChartType = SeriesChartType.Line;
            elso2.ChartType = SeriesChartType.Line;
            elso3.ChartType = SeriesChartType.Line;
            elso4.ChartType = SeriesChartType.Line;
            oldalso1.ChartType = SeriesChartType.Line;
            oldalso2.ChartType = SeriesChartType.Line;
            pwm.ChartType = SeriesChartType.Line;
            thrust.ChartType = SeriesChartType.Line;

            elso1.BorderWidth = 3;
            elso2.BorderWidth = 3;
            elso3.BorderWidth = 3;
            elso4.BorderWidth = 3;
            pwm.BorderWidth = 5;
            oldalso1.BorderWidth = 5;
            oldalso2.BorderWidth = 5;
            thrust.BorderWidth = 5;

            chart.Series[0].ChartArea = "Pull";
            chart.Series[1].ChartArea = "Pull";
            chart.Series[2].ChartArea = "Pull";
            chart.Series[3].ChartArea = "Pull";
            chart.Series[4].ChartArea = "Thrust";
            chart.Series[5].ChartArea = "Torque";
            chart.Series[6].ChartArea = "Torque";
            chart.Series[7].ChartArea = "PWM";

            chart.Titles.Add("Pull_indiv [N]");
            chart.Titles.Add("Thrust [N]");
            chart.Titles.Add("Torque [Nm]");
            chart.Titles.Add("PWM [microsec]");

            chart.Titles[0].DockedToChartArea = "Pull";
            chart.Titles[1].DockedToChartArea = "Thrust";
            chart.Titles[2].DockedToChartArea = "Torque";
            chart.Titles[3].DockedToChartArea = "PWM";

            //instructions
            label9.Text = " V2 \nAfter closing application, remove .csv and .png files from \nDocument folder \nStart - start logging \nStop - stop logging, save chart \nGetac tablet - COM20 \nEngage switch on Teensy box for values, disengage \nafter closing application";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) 
        {
            //choose selected com port, and instance new serial object, enable connect button
            com = comboBox1.SelectedItem.ToString();
            serial = new SerialPort(com, 115200);
            connect.Enabled = true;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //send trackbar value (PWM) to serial port for Teensy 
            textBox1.Text = trackBar1.Value.ToString();

            byte[] data = new byte[1];

            byte pwm = Convert.ToByte(trackBar1.Value);

            data[0] = pwm;

            serial.Write(data, 0, 1);
        }

        private void connect_Click(object sender, EventArgs e)
        {
            //to connect to serial port, or disconnect (disable every other button, textbox)
            try
            {
                if (connect.Text == "Connect")
                {
                    serial.Open();
                    serial.ReadTimeout = 1000;
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
                    serial.DiscardOutBuffer();
                    serial.Close();
                    //stop logging
                    serial.DataReceived -= serial_DataReceived;

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
            //for manual input of trackbar value (PWM), scroll/numerical input
            //set button color to step into if branch in start_click
            //enable necessary controls, disable automatic mode
            automatic.Enabled = false;
            start.Enabled = true;
            stop.Enabled = true;
            textBox1.Enabled = true;
            button1.Enabled = true;
            manual.BackColor = Color.Green;
        }

        private void automatic_Click(object sender, EventArgs e)
        {
            //change button color (to step into else branch in start_click)
            //enable necessary controls, disable manual mode
            automatic.BackColor = Color.Green;
            manual.Enabled = false;
            start.Enabled = true;
            stop.Enabled = true;
            textBox2.Enabled = true;
        }

        async void start_Click(object sender, EventArgs e)
        {
            //start measurement, enable trackbar (enable sending value to serial)
            //start logging session with SerialDataReceived event
            //if-else watches operation mode (manual/automatic) button color, which is changed upon click
            button1.Enabled = true;

            start.Enabled = false;

            serial.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);

            

            if (manual.BackColor == Color.Green)
            {
                trackBar1.Enabled = true;

            }
            else if (automatic.BackColor == Color.Green)
            {
                timer1.Start();//for test cycles (1h)

                byte[] data = new byte[1];

                byte pwm;

                double x = 0;
                while (tick < 1)//while timer1 not reached one hour (interval 3600000) -> sinus fnc output
                {
                    pwm = Convert.ToByte(SineRamp(x, 20, 40));
                    data[0] = pwm;
                    serial.Write(data, 0, 1);
                    x += 0.01;
                    await Task.Delay(delay);
                }
                timer2.Start();
                timer2.Interval = 4000;
                x = 0;
                while (tick < 2)//while timer1 not reached two hours (interval 3600000) -> sawtooth fnc output
                {
                    //for sawtooth upramp period 
                    if (sawtoothtick != 1)
                    {
                        pwm = Convert.ToByte(Sawtooth(x, 30, 80, timer2.Interval*0.001));
                        data[0] = pwm;
                        serial.Write(data, 0, 1);
                        x += 0.01;
                        await Task.Delay(delay);
                    }
                    else
                    {
                        x = 0;
                        sawtoothtick = 0;
                    }
                }
                while (tick < 4)//while timer1 not reached one hour (interval 3600000) -> sinus fnc output
                {
                    timer2.Stop();
                    pwm = Convert.ToByte(SineRamp(x, 30, 40));
                    data[0] = pwm;
                    serial.Write(data, 0, 1);
                    x += 0.01;
                    await Task.Delay(delay);
                }
                timer2.Start();
                timer2.Interval = 5000;
                x = 0;
                sawtoothtick = 0;
                while (tick < 5)//while timer1 not reached two hours (interval 3600000) -> sawtooth fnc output
                {
                    //for sawtooth upramp period 
                    if (sawtoothtick != 1)
                    {
                        pwm = Convert.ToByte(Sawtooth(x, 20, 100, timer2.Interval * 0.001));
                        data[0] = pwm;
                        serial.Write(data, 0, 1);
                        x += 0.01;
                        await Task.Delay(delay);
                    }
                    else
                    {
                        x = 0;
                        sawtoothtick = 0;
                    }
                }
                while (tick < 6)//while timer1 not reached one hour (interval 3600000) -> sinus fnc output
                {
                    timer2.Stop();
                    pwm = Convert.ToByte(SineRamp(x, 40, 40));
                    data[0] = pwm;
                    serial.Write(data, 0, 1);
                    x += 0.01;
                    await Task.Delay(delay);
                }
                timer2.Start();
                timer2.Interval = 5000;
                x = 0;
                sawtoothtick = 0;
                while (tick < 7)//while timer1 not reached two hours (interval 3600000) -> sawtooth fnc output
                {
                    //for sawtooth upramp period 
                    if (sawtoothtick != 1)
                    {
                        pwm = Convert.ToByte(Sawtooth(x, 20, 100, timer2.Interval * 0.001));
                        data[0] = pwm;
                        serial.Write(data, 0, 1);
                        x += 0.01;
                        await Task.Delay(delay);
                    }
                    else
                    {
                        x = 0;
                        sawtoothtick = 0;
                    }
                }
                while (tick < 8)//while timer1 not reached one hour (interval 3600000) -> sinus fnc output
                {
                    timer2.Stop();
                    pwm = Convert.ToByte(SineRamp(x, 30, 60));
                    data[0] = pwm;
                    serial.Write(data, 0, 1);
                    x += 0.01;
                    await Task.Delay(delay);
                }
                /*while (automatic.BackColor == Color.Green)
                {
                    switch (tick)
                    {
                        case 0://normal sine load (1400-1600 ms PWM)
                            pwm = Convert.ToByte(SineRamp(x, 20, 40));
                            data[0] = pwm;
                            break;
                        case 1://high sawtooth load (1300-1800 ms PWM)

                        case 2://normal sine load (1400-1700 ms PWM)
                            pwm = Convert.ToByte(SineRamp(x, 30, 40));
                            data[0] = pwm;
                            break;
                        case 3://normal sine load (1400-1700 ms PWM)
                            pwm = Convert.ToByte(SineRamp(x, 30, 40));
                            data[0] = pwm;
                            break;
                        case 4://high sawtooth load (1200-2000 ms PWM)
                        case 5://normal sine load (1400 - 1800 ms PWM)
                            pwm = Convert.ToByte(SineRamp(x, 40, 40));
                            data[0] = pwm;
                            break;
                        case 6:// high sawtooth load (1200 - 2000 ms PWM)
                        case 7://high sine load (1300 - 1800 ms PWM)
                            pwm = Convert.ToByte(SineRamp(x, 50, 30));
                            data[0] = pwm;
                            break;
                    }

                    serial.Write(data, 0, 1);

                    x += 0.01;

                    await Task.Delay(delay);
                }*/



                pwm = Convert.ToByte(0);
                data[0] = pwm;
                serial.Write(data, 0, 1);
            }
            /*else if (automatic.BackColor == Color.Green)
            {


                for (int i = 0; i < 100; i++) // to sweep trackbar values towards 100
                {
                    if (automatic.BackColor == Color.Green)
                    {
                        if (textBox2.Text.Length > 0) 
                        {
                            delay = int.Parse(textBox2.Text); // set ramping speed  (time delay between changing values)
                        }

                        textBox1.Text = i.ToString();
                        byte[] data = new byte[1];
                        data[0] = Convert.ToByte(i);
                        serial.Write(data, 0, 1);

                        await Task.Delay(delay);
                    }
                }

                for (int j = 100; j >= 0; j--) //to sweep trackbar values towards 0
                {
                    if (automatic.BackColor == Color.Green)
                    {
                        if (textBox2.Text.Length > 0)
                        {
                            delay = int.Parse(textBox2.Text);//set ramping speed (time delay between changing values)
                        }
                        textBox1.Text = j.ToString();
                        byte[] data = new byte[1];
                        data[0] = Convert.ToByte(j);
                        serial.Write(data, 0, 1);

                        await Task.Delay(delay);
                    }
                }

                automatic.BackColor = Control.DefaultBackColor; // changing back button color, stop process
            }*/
        }

        private void stop_Click(object sender, EventArgs e)
        { 
            //send 0 PWM to teensy
            byte[] data = new byte[1];
            data[0] = Convert.ToByte(0);
            serial.Write(data, 0, 1);

            //reset trackbar value
            trackBar1.Value = 0;
            textBox1.Text = trackBar1.Value.ToString();

            //reset necessary controls
            trackBar1.Enabled = false;
            start.Enabled = false;
            button1.Enabled = false;
            manual.Enabled = true;
            automatic.Enabled = true;
            automatic.BackColor = Control.DefaultBackColor;
            manual.BackColor = Control.DefaultBackColor;

            //save drawn chart
            chart.SaveImage(myDocumentsPath + "\\motor_" + fileNumber.ToString() + String.Format("{0:yyyy_MM_dd}", DateTime.Now) + "_chart", ChartImageFormat.Png);

            //for unique filename, enable writin new header (in serial_DataReceived event)
            fileNumber++;
            newFile = true;

            serial.DiscardOutBuffer();

            tick = 10;

            data = new byte[1];
            data[0] = Convert.ToByte(102);
            serial.Write(data, 0, 1);
        }

        private void button1_Click(object sender, EventArgs e)//Write button
        {
            //write desired PWM manually into textbox
            trackBar1.Value = int.Parse(textBox1.Text);

            byte[] data = new byte[1];
            data[0] = Convert.ToByte(int.Parse(textBox1.Text));
            serial.Write(data, 0, 1);
        }

        void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //event to recognize available data on serial port (Teensy send)
            
            if (newFile) //write header in .csv
            {
                AppendToFile("Timestamp,PWM,Elso1,Elso2,Elso3,Elso4,Oldalso1,Oldalso2,Current");
                newFile = false;
            }

            AppendToFile(serial.ReadExisting().ToString()); // method to write data

            LoadDataFromCsv(); // method to read .csv fro chart drawing, immediately after reading
        }

        private void AppendToFile(string toAppend)
        {
            //append text to folder on defined path
            myFile = "\\motor_" + fileNumber.ToString() +String.Format("{0:yyyy_MM_dd}", DateTime.Now)+ ".csv";

            File.AppendAllText(myDocumentsPath + myFile, toAppend + Environment.NewLine);

        }

        public void LoadDataFromCsv()
        {
          //to load data from .csv for chart drawing
            string filepath = myDocumentsPath + myFile;

            using (var reader = new StreamReader(filepath))
                
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                try
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                        
                    dataList = csvReader.GetRecords<Data>().ToList();
                    int i = dataList.Count;
                        

                    for (int j = k; j < i; j++) //put read data to appropriate series for chart, X is always timestamp
                    {
                        if (chart.InvokeRequired)
                        {
                            chart.Invoke(new Action(() =>
                            {
                                if (elso1.Points.Count() > 100)
                                {
                                    elso1.Points.RemoveAt(0);
                                    elso2.Points.RemoveAt(0);
                                    elso3.Points.RemoveAt(0);
                                    elso4.Points.RemoveAt(0);
                                    thrust.Points.RemoveAt(0);
                                    oldalso1.Points.RemoveAt(0);
                                    oldalso2.Points.RemoveAt(0);
                                    pwm.Points.RemoveAt(0);

                                    chart.ResetAutoValues();
                                }


                                elso1.Points.AddXY(dataList[j].Timestamp, dataList[j].Elso1*9.81);
                                elso2.Points.AddXY(dataList[j].Timestamp, dataList[j].Elso2 * 9.81);
                                elso3.Points.AddXY(dataList[j].Timestamp,dataList[j].Elso3 *9.81);
                                elso4.Points.AddXY(dataList[j].Timestamp, dataList[j].Elso4 *9.81);
                                thrust.Points.AddXY(dataList[j].Timestamp, (dataList[j].Elso1 + dataList[j].Elso2 + dataList[j].Elso3 + dataList[j].Elso4)*9.81);
                                oldalso1.Points.AddXY(dataList[j].Timestamp, dataList[j].Oldalso1 *9.81 * 0.0475);
                                oldalso2.Points.AddXY(dataList[j].Timestamp, dataList[j].Oldalso2 * 9.81* 0.0475);
                                pwm.Points.AddXY(dataList[j].Timestamp, dataList[j].PWM);

                                chart.DataBind();

                            }
                            ));
                        }

                    }
                    k = i;
                }
                catch (CsvHelper.HeaderValidationException exception)
                {
                    Console.WriteLine(exception);
                }
            }    
        }

        private void tare_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[1];
            data[0] = Convert.ToByte(101);
            serial.Write(data, 0, 1);
        }

        private double SineRamp(double x, int amplitude, int min)//intervals, borders
        {
            double pwmoutput = amplitude*Math.Sin(1.5*x-Math.PI/2)+min;

            return pwmoutput;
        }

        private double Sawtooth(double x, int ymin, int ymax, double period)//intervals, borders, slope
        {
            double pwmout = ymin + ((ymax - ymin) / period) * x;

            return pwmout;
        }

        private void timer1_Tick(object sender, EventArgs e)//To alternate cycles in test program
        {
            tick++;
        }

        private void timer2_Tick(object sender, EventArgs e)//for sawtooth period time
        {
            sawtoothtick++;
        }
    }
}





