// @author Ghislaine Nyagatare

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrimeGen;

namespace  Messenger
{
    /// <summary>
    /// Represents a private key which has the key and a list of
    /// emails that are used for validating when getting a message
    /// </summary>
    internal class PrivateKey
    {
        [JsonProperty("email")] public List<string> email = new List<string>();

        [JsonProperty("key")] public string key { get; set; } = ""; 
        
        public void addEmail(string em)
        {
            email.Add(em);
        }
    }

    /// <summary>
    /// Represents a public key
    /// only the key is stored locally with no email attached
    /// </summary>
    internal class PublicKey
    {
        [JsonProperty("email")] public string email { get; set; } = "";

        [JsonProperty("key")] public string key { get; set; } = "";
    }
    
    /// <summary>
    /// This class handles all the operations that manipulate public
    /// and private keys
    /// </summary>
    internal class KeyProcessor
    {
        /// <summary>
        /// Generates a keypair of size keySize bits (public and private
        /// keys) and store them locally on the disk (in files called public.key and private.key
        /// respectively), in the current directory.
        /// </summary>
        /// <param name="keySize">size of public and private keys</param>
        public async Task GenKey(int keySize)
        {
            var deviation = (int) (keySize * 0.3);
            var pLength = (keySize / 2) + (new Random().Next(2 * deviation) - deviation);
            var qLength = keySize - pLength;

            var p = await PrimeNumberChecker.GenerateAndPrintPrimeNumber(pLength, 1);
            var q = await PrimeNumberChecker.GenerateAndPrintPrimeNumber(qLength, 1);

            var N = BigInteger.Multiply(p, q);
            var r = BigInteger.Multiply(p - 1, q - 1);

            var E = await PrimeNumberChecker.GenerateAndPrintPrimeNumber(512, 1);
            var D = ModInverse(E, r);
            Console.WriteLine("D value " + D);

            var eAsByteArr = BitConverter.GetBytes(E.GetByteCount()).Reverse().ToArray(); // little endian
            var nAsByteArr = BitConverter.GetBytes(N.GetByteCount()).Reverse().ToArray(); // little endian
            var dAsByteArr = BitConverter.GetBytes(D.GetByteCount()).Reverse().ToArray(); // little endian

            List<byte> publicKeyAsBytes = new List<byte>();
            publicKeyAsBytes.AddRange(eAsByteArr);
            publicKeyAsBytes.AddRange(E.ToByteArray());
            publicKeyAsBytes.AddRange(nAsByteArr);
            publicKeyAsBytes.AddRange(N.ToByteArray());

            List<byte> privateKeyAsBytes = new List<byte>();
            privateKeyAsBytes.AddRange(dAsByteArr);
            privateKeyAsBytes.AddRange(D.ToByteArray());
            privateKeyAsBytes.AddRange(nAsByteArr);
            privateKeyAsBytes.AddRange(N.ToByteArray());

            try
            {
                string pubK= Convert.ToBase64String(publicKeyAsBytes.ToArray());
                var publicKey = new PublicKey{email = "", key = pubK};
                WritePublicKeyToDisk(publicKey);

                string privK = Convert.ToBase64String(privateKeyAsBytes.ToArray());
                var privateKey = new PrivateKey {email = new List<string>(), key = privK};
                WritePrivateKeyToDisk(privateKey);
            }
            catch (Exception)
            {
                Console.WriteLine("keyGen error");
            }
        }

        /// <summary>
        /// Retrieves public key for a particular user and
        /// stores it locally as email.key
        /// </summary>
        /// <param name="email">the email associated with the public key</param>
        public async Task GetKey(string email)
        {
            try
            {
                var getKeyURL = Messenger.server + "Key/" + email;
                using HttpResponseMessage result = await  Messenger.client.GetAsync(getKeyURL);
                result.EnsureSuccessStatusCode();
                string encodedKey = await result.Content.ReadAsStringAsync();

                string filename = Directory.GetCurrentDirectory() + "/" + email + ".key";
                FileStream fs = File.Create(filename);
                byte[] data = Encoding.Default.GetBytes(encodedKey);
                fs.Write(data, 0, data.Length);
                fs.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("Key not found for " + email);
            }
        }

