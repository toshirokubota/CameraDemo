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

        //MJPEGStream remoteSource = null;
        IVideoSource videoSource = null;
        //VideoCaptureDevice localSource = null;
        //IVideoSource localSource = null;

        bool bMotionDetectEnabled = false;
        bool bInversionEnabled = false;
        Queue<Bitmap> inqueue = new Queue<Bitmap>();
        Bitmap currentImage = null;
        Bitmap prevImage = null;
        Bitmap resultImage = null;
        int threshold = 20;
        long frameCount = 0;
        int sleep_time = 250;

        public Form1()
        {
            InitializeComponent();
#if REMOTE_CAMERA
            videoSource = new MJPEGStream("http://192.168.1.105:8099/videostream.cgi?user=admin&pwd=Foscam12chan");
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            videoSource.Start();
#else
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            videoSource.Start();
#endif
            backgroundWorker1.RunWorkerAsync();
        }

        unsafe void ProcessImage()
        {
            if (bInversionEnabled) return;

            Bitmap prev = null;
            Bitmap curr = null;
            Bitmap res = null;
            //lock (inqueue)
            try  
            {
                Monitor.Enter(inqueue);
                if (inqueue.Count >= 2) //need at least two for motion
                {
                    prev = (Bitmap)inqueue.Dequeue().Clone();
                    curr = (Bitmap)inqueue.Peek().Clone();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("1: " + ex.ToString());
                Application.Exit();
            }
            finally
            {
                Monitor.Exit(inqueue);
            }

            if (curr != null && prev != null)
            {
                res = (Bitmap)curr.Clone();
                if (bInversionEnabled || bMotionDetectEnabled)
                {
                    byte* bptr = null;
                    BitmapData data = null;
                    byte* bptr2 = null;
                    BitmapData data2 = null;
                    //lock (inqueue)
                    {
                        data = res.LockBits(new Rectangle(0, 0, res.Width, res.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                        bptr = (byte*)data.Scan0.ToPointer();
                        data2 = prev.LockBits(new Rectangle(0, 0, prev.Width, prev.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                        bptr2 = (byte*)data2.Scan0.ToPointer();
                        if (bInversionEnabled)
                        {
                            // _InvertImage(bptr, res.Width, res.Height);
                            //Thread.Sleep(10);
                        }
                        if (bMotionDetectEnabled)
                        {
                            // _MotionImage(bptr, bptr2, res.Width, res.Height, threshold);
                            //Thread.Sleep(10);
                        }
                        res.UnlockBits(data);
                        prev.UnlockBits(data2);
                        //Console.WriteLine("\tUnlocked res. {0}", inqueue.Count());
                    }
                     
                    //Thread.Sleep(10);
                }
                resultImage = (Bitmap)res.Clone();
                currentImage = (Bitmap)curr.Clone();
                prevImage = (Bitmap)prev.Clone();
                Console.WriteLine("\tUpdated result and output: {0}, {1}", resultImage, currentImage);
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
            /*lock (inqueue)
            {
                inqueue.Enqueue((Bitmap)e.Frame.Clone());
            }*/
            try
            {
                Monitor.Enter(inqueue);
                if (inqueue.Count < 10)
                {
                    inqueue.Enqueue((Bitmap)e.Frame.Clone());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("2: " + ex.ToString());
                Application.Exit();
            }
            finally
            {
                Monitor.Exit(inqueue);
            }
            frameCount++;
            Console.WriteLine("Frame: {0}, {1}", frameCount, inqueue.Count());
            Thread.Sleep(sleep_time);
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
#if REMOTE_CAMERA
            videoSource.Stop();
#else
            videoSource.Stop();
#endif
            backgroundWorker1.CancelAsync();
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
            //lock (inqueue)
            {
                if (currentImage != null)
                {
                    this.pictureBox1.Image = (Image)currentImage;
                    //Console.WriteLine("Updating outImage.{0},{1}", currentImage.Width, currentImage.Height);
                }
                if (prevImage != null)
                {
                    this.pictureBox2.Image = (Image)prevImage;
                }
                if (resultImage != null)
                {
                    this.pictureBox3.Image = (Image)resultImage;
                    //Console.WriteLine("Updating resultImage.{0},{1}", resultImage.Width, resultImage.Height);
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
            while (true)
            {
                ProcessImage();
                backgroundWorker1.ReportProgress(0);
                Thread.Sleep(sleep_time);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.pictureBox1.Invalidate();
        }
    }
}
