using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System.Threading;

namespace ManyNeurointerface
{
    class Program
    {
        static void Main(string[] args)
        {
            //получаем список портов
            string[] ports = SerialPort.GetPortNames();
            Regex comRegex = new Regex("COM[1-9][0-9]*");
            List<string> comPorts = new List<string>();

            foreach (var port in ports)
            {
                if (comRegex.IsMatch(port))
                {
                    Console.WriteLine(port);
                    comPorts.Add(port);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                //находим КОМпорт с подключенным нейроинтерфейсом
                SerialPort comPort = FindAvailablePortWithNeurointerface(comPorts);
                Console.WriteLine($"Порт, на котором располагается Mind Link свободный под названием: {comPort?.PortName}");

                //Запускаем поток для работы с нейроинтерфейсом
                Thread firstInterface = new Thread(new ParameterizedThreadStart(ThreadWithDatas));
                firstInterface.Name = $"Mind Link {i + 1}";
                firstInterface.Start(comPort);
            }

            Console.ReadKey();
        }

        static void ThreadWithDatas(object? port)
        {
            if (!(port is SerialPort)) return;
            SerialPort comPort = port as SerialPort;
            string filePath = "note2.txt";
            
            while (true)
            {
                int pLength = 0, readedChecksum = 0, checksum = 0;
                string readedByteString = "";
                if (comPort.ReadByte() != 170 ) continue;
                if (comPort.ReadByte() != 170) continue;
                pLength = comPort.ReadByte();
                if (pLength > 170) break;
                Byte[] buffer = new byte[pLength];
                //прочитать нужное кол-во байт
                comPort.Read(buffer, 0, pLength);
                for (int i = 0; i < pLength; i++)
                {
                    Console.Write(buffer[i] + " ");
                    readedByteString += buffer[i] + " ";
                    checksum += buffer[i];
                }
                Console.WriteLine();

                readedChecksum = comPort.ReadByte();
                checksum &= 0xFF;
                checksum = ~checksum & 0xFF;
                if (readedChecksum == checksum)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true, System.Text.Encoding.Default))
                    {
                        writer.WriteLineAsync(readedByteString);
                    }
                }
                Thread.Sleep(5000);
            }
        }

        static SerialPort FindAvailablePortWithNeurointerface(List<string> comPorts)
        {
            SerialPort comPort = new SerialPort();
            foreach (var port in comPorts)
            {
                try
                {
                    comPort.PortName = port;
                    comPort.ReadTimeout = 5000;

                    comPort.Open();

                    int firstByte = comPort.ReadByte();
                    int secondByte = comPort.ReadByte();

                    while (firstByte != 170 || secondByte != 170)
                    {
                        firstByte = secondByte;
                        secondByte = comPort.ReadByte();
                    }
                    return comPort;
                }
                catch (TimeoutException exp)
                {
                    Console.WriteLine($"Port \"{port}\": {exp.Message}");
                    comPort.Close();
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Port \"{port}\": {exp.Message}");
                }
            }
            return null;
        }
    }
}

