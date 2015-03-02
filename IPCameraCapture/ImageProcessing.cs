using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace IPCameraCapture
{
    class ImageProcessing
    {
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe void InvertImage(char* buffer, int length);
        [DllImport("myCPPLibrary.dll", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int SubtractImage(char* buffer1, char* buffer2, int length, int thres);
        unsafe static void _InvertImage(byte* bptr, int width, int height)
        {
            InvertImage((char*)bptr, width * height * 3);
        }
        unsafe static void _MotionImage(byte* curr, byte* prev, int width, int height, int threshold)
        {
            int length = width * height * 3;
            int count = SubtractImage((char*)curr, (char*)prev, width * height * 3, threshold);
        }
    }
}
