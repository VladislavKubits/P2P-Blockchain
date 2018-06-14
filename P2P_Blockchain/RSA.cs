using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;

namespace P2P_Blockchain
{
    class RSA
    {

        static public void AssignNewKey(out string publicKey, out string privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                publicKey = KeyToString(rsa.ExportParameters(false));
                privateKey = KeyToString(rsa.ExportParameters(true));
            }
        }

        static public string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }

        static private string KeyToString(RSAParameters _Key)
        {
            //нам нужен буфер
            var sw = new System.IO.StringWriter();
            //нам нужен сериализатор
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //сериализуйте ключ в поток
            xs.Serialize(sw, _Key);
            //получить строку из потока
            return sw.ToString();
        }

        static public RSAParameters StringToKey(string _StringKey)
        {
            //получить поток из строки
            var sr = new System.IO.StringReader(_StringKey);
            //нам нужен десериализатор
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            //вернуть объект из потока
            return (RSAParameters)xs.Deserialize(sr);
        }

        static public byte[] SignData(byte[] hashOfDataToSign, string PrivKey)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(StringToKey(PrivKey));

                var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                rsaFormatter.SetHashAlgorithm("SHA256");
                return rsaFormatter.CreateSignature(hashOfDataToSign);
            }
        }
        static public bool VerifySignature(byte[] hashOfDataToSign,
                                    byte[] signature, string PubKey)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.ImportParameters(StringToKey(PubKey));
                var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                return rsaDeformatter.VerifySignature(hashOfDataToSign, signature);
            }
        }

        static public byte[] hashedText(string text)
        {
            var document = Encoding.UTF8.GetBytes(text);
            byte[] hashedDocument;

            using (var sha256 = SHA256.Create())
            {
                hashedDocument = sha256.ComputeHash(document);
            }

            return hashedDocument;
        }
    }
}
