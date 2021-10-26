using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
namespace myApp
{
    class Blocktrade
    {
        TcpClient _tradeClient;
        SslStream _tradeStreamSSL;
        string _host = "h28.p.ctrader.com";
        int _tradePort = 5212;
        int _messageSequenceNumber = 1;
        private string _username = "3006156";
        private string _password = "sp0tw@re";
        private string _senderCompID = "sales.3006156";
        private string _senderSubID = "3006156";
        private string _targetCompID = "CSERVER";
        public Blocktrade()
        {
            _tradeClient = new TcpClient(_host, _tradePort);
            _tradeStreamSSL = new SslStream(_tradeClient.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            _tradeStreamSSL.AuthenticateAsClient(_host);
        }

        public void Logon()
        {
            var body = new StringBuilder();
            //Defines a message encryption scheme.Currently, only transportlevel security 
            //is supported.Valid value is "0"(zero) = NONE_OTHER (encryption is not used).
            body.Append("98=0|");
            //Heartbeat interval in seconds.
            //Value is set in the 'config.properties' file (client side) as 'SERVER.POLLING.INTERVAL'.
            //30 seconds is default interval value. If HeartBtInt is set to 0, no heart beat message 
            //is required.
            body.Append("108=" + "30" + "|");
            // All sides of FIX session should have
            //sequence numbers reset. Valid value
            //is "Y" = Yes(reset).
            body.Append("141=Y|");
            //The numeric User ID. User is linked to SenderCompID (#49) value (the
            //user’s organization).
            body.Append("553=" + _username + "|");
            //USer Password
            body.Append("554=" + _password + "|");

            var header = ConstructHeader(body.ToString());
            var trailer = ConstructTrailer(header + body);
            var message = header + body + trailer;
            // var message = "8=FIX.4.4|9=122|35=A|49=sales.3006156|56=CSERVER|57=TRADE|50=3006156|34=1|52=20211025-17:26:50|98=0|108=30|141=Y|553=3006156|554=sp0tw@re|10=164|";
            Console.WriteLine("message: " + message);
            var response = sendData(message.Replace("|", "\u0001"));
            Console.WriteLine("response: " + response);
        }

        private string ConstructHeader(string bodyMessage)
        {
            var header = new StringBuilder();
            // Protocol version. FIX.4.4 (Always unencrypted, must be first field 
            // in message.
            header.Append("8=FIX.4.4|");
            var message = new StringBuilder();
            // Message type. Always unencrypted, must be third field in message.
            message.Append("35=" + "A" + "|");
            // ID of the trading party in following format: <BrokerUID>.<Trader Login> 
            // where BrokerUID is provided by cTrader and Trader Login is numeric 
            // identifier of the trader account.
            message.Append("49=" + _senderCompID + "|");
            // Message target. Valid value is "CSERVER"
            message.Append("56=" + _targetCompID + "|");
            // Additional session qualifier. Possible values are: "QUOTE", "TRADE".
            message.Append("57=" + "TRADE" + "|");
            // Assigned value used to identify specific message originator.
            message.Append("50=" + _senderSubID + "|");
            // Message Sequence Number
            message.Append("34=" + _messageSequenceNumber + "|");
            // Time of message transmission (always expressed in UTC(Universal Time 
            // Coordinated, also known as 'GMT').
            message.Append("52=" + DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss") + "|");

            var length = message.Length + bodyMessage.Length;
            // Message body length. Always unencrypted, must be second field in message.
            header.Append("9=" + length + "|");
            header.Append(message);
            return header.ToString();
        }

        private string ConstructTrailer(string message)
        {
            //Three byte, simple checksum. Always last field in message; i.e. serves,
            //with the trailing<SOH>, 
            //as the end - of - message delimiter. Always defined as three characters
            //(and always unencrypted).
            var trailer = "10=" + CalculateChecksum(message.Replace("|", "\u0001").ToString()).ToString().PadLeft(3, '0') + "|";
            return trailer;
        }
        private int CalculateChecksum(string dataToCalculate)
        {
            byte[] byteToCalculate = Encoding.ASCII.GetBytes(dataToCalculate);
            int checksum = 0;
            foreach (byte chData in byteToCalculate)
            {
                checksum += chData;
            }
            return checksum % 256;
        }
        private string sendData(string message)
        {
            var byteArray = Encoding.ASCII.GetBytes(message);
            _tradeStreamSSL.Write(byteArray, 0, byteArray.Length);
            var buffer = new byte[1024];

            Thread.Sleep(100);
            _tradeStreamSSL.Read(buffer, 0, 1024);

            _messageSequenceNumber++;
            var returnMessage = Encoding.ASCII.GetString(buffer);
            return returnMessage;
        }
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return false;
        }
    }


    class Program
    {
        static void Main()
        {
            Blocktrade bt = new Blocktrade();
            bt.Logon();

        }
    }
}

