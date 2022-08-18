using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace motorMeasurementControl
{
    class Data
    {
        private double timestamp;

        private double pwm;

        private double elso1;

        private double elso2;

        private double elso3;

        private double elso4;

        private double oldalso1;

        private double oldalso2;

        //private double current;

        //public static Chart chart_ { get; set; }

        public double Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public double PWM
        {
            get { return pwm; }
            set { pwm = value; }
        }

        public double Elso1
        {
            get { return elso1; }
            set { elso1= value; }
        }

        public double Elso2
        {
            get { return elso2; }
            set { elso2 = value; }
        }

        public double Elso3
        {
            get { return elso3; }
            set { elso3 = value; }
        }

        public double Elso4
        {
            get { return elso4; }
            set { elso4 = value; }
        }

        public double Oldalso1
        {
            get { return oldalso1; }
            set { oldalso1 = value; }
        }

        public double Oldalso2
        {
            get { return oldalso2; }
            set { oldalso2 = value; }
        }

       /* public double Current
        {
            get { return current; }
            set { current = value; }
        }*/
    }
}
