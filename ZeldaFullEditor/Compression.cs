﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace ZeldaFullEditor
{
    public static class Compression
    {
        //Tiles graphics decompression set it on 0x600bytes no matter what to prevent bugs
        public static byte[] Decompress(int pos, byte[] ROM_DATA,bool reversed = false)
        {
            List<byte> dataBuffer = new List<byte>();
            bool done = false;

            while (done == false)
            {
                bool expand = false;
                byte b = ROM.DATA[pos];

                //Console.WriteLine(b + " : " + ((b >> 5) & 0x07));
                if (b == 0xFF)
                {
                    done = true;
                }

                if (((b >> 5) & 0x07) == 7) //expanded command
                {
                    expand = true;
                }
                byte cmd = 0;
                short length = 0;
                if (expand)
                {
                    cmd = (byte)((b >> 2) & 0x07);
                    length = BitConverter.ToInt16(new byte[] { (ROM.DATA[pos + 1]), (byte)(b & 0x03) }, 0);
                    pos += 2;
                }
                else
                {
                    cmd = (byte)((b >> 5) & 0x07);
                    length = (byte)(b & 0x1F);
                    pos += 1;
                }
                length += 1;
                if (cmd == 0)//000 Direct Copy Followed by (L+1) bytes of data
                {
                    for (int i = 0; i < length; i++)
                    {
                        dataBuffer.Add(ROM.DATA[pos]);
                        pos++;
                    }
                }

                if (cmd == 1)//001 "Byte Fill" Followed by one byte to be repeated (L + 1) times
                {
                    byte copiedByte = ROM.DATA[pos];
                    pos++;
                    for (int i = 0; i < length; i++)
                    {
                        dataBuffer.Add(copiedByte);
                    }
                }

                if (cmd == 2)//010    "Word Fill" Followed by two bytes. Output first byte, then second, then first,then second, etc. until (L+1) bytes has been outputted
                {
                    byte copiedByte = ROM.DATA[pos];
                    byte copiedByte2 = ROM.DATA[pos + 1];
                    pos += 2;
                    int j = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (j == 0)
                        {
                            dataBuffer.Add(copiedByte);
                            j = 1;
                        }
                        else
                        {
                            dataBuffer.Add(copiedByte2);
                            j = 0;
                        }
                    }
                }

                if (cmd == 3)//"Increasing Fill" Followed by one byte to be repeated (L + 1) times, but the byte is increased by 1 after each write
                {
                    byte copiedByte = ROM.DATA[pos];
                    pos += 1;
                    for (int i = 0; i < length; i++)
                    {
                        dataBuffer.Add((byte)(copiedByte + i));
                    }
                }

                if (cmd == 4)//"Repeat" Followed by two bytes (ABCD byte order) containing address (in the output buffer) to copy (L + 1) bytes from
                {
                    byte copiedByte = ROM.DATA[pos];
                    byte copiedByte2 = ROM.DATA[pos + 1];
                    int pos2 = BitConverter.ToInt16(new byte[] { copiedByte, copiedByte2 }, 0);
                    if (reversed)
                    {
                        pos2 = BitConverter.ToInt16(new byte[] { copiedByte2, copiedByte }, 0);
                    }

                    pos += 2;
                    for (int i = 0; i < length; i++)
                    {
                        dataBuffer.Add(dataBuffer[pos2]);
                        pos2++;
                    }
                }

            }

            return dataBuffer.ToArray();
        }


        public static byte[] DecompressTiles() //to gfx.bin
        {
            byte[] buffer = new byte[0x63000];// (185)
            byte[] bufferBlock;
            int bufferPos = 0;
            for (int i = 0; i < 96; i++)
            {
                byte[] b = new byte[] { ROM.DATA[Constants.gfx_pointer_3 + i], ROM.DATA[Constants.gfx_pointer_2 + i], ROM.DATA[Constants.gfx_pointer_1 + i], 0 };
                int addr = BitConverter.ToInt32(b, 0);
                bufferBlock = Decompress(Addresses.snestopc(addr), ROM.DATA);
                for (int j = 0; j < bufferBlock.Length; j++)
                {
                    buffer[bufferPos] = bufferBlock[j];
                    bufferPos++;
                }
            }

            for (int i = 115; i < 216; i++)
            {
                if (i < 115 || i > 126)
                {
                    byte[] b = new byte[] { ROM.DATA[Constants.gfx_pointer_3 + i], ROM.DATA[Constants.gfx_pointer_2 + i], ROM.DATA[Constants.gfx_pointer_1 + i], 0 };
                    int addr = BitConverter.ToInt32(b, 0);
                    bufferBlock = Decompress(Addresses.snestopc(addr), ROM.DATA);
                    if (bufferBlock.Length == 0x600)
                    {
                        for (int j = 0; j < bufferBlock.Length; j++)
                        {
                            buffer[bufferPos] = bufferBlock[j];
                            bufferPos++;
                        }
                    }
                }
                else
                {
                    byte[] b = new byte[] { ROM.DATA[Constants.gfx_pointer_3 + i], ROM.DATA[Constants.gfx_pointer_2 + i], ROM.DATA[Constants.gfx_pointer_1 + i], 0 };
                    int addr = BitConverter.ToInt32(b, 0);
                    addr = Addresses.snestopc(addr);
                    for (int j = 0; j < 0x600; j++)
                    {
                        buffer[bufferPos] = ROM.DATA[addr+j];
                        bufferPos++;
                    }
                }
            }
            if (Constants.Rando)
            {
                bufferBlock = Decompress(0x18A800, ROM.DATA);
                for (int j = 0; j < 0x600; j++)
                {
                    buffer[bufferPos] = bufferBlock[j];
                    bufferPos++;
                }
            }



            return buffer;
        }

        public static byte[] bpp3tobpp4(byte[] bpp3_data) //to gfx.bin
        {
            List<byte> buffer = new List<byte>();// (97)
                for (int j = 0; j < 64 * 198; j++) //number of 8x8 blocks
            {
                for (int i = 0; i < 16; i++) //those remain the same
                {
                    buffer.Add(bpp3_data[(j * 24) + i]);
                }
                for(int i = 0;i<8;i++)
                {
                    buffer.Add(bpp3_data[(j * 24) + 16 + i]);
                    buffer.Add(0);
                }
            }
            FileStream fs = new FileStream("testgfx.gfx", FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(buffer.ToArray(), 0, buffer.Count);
            fs.Close();
            return buffer.ToArray();

        }

    }
}
