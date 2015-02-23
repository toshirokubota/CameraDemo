#define REMOTE_CAMERA

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
using AForge.Video.DirectShow;


namespace IPCameraCapture
{

    public partial class Form1 : Form
    {
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe void InvertImage(char* buffer, int length);
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int SubtractImage(char* buffer1, char* buffer2, int length, int thres);

        MJPEGStream remoteSource = null;
        VideoCaptureDevice localSource = null;
        bool bMotionDetectEnabled = false;
        bool bInversionEnabled = false;
        Bitmap prev_bmp = null;
        Bitmap prev_copy = null;
        Bitmap curr_bmp = null;
        Bitmap curr_copy = null;
        Bitmap result_bmp = null;
        int threshold = 50;
        long frameCount = 0;

        public Form1()
        {
            InitializeComponent();
#if REMOTE_CAMERA
            remoteSource = new MJPEGStream("http://192.168.1.105:8099/videostream.cgi?user=admin&pwd=Foscam12chan");
            remoteSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            remoteSource.Start();
#else
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            localSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            localSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            localSource.Start();
#endif
        }

        unsafe void _InvertImage(byte* bptr, int width, int height)
        {
            InvertImage((char*)bptr, width * height * 3);
        }
        unsafe void _MotionImage(byte* curr, byte* prev, int width, int height, int threshold)
        {
            int length = width * height * 3;
            int count = SubtractImage((char*)curr, (char*)prev, width * height * 3, threshold);
            Console.WriteLine("{0}: {1}", frameCount, count);
        }
        unsafe void video_NewFrame(object sender, NewFrameEventArgs e)
        {
            lock (this)
            {
                frameCount++;
                if (curr_bmp != null)
                {
                    prev_bmp = curr_bmp;
                    prev_copy = curr_copy;
                    //this.pictureBox2.Invalidate();
                }
                curr_bmp = (Bitmap)e.Frame.Clone();
                curr_copy = (Bitmap)e.Frame.Clone();

                if (prev_copy != null)
                {
                    if (bInversionEnabled || bMotionDetectEnabled)
                    {
                        BitmapData data = null;
                        BitmapData data2 = null;
                        byte* bptr = null;
                        byte* bptr2 = null;
                        try
                        {
                            data = curr_copy.LockBits(new Rectangle(0, 0, curr_copy.Width, curr_copy.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                            bptr = (byte*)data.Scan0.ToPointer();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "bm lock error");
                        }
                        try
                        {
                            data2 = prev_copy.LockBits(new Rectangle(0, 0, prev_copy.Width, prev_copy.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                            bptr2 = (byte*)data2.Scan0.ToPointer();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "prev_bm lock error");
                        }
                        try
                        {
                            if (bInversionEnabled)
                            {
                                _InvertImage(bptr, curr_copy.Width, curr_copy.Height);
                            }
                            if (bMotionDetectEnabled)
                            {
                                _MotionImage(bptr, bptr2, prev_copy.Width, prev_copy.Height, threshold);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "native code error.");
                        }
                        curr_copy.UnlockBits(data);
                        prev_copy.UnlockBits(data2);
                        result_bmp = (Bitmap) curr_copy.Clone();
                        //this.pictureBox3.Invalidate();
                    }
                }
                this.pictureBox1.Invalidate();
                //pictureBox1.Image = (Bitmap)e.Frame.Clone();
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
#if REMOTE_CAMERA
            remoteSource.Stop();
#else
            localSource.Stop();
#endif
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bMotionDetectEnabled = this.checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bInversionEnabled = this.checkBox2.Checked;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (curr_bmp != null)
            {
                this.pictureBox1.Image = (Image)curr_bmp;
            }
            if (prev_bmp != null)
            {
                this.pictureBox2.Image = (Image)prev_bmp;
            }
            if (result_bmp != null)
            {
                this.pictureBox3.Image = (Image)result_bmp;
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
