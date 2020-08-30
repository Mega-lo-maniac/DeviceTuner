﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace DeviceTuner.Modules.ModuleRS485.Models
{
    public class SerialSender : ISerialSender
    {
        private readonly int _packetLengthIndex = 1; //индекс байта в посылаемом пакете отвечающего за общую длину пакета
        private bool portReceive;
        private string receiveBuffer = "";
        private readonly SerialPort _serialPort;

        /// <summary>
        /// Болидовская таблица CRC
        /// </summary>
        private readonly byte[] crc8Table = new byte[]
        {
            0x00, 0x5E, 0x0BC, 0x0E2, 0x61, 0x3F, 0x0DD, 0x83, 0x0C2, 0x9C, 0x7E, 0x20, 0x0A3, 0x0FD, 0x1F, 0x41,
            0x9D, 0x0C3, 0x21, 0x7F, 0x0FC, 0x0A2, 0x40, 0x1E, 0x5F, 0x01, 0x0E3, 0x0BD, 0x3E, 0x60, 0x82, 0x0DC,
            0x23, 0x7D, 0x9F, 0x0C1, 0x42, 0x1C, 0x0FE, 0x0A0, 0x0E1, 0x0BF, 0x5D, 0x03, 0x80, 0x0DE, 0x3C, 0x62,
            0x0BE, 0x0E0, 0x02, 0x5C, 0x0DF, 0x81, 0x63, 0x3D, 0x7C, 0x22, 0x0C0, 0x9E, 0x1D, 0x43, 0x0A1, 0x0FF,
            0x46, 0x18, 0x0FA, 0x0A4, 0x27, 0x79, 0x9B, 0x0C5, 0x84, 0x0DA, 0x38, 0x66, 0x0E5, 0x0BB, 0x59, 0x07,
            0x0DB, 0x85, 0x67, 0x39, 0x0BA, 0x0E4, 0x06, 0x58, 0x19, 0x47, 0x0A5, 0x0FB, 0x78, 0x26, 0x0C4, 0x9A,
            0x65, 0x3B, 0x0D9, 0x87, 0x04, 0x5A, 0x0B8, 0x0E6, 0x0A7, 0x0F9, 0x1B, 0x45, 0x0C6, 0x98, 0x7A, 0x24,
            0x0F8, 0x0A6, 0x44, 0x1A, 0x99, 0x0C7, 0x25, 0x7B, 0x3A, 0x64, 0x86, 0x0D8, 0x5B, 0x05, 0x0E7, 0x0B9,
            0x8C, 0x0D2, 0x30, 0x6E, 0x0ED, 0x0B3, 0x51, 0x0F, 0x4E, 0x10, 0x0F2, 0x0AC, 0x2F, 0x71, 0x93, 0x0CD,
            0x11, 0x4F, 0x0AD, 0x0F3, 0x70, 0x2E, 0x0CC, 0x92, 0x0D3, 0x8D, 0x6F, 0x31, 0x0B2, 0x0EC, 0x0E, 0x50,
            0x0AF, 0x0F1, 0x13, 0x4D, 0x0CE, 0x90, 0x72, 0x2C, 0x6D, 0x33, 0x0D1, 0x8F, 0x0C, 0x52, 0x0B0, 0x0EE,
            0x32, 0x6C, 0x8E, 0x0D0, 0x53, 0x0D, 0x0EF, 0x0B1, 0x0F0, 0x0AE, 0x4C, 0x12, 0x91, 0x0CF, 0x2D, 0x73,
            0x0CA, 0x94, 0x76, 0x28, 0x0AB, 0x0F5, 0x17, 0x49, 0x08, 0x56, 0x0B4, 0x0EA, 0x69, 0x37, 0x0D5, 0x8B,
            0x57, 0x09, 0x0EB, 0x0B5, 0x36, 0x68, 0x8A, 0x0D4, 0x95, 0x0CB, 0x29, 0x77, 0x0F4, 0x0AA, 0x48, 0x16,
            0x0E9, 0x0B7, 0x55, 0x0B, 0x88, 0x0D6, 0x34, 0x6A, 0x2B, 0x75, 0x97, 0x0C9, 0x4A, 0x14, 0x0F6, 0x0A8,
            0x74, 0x2A, 0x0C8, 0x96, 0x15, 0x4B, 0x0A9, 0x0F7, 0x0B6, 0x0FC, 0x0A, 0x54, 0x0D7, 0x89, 0x6B, 0x35
        };

        private SerialSender()
        {
        }

        public SerialSender(SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public bool ChangeDeviceAddress(byte deviceAddress, byte newDeviceAddress)
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                // make DataReceived event handler
                _serialPort.DataReceived += sp_DataReceived;

                byte[] cmd = new byte[] { deviceAddress, 0x00, 0x0F, newDeviceAddress, newDeviceAddress }; ;
                SendPacket(cmd);
                while (portReceive == true) { }
                Thread.Sleep(100);


                Debug.WriteLine(receiveBuffer);
                _serialPort.Close();
                if(receiveBuffer != null)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetDeviceModel(byte deviceAddress)
        {
            throw new NotImplementedException();
        }

        public bool IsDeviceOnline(byte deviceAddress)
        {
            throw new NotImplementedException();
        }

        public List<byte> SearchOnlineDevices()
        {
            throw new NotImplementedException();
        }

        private void SendPacket(byte[] sendArray)
        {
            byte bytesCounter = 1; //сразу начнём считать с единицы, т.к. всё равно придётся добавить один байт(сам байт длины команды)
            List<byte> lst = new List<byte>();
            foreach (byte bt in sendArray)
            {
                lst.Add(bt);
                bytesCounter++;
            }

            lst.Insert(1, bytesCounter); //вставляем вторым байтом в пакет длину всего пакета + байт длины пакета


            _serialPort.Write(lst.ToArray(), 0, bytesCounter);
            _serialPort.Write(CRC8(lst.ToArray()), 0, 1);
        }

        private byte[] CRC8(byte[] bytes)
        {
            byte crc = 0;
            for (var i = 0; i < bytes.Length; i++)
                crc = crc8Table[crc ^ bytes[i]];

            byte[] chr = new byte[1];
            chr[0] = crc;
            return chr;
        }

        public ObservableCollection<string> GetAvailableCOMPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            ObservableCollection<string> portsList = new ObservableCollection<string>();
            foreach(string port in ports)
            {
                portsList.Add(port);
            }
            return portsList;
        }

        public SerialPort GetSerialPortObjectRef()
        {
            return _serialPort;
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            portReceive = true;
            //Console.WriteLine("I am in event handler");
            SerialPort sPort = (SerialPort)sender;
            string data = sPort.ReadExisting();
            foreach (char ch in data)
            {
                int value = Convert.ToInt32(ch);
                //Console.Write(value.ToString("X") + " ");
            }
            receiveBuffer += data;
            portReceive = false;
        }
    }
}