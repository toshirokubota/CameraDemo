using System;
using System.IO;
using System.Linq;
using System.Text;
using WebCam_Capture;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WebcamCapture
{
    //Design by Pongsakorn Poosankam
    class WebCam
    {
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe void InvertImage(char* buffer, int length);
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe void SubtractImage(char* buffer1, char* buffer2, int length, int thres);
        [DllImport("openCVDLL.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int detectFace(char* buffer, int width, int height);
        [DllImport("openCVDLL.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int foo(char* buffer, int width, int height);

        private WebCamCapture webcam;
        private System.Windows.Forms.PictureBox _FrameImage;
        private int FrameNumber = 30;
        private bool bDetect = false;
        private bool bInvert = false;
        private bool bInitialized = false;
        Bitmap bm_prev = null;

        public void InitializeWebCam(ref System.Windows.Forms.PictureBox ImageControl)
        {
            webcam = new WebCamCapture();
            webcam.FrameNumber = ((ulong)(0ul));
            webcam.TimeToCapture_milliseconds = FrameNumber;
            webcam.ImageCaptured += new WebCamCapture.WebCamEventHandler(webcam_ImageCaptured);
            _FrameImage = ImageControl;
        }

        unsafe void webcam_ImageCaptured(object source, WebcamEventArgs e)
        {
            //_FrameImage.Image = e.WebCamImage;
            Bitmap bm = (Bitmap)e.WebCamImage;
            Bitmap bm_copy = (Bitmap)bm.Clone();
            BitmapData data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
            //IntPtr ptr = data.Scan0;
            Byte* bptr = (Byte*)data.Scan0.ToPointer();
            if (this.inversion)
            {
                WebCam.InvertImage((char*)bptr, (int)(bm.Width * bm.Height * 3));
            }
            if (this.detection)
            {
                if (bm_prev != null)
                {
                    BitmapData data2 = bm_prev.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); //.Format32bppArgb); //.Format24bppRgb);
                    Byte* bptr2 = (Byte*)data2.Scan0.ToPointer();
                    WebCam.SubtractImage((char*)bptr, (char*)bptr2, (int)(bm.Width * bm.Height * 3), 30);
                }
            }
            /*if (this.detection)
            {
                if (bInitialized == false)
                {
                    int ret = WebCam.foo(null, 0, 0);
                    if (ret < 0)
                    {
                        MessageBox.Show("foo returned " + ret);
                        detection = false;
                    }
                    else
                    {
                        bInitialized = true;
                    }
                }
                int ret2 = WebCam.detectFace((char*)bptr, (int)bm.Width, (int)bm.Height);
                //int ret = WebCam.foo((char*)bptr, (int)bm.Width, (int)bm.Height);
                if (ret2 < 0)
                {
                    MessageBox.Show("detectFace returned " + ret2);
                    detection = false;
                }
            }*/
            bm.UnlockBits(data);
            _FrameImage.Image = bm; 
            bm_prev = bm_copy;
        }

        public void Start()
        {
            webcam.TimeToCapture_milliseconds = FrameNumber;
            webcam.Start(0);
        }

        public void Stop()
        {
            webcam.Stop();
        }

        public void Continue()
        {
            // change the capture time frame
            webcam.TimeToCapture_milliseconds = FrameNumber;

            // resume the video capture from the stop
            webcam.Start(this.webcam.FrameNumber);
        }

        public void ResolutionSetting()
        {
            //webcam.Config();
        }

        public void AdvanceSetting()
        {
            //webcam.Config2();
        }
        public bool detection
        {
            get { return bDetect; }
            set { bDetect = value; }
        }
        public bool inversion
        {
            get { return bInvert; }
            set { bInvert = value; }
        }
    }
}
