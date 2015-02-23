// myCPPLibrary.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <iostream>

extern "C" __declspec( dllexport ) void HelloWorld();

extern "C" __declspec( dllexport ) void InvertImage(char* buffer, int length);
extern "C" __declspec( dllexport ) int SubtractImage(char* buffer1, char* buffer2, int length, int thres);

void
HelloWorld()
{
	std::cout << "Hello World." << std::endl;
}

void
InvertImage(char* buffer, int length)
{
	for(int i=0; i<length; ++i)
	{
		buffer[i] = 255 - buffer[i];
	}
}

int
SubtractImage(char* buffer1, char* buffer2, int length, int thres)
{
	int count = length;
	for(int i=0; i<length; ++i)
	{
		//buffer1[i] = 255 - buffer2[i];
		int dif = buffer1[i] - buffer2[i];
		if(dif > thres) 
		{
			buffer1[i] = 100;
			//count++;
			if(i < count) count = i;
		}
		else if(dif < -thres) {
			buffer1[i] = 0;
			//count++;
			if(i < count) count = i;
		}
		else {
			//count++;
			buffer1[i] = 50;
		}
		//buffer1[i] = rand() & 0xff;
	}
	return count;
}


