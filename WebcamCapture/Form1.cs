using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebcamCapture
{
    public partial class Form1 : Form
    {
        WebCam webcam;
        public Form1()
        {
            InitializeComponent();
            webcam = new WebCam();
            webcam.InitializeWebCam(ref pictureBox1);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            //webcam.Stop();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox1.Checked)
            {
                webcam.detection = true;
            }
            else
            {
                webcam.detection = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            webcam.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            webcam.Stop();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox2.Checked)
            {
                webcam.inversion = true;
            }
            else
            {
                webcam.inversion = false;
            }
        }
    }
}
