using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AForge.Video;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace IPCameraCapture
{

    public partial class Form1 : Form
    {
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe void InvertImage(char* buffer, int length);
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int SubtractImage(char* buffer1, char* buffer2, int length, int thres);

        MJPEGStream stream = null;
        bool bMotionDetectEnabled = false;
        bool bInversionEnabled = false;
        Bitmap prev_bm = null;
        int threshold = 0;

        public Form1()
        {
            InitializeComponent();

            stream = new MJPEGStream("http://192.168.1.105:8099/videostream.cgi?user=admin&pwd=Foscam12chan");
            stream.NewFrame += new NewFrameEventHandler(video_NewFrame);
            stream.Start();        
        }
        unsafe void _InvertImage(char* bptr, int width, int height)
        {
            InvertImage(bptr, width * height * 3);
        }
        unsafe void _MotionImage(char* curr, char* prev, int width, int height, int threshold)
        {
            int count = SubtractImage(curr, prev, width * height * 3, threshold);
            //count += 0;
            width += 0;
        }
        unsafe void video_NewFrame(object sender, NewFrameEventArgs e)
        {
            Bitmap bm = (Bitmap)e.Frame.Clone();
            Bitmap bmcopy = (Bitmap)e.Frame.Clone();
            BitmapData data = null;
            BitmapData data2 = null;
            Byte* bptr = null;
            Byte* bptr2 = null;
            if (prev_bm != null)
            {
                if (bInversionEnabled || bMotionDetectEnabled)
                {
                    lock (this)
                    {
                        //try
                        {
                            data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                            bptr = (Byte*)data.Scan0.ToPointer();
                        }
                        /*catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "bm lock error");
                        }*/
                        //try
                        {
                            data2 = prev_bm.LockBits(new Rectangle(0, 0, prev_bm.Width, prev_bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                            bptr2 = (Byte*)data2.Scan0.ToPointer();
                        }
                        /*catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "prev_bm lock error");
                        }*/
                        try
                        {
                            if (bInversionEnabled)
                            {
                                _InvertImage((char*)bptr, bm.Width, bm.Height);
                            }
                            if (bMotionDetectEnabled)
                            {
                                //_MotionImage((char*)bptr, (char*)bptr2, prev_bm.Width, prev_bm.Height, threshold);
                                int length = bm.Width * bm.Height;
                                char* curr = (char*)bptr;
                                char* prev = (char*)bptr2;
                                for (int i = 0; i < length; ++i)
                                {
                                    int dif = (int)curr[i] - (int)prev[i];
                                    if (dif > threshold)
                                    {
                                        curr[i] = (char)100;
                                    }
                                    else if (dif < -threshold)
                                    {
                                        curr[i] = (char)0;
                                    }
                                    else
                                    {
                                        curr[i] = (char)50;
                                    }
                                }
                                length += 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "native code error.");
                        }
                        bm.UnlockBits(data);
                        prev_bm.UnlockBits(data2);
                    }
                }
                //pictureBox1.Image = (Bitmap)e.Frame.Clone();
            }
            this.pictureBox1.Image = bm;
            this.prev_bm = bmcopy;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stream.Stop();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bMotionDetectEnabled = this.checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bInversionEnabled = this.checkBox2.Checked;
        }
    }
}
