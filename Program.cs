//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="https://github.com/marcogrcr/NET-SMS-Sender">
//     License of use:
//
//     1. You may use and modify this file as you see fit.
//     2. Please credit the original author by preserving this header :)
// </copyright>
//-----------------------------------------------------------------------

namespace NetSmsSender
{
    using System;

    internal class Program
    {
        private static void Main()
        {
            try
            {
                // Change the 'X' to the port where the phone is located (e.g. COM8).
                string portName = "COMX";

                // The number's country code.
                short countryCode = 1;

                // The local number.
                long localNumber = 3052345678;

                // The message text.
                string text = "Hello World!";

                // Optional: Set this to true if you want debug messages.
                SmsSender.Debug = true;

                using (var sender = new SmsSender(portName, countryCode, localNumber, text))
                {
                    sender.Send();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}.", ex.Message);
            }
        }
    }
}