        /// <summary>
        /// Sends the public key that was generated in the keyGen
        /// phase and send it to the server, with the email address given
        /// </summary>
        /// <param name="email">the email associated with the public key</param>
        public async Task SendKey(string email) 
        {
            try
            {
                string publicKeyfilename = Directory.GetCurrentDirectory() + "/public.key";
                var publicKey = JObject.Parse(await File.ReadAllTextAsync(publicKeyfilename)) ;
                var message = new JObject {["email"] = email,  ["key"] = publicKey["key"]};

                var requestContent = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");
                var sendKeyURL =  Messenger.server + "Key/" + email;
                using HttpResponseMessage result = await  Messenger.client.PutAsync(sendKeyURL, requestContent);
                result.EnsureSuccessStatusCode();

                string privateKeyfilename = Directory.GetCurrentDirectory() + "/private.key";
                var privateKey = JObject.Parse(await File.ReadAllTextAsync(privateKeyfilename)).ToObject<PrivateKey>();
                if (privateKey != null)
                {
                    privateKey.addEmail(email); // updated the private key mail list for later validation
                    WritePrivateKeyToDisk(privateKey);
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Key does not exist for " + email);
            }
        }

        private BigInteger ModInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }

            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        private void WritePrivateKeyToDisk(PrivateKey privateKey)
        {
            string privateKeyfilename = Directory.GetCurrentDirectory() + "/private.key";
            FileStream fs = File.Create(privateKeyfilename);
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(privateKey));
            fs.Write(data, 0, data.Length);
            fs.Close();
        }
        
