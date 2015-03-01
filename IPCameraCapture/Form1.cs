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
using AForge.Video;
using System.Threading;


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
        Queue<Bitmap> inqueue = new Queue<Bitmap>();
        Bitmap outImage = null;
        Bitmap resultImage = null;
        int threshold = 50;
        long frameCount = 0;
        int sleep_time = 30;

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

        unsafe void ProcessImage()
        {
            Bitmap curr = null;
            Bitmap res = null;
            //lock (inqueue)
            {
                if (inqueue.Count > 0)
                {
                    curr = inqueue.Dequeue();
                }
            }
            if (curr != null)
            {
                res = (Bitmap)curr.Clone();
                if (bInversionEnabled)
                {
                    byte* bptr = null;
                    BitmapData data = null;
                    if (res != null)
                    {
                        data = res.LockBits(new Rectangle(0, 0, res.Width, res.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                        bptr = (byte*)data.Scan0.ToPointer();
                        _InvertImage(bptr, res.Width, res.Height);
                        res.UnlockBits(data);
                        Console.WriteLine("\tUnlocked res. {0}", inqueue.Count());
                    }
                }
                //lock (inqueue)
                {
                    try
                    {
                        resultImage = (Bitmap)res.Clone();
                        outImage = (Bitmap)curr.Clone();
                        Console.WriteLine("\tUpdated result and output: {0}, {1}", resultImage, outImage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\tException caught: {0}, {1}", resultImage, outImage);
                    }
                }
            }
        }

        unsafe void _InvertImage(byte* bptr, int width, int height)
        {
            InvertImage((char*)bptr, width * height * 3);
        }
        unsafe void _MotionImage(byte* curr, byte* prev, int width, int height, int threshold)
        {
            int length = width * height * 3;
            int count = SubtractImage((char*)curr, (char*)prev, width * height * 3, threshold);
        }
        unsafe void video_NewFrame(object sender, NewFrameEventArgs e)
        {
            lock (inqueue)
            {
                frameCount++;
                inqueue.Enqueue(e.Frame);
                Console.WriteLine("Frame: {0}, {1}", frameCount, inqueue.Count());
                backgroundWorker1.RunWorkerAsync();
            }
            Thread.Sleep(sleep_time);
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
            lock (inqueue)
            {
                if (outImage != null)
                {
                    this.pictureBox1.Image = (Image)outImage;
                    this.pictureBox2.Image = (Image)outImage;
                    Console.WriteLine("Updating outImage.{0},{1}", outImage.Width, outImage.Height);
                }
                if (resultImage != null)
                {
                    this.pictureBox3.Image = (Image)resultImage;
                    Console.WriteLine("Updating resultImage.{0},{1}", resultImage.Width, resultImage.Height);
                }
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ProcessImage();
            /*lock(inqueue)
            {
                outImage = inqueue.Dequeue();
            }*/
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.pictureBox1.Invalidate();
        }
    }
}
