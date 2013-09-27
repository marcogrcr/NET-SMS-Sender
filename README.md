NET-SMS-Sender
==============

A C# implementation of a SMS message sender using AT commands.

**HOW TO USE**

Create an instance of the SmsSender and call the Send() method.

    using (var sender = new SmsSender(/* serial port name */, /* country code */, /* local number */, /* text to send */))
    {
        sender.Send();
    }

**EXAMPLE**

See Program.cs for an example of use.
