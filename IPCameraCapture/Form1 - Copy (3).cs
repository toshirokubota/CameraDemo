//#define REMOTE_CAMERA

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
        int threshold = 30;
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
                Bitmap bm = (Bitmap)e.Frame.Clone();
                Bitmap bmcopy = (Bitmap)e.Frame.Clone();
                BitmapData data = null;
                BitmapData data2 = null;
                byte* bptr = null;
                byte* bptr2 = null;

                this.pictureBox1.Image = bmcopy;
                if (prev_bmp != null)
                {
                    this.pictureBox2.Image = (Image) prev_bmp.Clone();
                    if (bInversionEnabled || bMotionDetectEnabled)
                    {
                        try
                        {
                            data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                            bptr = (byte*)data.Scan0.ToPointer();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "bm lock error");
                        }
                        try
                        {
                            data2 = prev_bmp.LockBits(new Rectangle(0, 0, prev_bmp.Width, prev_bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
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
                                _InvertImage(bptr, bm.Width, bm.Height);
                            }
                            if (bMotionDetectEnabled)
                            {
                                _MotionImage(bptr, bptr2, prev_bmp.Width, prev_bmp.Height, threshold);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "native code error.");
                        }
                        bm.UnlockBits(data);
                        prev_bmp.UnlockBits(data2);
                        this.pictureBox3.Image = (Image)bm.Clone();
                        this.pictureBox3.Invalidate();
                    }
                }
                //pictureBox1.Image = (Bitmap)e.Frame.Clone();

                this.prev_bmp = bmcopy;
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
    }
}