        private void WritePublicKeyToDisk(PublicKey publicKey)
        {
            string publicKeyfilename = Directory.GetCurrentDirectory() + "/public.key";
            FileStream fs = File.Create(publicKeyfilename);
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(publicKey));
            fs.Write(data, 0, data.Length);
            fs.Close();
        }
    }

    /// <summary>
    /// Handles the encryption and decryption of messages
    /// </summary>
    class MessageProcessor
    {
        /// <summary>
        /// Takes a plaintext message, encrypts it using the
        /// public key of the person you are sending it to.
        /// Sends the encrypted message to the server
        /// </summary>
        /// <param name="email">the email you want to send the message to</param>
        /// <param name="plaintext">the message you want to send in plaintext</param>
        public async Task SendMsg(string email, string plaintext)
        {
            try
            {
                string filename = Directory.GetCurrentDirectory() + "/" + email + ".key";
                var publicKey = JObject.Parse(await File.ReadAllTextAsync(filename)).ToObject<PublicKey>();

                if (publicKey != null)
                {
                    var cipher = EncryptMessage(plaintext, publicKey.key);
                    if (cipher == null) Console.WriteLine("SOMETHING WRONG");

                    var message = new JObject
                    {
                        ["email"] = email,
                        ["content"] = cipher
                    };

                    var requestContent = new StringContent(message.ToString(), Encoding.UTF8, 
                        "application/json");
                
                    var sendMsgURL = Messenger.server + "Message/" + email;
                    using HttpResponseMessage result = await Messenger.client.PutAsync(sendMsgURL, requestContent);
                    result.EnsureSuccessStatusCode();
                    Console.WriteLine("Message written");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Key does not exist for " + email);
            }
        }

        /// <summary>
        /// Retrieve a message for a particular user
        /// </summary>
        /// <param name="email">the email whose messages you want to retrieve</param>
        public async Task GetMsg(string email)
        {
            string filename = Directory.GetCurrentDirectory() + "/private.key";
            var personalKey = JObject.Parse(await File.ReadAllTextAsync(filename)).ToObject<PrivateKey>();
            if (personalKey != null && personalKey.email.Contains(email))
            {
                var getMsgURL = Messenger.server + "Message/" + email;
                using HttpResponseMessage result = await Messenger.client.GetAsync(getMsgURL);
                result.EnsureSuccessStatusCode();
                var messageAsStr = await result.Content.ReadAsStringAsync();
                var encodedMessage =  JObject.Parse(messageAsStr)["content"]?.ToString();

                if (encodedMessage != null)
                {
                    string plainMessage = DecryptMessage(encodedMessage, personalKey.key); // perform decryption here
                    Console.WriteLine(plainMessage);
                }
            }
            else
            {
                Console.WriteLine("Message can't be decoded.");
            }
        }

        private string EncryptMessage(string plaintext, string publicKey)
        {
            var key = Convert.FromBase64String(publicKey);

            var e = BitConverter.ToInt32(key.Take(4).Reverse().ToArray(), 0);
            var E = new BigInteger(key.Skip(4).Take(e).ToArray());
            var n = BitConverter.ToInt32(key.Skip(4 + e).Take(4).Reverse().ToArray(), 0);
            var N = new BigInteger(key.Skip(4 + e + 4).Take(n).ToArray());

            byte[] plainTextAsBytes = Encoding.UTF8.GetBytes(plaintext);
            var plain = new BigInteger(plainTextAsBytes);
            var ciphertext = BigInteger.ModPow(plain, E, N);
            return Convert.ToBase64String(ciphertext.ToByteArray());
        }

        private string DecryptMessage(string ciphertext, string personalKey)
        {
            var key = Convert.FromBase64String(personalKey);

            var d = BitConverter.ToInt32(key.Take(4).Reverse().ToArray(), 0);
            var D = new BigInteger(key.Skip(4).Take(d).ToArray());
            var n = BitConverter.ToInt32(key.Skip(4 + d).Take(4).Reverse().ToArray(), 0);
            var N = new BigInteger(key.Skip(4 + d + 4).Take(n).ToArray());
            
            byte[] messageAsBytes = Convert.FromBase64String(ciphertext);
            var cipher = new BigInteger(messageAsBytes);
            var plain = BigInteger.ModPow(cipher, D, N);
            return Encoding.UTF8.GetString(plain.ToByteArray());
        }
    }

    /// <summary>
    /// Program used public key encryption to send secure messages to other users
    /// Main program. Handles user input.
    /// </summary>
    class Messenger
    {
        
        public static string server = "http://kayrun.cs.rit.edu:5000/";
        public static readonly HttpClient client = new HttpClient();

        private static void PrintHelpMessage()
        {
            const string? usage = "dotnet run <option> <other arguments>\n" + 
                                  "\tgenKey \t\t- keySize\n" + 
                                  "\tsendKey \t- email\n" + 
                                  "\tgetKey \t\t- email\n" + 
                                  "\tsendMsg \t- email plaintext\n" + 
                                  "\tgetMsg \t\t- email\n";
            Console.WriteLine(usage);
        }
        
        public static async Task Main(string[] args)
        {
            
            if (args.Length > 1) // each option has at least 2 arguments
            {
                var mp = new MessageProcessor();
                var kp = new KeyProcessor();
                
                var option = args[0];
                try
                {
                    switch (option)
                    {
                        case "keyGen" when args.Length == 2:
                        {
                            var keySize = Convert.ToInt32(args[1]);
                            await kp.GenKey(keySize);
                            break;
                        }
                        case "sendKey" when args.Length == 2:
                        {
                            var email = args[1];
                            await kp.SendKey(email);
                            break;
                        }
                        case "getKey" when args.Length == 2:
                        {
                            var email = args[1];
                            await kp.GetKey(email);
                            break;
                        }
                        case "sendMsg" when args.Length == 3:
                        {
                            var email = args[1];
                            var plainText = args[2];
                            await mp.SendMsg(email, plainText);
                            break;
                        }
                        case "getMsg" when args.Length == 2:
                        {
                            var email = args[1];
                            await mp.GetMsg(email);
                            break;
                        }
                        default:
                            PrintHelpMessage();
                            break;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid parameters for option <" + args[0] + ">"); 
                }
            }
            else
            {
                PrintHelpMessage();
            }
        }
    }
}