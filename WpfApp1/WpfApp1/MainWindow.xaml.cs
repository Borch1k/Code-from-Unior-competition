using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool ProcStatus;
        public SerialPort port;
        public RoutedEventArgs someEv;
        public Queue<String> buffer;

        Dictionary<string, string> opcodes = new Dictionary<string, string>();

        void Init()
        {
            opcodes.Add("ADD", "010100");
            opcodes.Add("ADDN", "011000");
            opcodes.Add("SUB", "100100");
            opcodes.Add("SUBR", "101000");
            opcodes.Add("JRR", "010110");
            opcodes.Add("JRN", "011010");
            opcodes.Add("JMP", "010010");
            opcodes.Add("JTA", "011010");
            opcodes.Add("JZ", "001111");
            opcodes.Add("JNZ", "001110");
            opcodes.Add("PCS", "000100");
            opcodes.Add("LNR", "011000");
            opcodes.Add("LMAR", "001001");
            opcodes.Add("SRA", "000110");
            opcodes.Add("SR", "000111");
            opcodes.Add("OR", "110100");
            opcodes.Add("XOR", "110101");
            opcodes.Add("AND", "110110");
            opcodes.Add("ORN", "111000");
            opcodes.Add("XORN", "111001");
            opcodes.Add("ANDN", "111010");
            opcodes.Add("NOT", "110111");
            opcodes.Add("ROUT", "111111");
        }

        string toBinary(int n, int length)
        {
            string r = "";
            if (n < 0)
            {
                n += 131072;
            }
            for (int i = 0; i < length; i++) { r = (n % 2 == 0 ? "0" : "1") + r; n /= 2; }
            return r;
        }
        int toDecimal(string n)
        {
            int r = 0;
            for (int i = 0; i < 8; i++) {
                r = (n[i] == '0' ? 0 : 1) + r * 2;
            }
            return r;
        }

        string ConvertLine(string line)
        {
            string outp = "";

            string[] args = line.ToUpper().Split(' ');
            try
            {
                outp = opcodes[args[0]];
                if (args[0] == "PCS")
                {
                    outp += toBinary(0, 16);
                    outp += toBinary(0, 5);
                    outp += toBinary(Convert.ToInt16(args[1]), 5);
                }
                else if (args[0] == "LNR")
                {
                    outp += toBinary(Convert.ToInt16(args[1]), 16);
                    outp += toBinary(0, 5);
                    outp += toBinary(Convert.ToInt16(args[2]), 5);
                }
                else if (args[0] == "LMAR")
                {
                    outp += toBinary(Convert.ToInt16(args[1]), 16);
                    outp += toBinary(0, 5);
                    outp += toBinary(Convert.ToInt16(args[2]), 5);
                }
                else if (args[0] == "LMR")
                {
                    outp += toBinary(Convert.ToInt16(args[1]), 16);
                    outp += toBinary(0, 5);
                    outp += toBinary(Convert.ToInt16(args[2]), 5);
                }
                else if (args[0] == "NOT")
                {
                    outp += toBinary(Convert.ToInt16(args[1]), 16);
                    outp += toBinary(0, 5);
                    outp += toBinary(Convert.ToInt16(args[2]), 5);
                }
                else
                {
                    if (args.Length == 2)
                    {
                        outp += toBinary(Convert.ToInt16(args[1]), 16);
                        outp += toBinary(0, 5);
                        outp += toBinary(0, 5);
                    }
                    else if (args.Length == 3)
                    {
                        if (args[2].Contains("/"))
                        {
                            outp += toBinary(Convert.ToInt16(args[1]), 16);
                            outp += toBinary(0, 5);
                            outp += toBinary(0, 5);
                        }
                        outp += toBinary(Convert.ToInt16(args[1]), 16);
                        outp += toBinary(Convert.ToInt16(args[2]), 5);
                        outp += toBinary(0, 5);

                    }
                    else if (args.Length == 4)
                    {
                        if (args[3].Contains("/"))
                        {
                            outp += toBinary(Convert.ToInt16(args[1]), 16);
                            outp += toBinary(Convert.ToInt16(args[2]), 5);
                            outp += toBinary(0, 5);
                        }
                        outp += toBinary(Convert.ToInt16(args[1]), 16);
                        outp += toBinary(Convert.ToInt16(args[2]), 5);
                        outp += toBinary(Convert.ToInt16(args[3]), 5);
                    }
                }
            }
            catch
            {
                outp = ".";
            }

            return outp;
        }

        public MainWindow()
        {
            InitializeComponent();
            Init();
            port = new SerialPort();
            buffer = new Queue<String>();
            try
            {
                // настройки порта
                port.PortName = SerialPort.GetPortNames()[1];
                port.BaudRate = 115200;
                port.DataBits = 8;
                port.Parity = System.IO.Ports.Parity.None;
                port.StopBits = System.IO.Ports.StopBits.Two;
                port.ReadTimeout = 1000;
                port.WriteTimeout = 1000;
                port.Open();
                Console.AppendText("Processor Connected" + "\n");
            }
            catch (Exception e)
            {
                Console.AppendText("ERROR: невозможно открыть порт:" + e.ToString() + "\n");
                return;
            }

            port.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

        }

        private void LoadFunction(object sender, RoutedEventArgs e)
        {
            Binary.Text = "";

            Queue<String> BinCode = new Queue<string>();
            string CodeLines = Code.Text;
            string[] lines = CodeLines.Split('\n');
            foreach (string line in lines)
            {
                BinCode.Enqueue(ConvertLine(line));
            }
            int counter = 0;
            while (BinCode.Count > 0)
            {
                string data = BinCode.Dequeue();
                if (data != ".")
                {
                    counter++;
                    Binary.AppendText(counter + ". " + data + "\n");
                } else
                {
                    Binary.AppendText("." + "\n");
                }

            }
        }

        private void SendCommand(Queue<string> bufferToSend)
        {
            Queue<String> refinedBufferToSend = new Queue<string>();

            while (bufferToSend.Count > 0)
            {
                string data = bufferToSend.Dequeue();
                for (int i = 0; i < 6; i++)
                {
                    refinedBufferToSend.Enqueue(data.Substring(i * 8, 8));
                }
            }

            Queue<byte> BinaryToSend = new Queue<byte>();

            while (refinedBufferToSend.Count > 0)
            {
                string data = refinedBufferToSend.Dequeue();
                BinaryToSend.Enqueue(Convert.ToByte(toDecimal(data)));
            }

            Console.AppendText("Program sended" + "\n");

            byte[] arr = BinaryToSend.ToArray();

            port.Write(arr, 0, arr.Length);
        }

        private void PushFunction(object sender, RoutedEventArgs e)
        {
            Queue<String> BinCode = new Queue<string>();
            string CodeLines = Code.Text;
            string[] lines = CodeLines.Split('\n');
            string converted = "";
            foreach (string line in lines)
            {
                converted = ConvertLine(line);
                if (converted != ".")
                {
                    BinCode.Enqueue(converted);
                }
            }

            Queue<String> bufferToSend = new Queue<string>();

            bufferToSend.Enqueue("00000100" + toBinary(0, 32) + toBinary(0, 8));

            int counter = 0;
            while (BinCode.Count > 0)
            {
                counter++;
                string data = BinCode.Dequeue();
                bufferToSend.Enqueue("10000000" + toBinary(counter, 32) + toBinary(0, 8));
                bufferToSend.Enqueue("01000000" + data + toBinary(0, 8));
            }

            bufferToSend.Enqueue("00000111" + toBinary(0, 32) + toBinary(0, 8));
            bufferToSend.Enqueue("00000001" + ConvertLine("JTA 1") + toBinary(0, 8));

            SendCommand(bufferToSend);

        }

        private void StopFunction(object sender, RoutedEventArgs e)
        {
            Queue<string> bufferToSend = new Queue<string>();

            bufferToSend.Enqueue("00000100" + toBinary(0, 32) + toBinary(0, 8));
            Console.AppendText("Processor was stoped");

            SendCommand(bufferToSend);
        }

        private void StartFunction(object sender, RoutedEventArgs e)
        {
            Queue<string> bufferToSend = new Queue<string>();

            bufferToSend.Enqueue("00000111" + toBinary(0, 32) + toBinary(0, 8));
            Console.AppendText("Processor was started");

            SendCommand(bufferToSend);
        }

        private void ClearConsole(object sender, RoutedEventArgs e)
        {
            Console.Text = "";
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void JumpToAdr(object sender, RoutedEventArgs e)
        {
            Queue<string> bufferToSend = new Queue<string>();

            string converted = ConvertLine("JTA " + Address.Text);
            if (converted != ".")
            {
                bufferToSend.Enqueue("00000001" + converted + toBinary(0, 8));
                Console.AppendText("Successfully jumped");
            } else
            {
                Console.AppendText("Jump is unsuccessful, change the address and try again");
            }

            SendCommand(bufferToSend);
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bufferSize = sp.BytesToRead;
            for (int i = 0; i < bufferSize / 4; i++)
            {
                int data = 0;
                for (int j = 0; j < 4; j++)
                {
                    int bt = sp.ReadByte();
                    data = data << 8;
                    data += bt;
                }
                buffer.Enqueue(data.ToString());
            }
            Write();
        }

        void Write()
        {
            while (buffer.Count > 0)
            {
                string data = buffer.Dequeue();
                Dispatcher.BeginInvoke((Action)(() => Console.AppendText(toBinary(Convert.ToInt32(data), 32) + "\n")));
            }
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Binary.ScrollToVerticalOffset(e.VerticalOffset);
        }
    } 
}
