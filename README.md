NET-SMS-Sender
==============

A C# implementation of a SMS message sender using AT commands.

Supports concatenated messages of more than 160 characters!

**HOW TO USE**

Create an instance of the SmsSender and call the Send() method.

    using (var sender = new SmsSender(/* serial port name */, /* country code */, /* local number */, /* text to send */))
    {
        sender.Send();
    }

To send a message of more than 160 characters, use it the same way, the library will automatically split the text and send concatenated messages.

**EXAMPLE**

See Program.cs for an example of use.
