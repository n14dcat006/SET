using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class Common
    {
        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void send(string message,ref Socket socket)
        {
            Common c = new Common();
            int toSendLen = System.Text.Encoding.ASCII.GetByteCount(message);

            int numberOfBytesRead = 0;
            int totalNumberOfBytes = 0;
           
            byte[] toSendBytes = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);
            do
            {
                numberOfBytesRead = socket.Send(toSendLenBytes, 4, SocketFlags.None);
                totalNumberOfBytes = totalNumberOfBytes + numberOfBytesRead;
            } while (totalNumberOfBytes == 4);
            int byteSend = 0;
            do
            {
                numberOfBytesRead = socket.Send(toSendBytes,byteSend, toSendLen-byteSend, SocketFlags.None);
                totalNumberOfBytes = totalNumberOfBytes + numberOfBytesRead;
            } while (totalNumberOfBytes == toSendLen);
            //Console.WriteLine(c.ByteArrayToString(toSendBytes));
        }
        //[MethodImpl(MethodImplOptions.Synchronized)]
        public string receive(ref Socket socket)
        {
            Common c = new Common();
            byte[] rcvLenBytes = new byte[4];

            int numberOfBytesRead = 0;
            int totalNumberOfBytes = 0;
            do
            {
                numberOfBytesRead = socket.Receive(rcvLenBytes, 4, SocketFlags.None);
                totalNumberOfBytes = totalNumberOfBytes + numberOfBytesRead;
            } while (totalNumberOfBytes == 4);
            
            int rcvLen = System.BitConverter.ToInt32(rcvLenBytes, 0);
            byte[] rcvBytes = new byte[rcvLen];

            var total = 0; // total bytes to received
            var dataleft = rcvLen; // bytes that havend been received 
            while (total < rcvLen)
            {
                var recv = socket.Receive(rcvBytes, total, dataleft, SocketFlags.None);
                if (recv == 0) // if received data = 0 than stop reseaving
                {
                    rcvLenBytes = null;
                    break;
                }
                total += recv;  // total bytes read + bytes that are received
                dataleft -= recv; // bytes that havend been received
            }
            
            
            String Message = System.Text.Encoding.ASCII.GetString(rcvBytes);
            //bool b = rcvBytes.All(x => x == default(byte));
            //Console.WriteLine(c.ByteArrayToString(rcvBytes));
            return Message;
        }
        public string Sign(string privateKey, string plainText)
        {
            byte[] plain = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(privateKey);
                encrypted = rsa.SignData(plain, CryptoConfig.MapNameToOID("SHA512"));
            }
            return ByteArrayToString(encrypted);
        }
        public string EncryptRSA(string publicKey, string plainText)
        {
            byte[] plain = Encoding.ASCII.GetBytes(plainText);
            byte[] encrypted;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(publicKey);
                encrypted = rsa.Encrypt(plain, true);
            }
            return ByteArrayToString(encrypted);
        }
        public string DecryptionRSA(string privateKey, string encryptText)
        {
            byte[] decrypt = StringToByteArray(encryptText);
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.FromXmlString(privateKey);
                decrypt = rsa.Decrypt(decrypt, true);
                string kq = System.Text.Encoding.ASCII.GetString(decrypt);
                return kq;
            }
        }
        public bool VerifyCertificate(X509Certificate2 caCertificate, X509Certificate2 additionalCertificates)
        {
            var chain = new X509Chain();
            chain.ChainPolicy.ExtraStore.Add(additionalCertificates);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;
            return chain.Build(caCertificate);
        }
        public Boolean Verify(string publicKey, string encryptMessage, string orgMessage)
        {
            byte[] decrypt = StringToByteArray(encryptMessage);
            Boolean decrypted;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.FromXmlString(publicKey);
                decrypted = rsa.VerifyData(Encoding.UTF8.GetBytes(orgMessage), CryptoConfig.MapNameToOID("SHA512"), StringToByteArray(encryptMessage));
            }
            return decrypted;
        }
        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
        public string Random(int n)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[n];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }
        public string EncryptDES(string originalString, string key)
        {
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(key);
            if (String.IsNullOrEmpty(originalString))
            {
                throw new ArgumentNullException
                       ("The string which needs to be encrypted can not be null.");
            }
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);
            StreamWriter writer = new StreamWriter(cryptoStream);
            writer.Write(originalString);
            writer.Flush();
            cryptoStream.FlushFinalBlock();
            writer.Flush();
            return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }
        public string DecryptDES(string cryptedString, string key)
        {
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(key);
            if (String.IsNullOrEmpty(cryptedString))
            {
                throw new ArgumentNullException
                   ("The string which needs to be decrypted can not be null.");
            }
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream
                    (Convert.FromBase64String(cryptedString));
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                cryptoProvider.CreateDecryptor(bytes, bytes), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }
        public string PrivateEncryption(string privateKey, string message)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            rsa.FromXmlString(privateKey);
            byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            if (data == null)
                throw new ArgumentNullException("data");
            if (rsa.PublicOnly)
                throw new InvalidOperationException("Private key is not loaded");

            int maxDataLength = (rsa.KeySize / 8) - 6;
            if (data.Length > maxDataLength)
                throw new ArgumentOutOfRangeException("data", string.Format(
                    "Maximum data length for the current key size ({0} bits) is {1} bytes (current length: {2} bytes)",
                    rsa.KeySize, maxDataLength, data.Length));

            // Add 4 byte padding to the data, and convert to BigInteger struct
            BigInteger numData = GetBig(AddPadding(data));

            RSAParameters rsaParams = rsa.ExportParameters(true);
            BigInteger D = GetBig(rsaParams.D);
            BigInteger Modulus = GetBig(rsaParams.Modulus);
            BigInteger encData = BigInteger.ModPow(numData, D, Modulus);

            byte[] byteKQ= encData.ToByteArray();
            return ByteArrayToString(byteKQ);
        }
        public string PublicDecryption(string publicKey, string message)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            rsa.FromXmlString(publicKey);
            byte[] cipherData = StringToByteArray(message);
            if (cipherData == null)
                throw new ArgumentNullException("cipherData");

            BigInteger numEncData = new BigInteger(cipherData);

            RSAParameters rsaParams = rsa.ExportParameters(false);
            BigInteger Exponent = GetBig(rsaParams.Exponent);
            BigInteger Modulus = GetBig(rsaParams.Modulus);

            BigInteger decData = BigInteger.ModPow(numEncData, Exponent, Modulus);

            byte[] data = decData.ToByteArray();
            byte[] result = new byte[data.Length - 1];
            Array.Copy(data, result, result.Length);
            result = RemovePadding(result);

            Array.Reverse(result);
            return System.Text.Encoding.ASCII.GetString(result);
        }
        public BigInteger GetBig(byte[] data)
        {
            byte[] inArr = (byte[])data.Clone();
            Array.Reverse(inArr);  // Reverse the byte order
            byte[] final = new byte[inArr.Length + 1];  // Add an empty byte at the end, to simulate unsigned BigInteger (no negatives!)
            Array.Copy(inArr, final, inArr.Length);

            return new BigInteger(final);
        }
        public byte[] AddPadding(byte[] data)
        {
            Random rnd = new Random();
            byte[] paddings = new byte[4];
            rnd.NextBytes(paddings);
            paddings[0] = (byte)(paddings[0] | 128);

            byte[] results = new byte[data.Length + 4];

            Array.Copy(paddings, results, 4);
            Array.Copy(data, 0, results, 4, data.Length);
            return results;
        }
        public byte[] RemovePadding(byte[] data)
        {
            byte[] results = new byte[data.Length - 4];
            Array.Copy(data, results, results.Length);
            return results;
        }
        public string HashAndEncryptRSA(string message, string priKey)
        {
            string hash = Hash(message);
            return PrivateEncryption(priKey, message);
        }
        public string[] DESandEncryptKey(string message, string pubKey)
        {
            string key = Random(8);
            string encryptMessage = EncryptDES(message, key);
            string encryptKey = EncryptRSA(pubKey, key);
            string[] kq ={encryptMessage, encryptKey};
            return kq;
        }

    }
}
