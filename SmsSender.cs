//-----------------------------------------------------------------------
// <copyright file="SmsSender.cs" company="https://github.com/marcogrcr/NET-SMS-Sender">
//     License of use:
//
//     1. You may use and modify this file as you see fit.
//     2. Please credit the original author by preserving this header :)
// </copyright>
//-----------------------------------------------------------------------

namespace NetSmsSender
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Ports;
    using System.Threading;

    /// <summary>
    /// Sends SMS messages by issuing AT commands on a serial port.
    /// </summary>
    /// <remarks>
    /// For more information see:
    /// http://circuitfreak.blogspot.com/2013/03/c-programming-sending-sms-using-at.html
    /// https://github.com/CircuitFreakCoder/CSharp_SMS
    /// http://mobiletidings.com/2009/01/12/sending-out-an-sms-in-pdu-mode/
    /// http://developer.nokia.com/Community/Discussion/showthread.php/21237-CMS-ERROR-301-problem
    /// http://www.developershome.com/sms/resultCodes2.asp
    /// </remarks>
    public sealed class SmsSender : IDisposable
    {
        /// <summary>
        /// The baud rate of the serial port.
        /// </summary>
        private const int BaudRate = 115200;

        /// <summary>
        /// The initial sleep time after opening the serial port.
        /// </summary>
        private const int InitialSleepTime = 500;

        /// <summary>
        /// The PDU formatted SMS messages to send.
        /// </summary>
        private IEnumerable<PduSmsMessage> pduMessages;

        /// <summary>
        /// The serial port.
        /// </summary>
        private SerialPort port;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmsSender" /> class.
        /// </summary>
        /// <param name="portName">The name of the serial port.</param>
        /// <param name="countryCode">The number's country code.</param>
        /// <param name="localNumber">The number in local format.</param>
        /// <param name="text">The text to send.</param>
        public SmsSender(string portName, short countryCode, long localNumber, string text)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("A port name must be specified.", "portName");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException("A text must be specified.", "text");
            }

            // Port creation.
            this.port = new SerialPort(portName, SmsSender.BaudRate);
            this.port.DataReceived += this.DataReceived;

            // Number composition.
            var numberOfDigits = localNumber.ToString(CultureInfo.InvariantCulture).Length;
            var fullNumber     = ((long)(countryCode * Math.Pow(10, numberOfDigits))) + localNumber;

            this.pduMessages = text.Length <= PduSmsMessage.MaximumSmsTextLength ? new[] { new PduSmsMessage(fullNumber, text) } : PduSmsMessage.GetConcatenatedMessages(fullNumber, text);
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug mode is activated.
        /// </summary>
        public static bool Debug { get; set; }

        /// <summary>
        /// Closes the serial port.
        /// </summary>
        public void Dispose()
        {
            if (this.port != null)
            {
                this.port.Dispose();
            }
        }

        /// <summary>
        /// Sends the SMS message.
        /// </summary>
        public void Send()
        {
            lock (this.port)
            {
                this.OpenPort();
                this.SetPduMode();

                foreach (var pduMessage in this.pduMessages)
                {
                    this.SetSize(pduMessage);
                    this.SetContent(pduMessage);
                }
            }
        }

        /// <summary>
        /// Notifies waiting threads that there is data available.
        /// </summary>
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (this.port)
            {
                Monitor.Pulse(this.port);
            }
        }

        /// <summary>
        /// Blocking call that returns a non-null/non-empty response from the serial port.
        /// </summary>
        private string GetPortResponse()
        {
            string result;

            do
            {
                Monitor.Wait(this.port);
            }
            while (string.IsNullOrWhiteSpace(result = this.port.ReadExisting()));

            return result;
        }

        /// <summary>
        /// Opens the serial port.
        /// </summary>
        private void OpenPort()
        {
            this.port.Open();
            Thread.Sleep(SmsSender.InitialSleepTime);
        }

        /// <summary>
        /// Sets the PDU formatted SMS message content.
        /// </summary>
        /// <param name="pduMessage">The PDU formatted SMS message to send.</param>
        private void SetContent(PduSmsMessage pduMessage)
        {
            // Set the content.
            this.port.Write(string.Format(CultureInfo.InvariantCulture, "{0}\x1A", pduMessage));

            // Validate response.
            while (true)
            {
                var response = this.GetPortResponse();

                if (response.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Got error response '{0}'.", response));
                }

                if (SmsSender.Debug)
                {
                    Console.WriteLine("SetContent() got response: '{0}'.", response.Trim());
                }

                if (response.IndexOf("+CMGS:", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Sets the mode to PDU.
        /// </summary>
        private void SetPduMode()
        {
            // Set the mode.
            this.port.Write("AT+CMGF=0\r\n");

            // Validate response.
            while (true)
            {
                var response = this.GetPortResponse();

                if (response.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Got error response '{0}'.", response));
                }

                if (SmsSender.Debug)
                {
                    Console.WriteLine("SetContent() got response: '{0}'.", response.Trim());
                }

                if (response.IndexOf("OK", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Sets the size of the PDU.
        /// </summary>
        /// <param name="pduMessage">The PDU formatted SMS message to send.</param>
        private void SetSize(PduSmsMessage pduMessage)
        {
            // Set the size.
            this.port.Write(string.Format(CultureInfo.InvariantCulture, "AT+CMGS={0}\r\n", pduMessage.Length));

            // Validate response.
            while (true)
            {
                var response = this.GetPortResponse();

                if (response.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Got error response '{0}'.", response));
                }

                if (SmsSender.Debug)
                {
                    Console.WriteLine("SetContent() got response: '{0}'.", response.Trim());
                }

                if (response.IndexOf(">", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    break;
                }
            }
        }
    }
}