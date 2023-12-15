using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static PrimeGen.PrimeNumberChecker;

namespace PrimeGen
{
    class PrimeNumberGenerator {

        /**
         * Generates the prime numbers depending on the count
         * @param bitLength: the desired length of the prime number
         * @param count: the number of prime numbers wanted
         */
        public PrimeNumberGenerator(int bitLength, int count) {
             var t = GenerateAndPrintPrimeNumber(bitLength, count);
        }

        /**
         * Prints the usage of Prime number generator
         */
        private static void PrintHelpMessage()
        {
            string help = "dotnet run <bits> <count=1>\n" +
                          "\t- bits - the number of bits of the prime number, this must be a multiple of 8, and at least 32 bits.\n" +
                          "\t- count - the number of prime numbers to generate, defaults to 1";
            Console.WriteLine(help);
        }

        // public static void Main(string[] args)
        // {
        //     if (args.Length > 2 || args.Length < 1)
        //     {
        //         PrintHelpMessage();
        //     }
        //     else
        //     {
        //         var bitLength = Convert.ToInt32(args[0]);
        //         // check if the bitLenght is a multiple of 8 and bigger than 32
        //         // if (bitLength % 8 != 0 || bitLength < 32) {
        //         //     PrintHelpMessage();
        //         //     return;
        //         // }
        //         var count = 1;
        //         if (args.Length == 2)
        //         {
        //             count = Convert.ToInt32(args[1]);
        //         }
        //
        //
        //         // Console.WriteLine("BitLength: " + bitLength + " bits");
        //         var sw = new Stopwatch();
        //         sw.Start();
        //         new PrimeNumberGenerator(bitLength, count);
        //         sw.Stop();
        //         // Console.WriteLine("Time to Generate: " + sw.Elapsed);
        //     }
        // }

    }
    
    static class PrimeNumberChecker
    {
        private static Object myLock = new object();
        
        /**
         * Generates the prime numbers depending on the count
         * @param bitLength: the desired length of the prime number
         * @param count: the number of prime numbers wanted
         */
        public async static Task<BigInteger> GenerateAndPrintPrimeNumber(int bigLength, int count)
        {
            int currentPrimesFound = 0;
            BigInteger prime = new BigInteger();
            
            Parallel.For(0, Int64.MaxValue, (i, state) =>
            {
                if (state.IsStopped)
                {
                    return;
                }
                using RandomNumberGenerator gn = RandomNumberGenerator.Create();
                var bytes = new byte[bigLength / 8];
                gn.GetBytes(bytes);
                bytes[^1] &= (byte)0x7F;

                var bigInt = new BigInteger(bytes);
                if (bigInt.IsEven || bigInt.IsPowerOfTwo)
                {
                    return;
                }
                else if (bigInt.IsProbablyPrime())
                {
                    lock (myLock)
                    {
                        if (currentPrimesFound < count)
                        {

                            // Console.WriteLine(++currentPrimesFound + ": " + bigInt);
                            // if (currentPrimesFound < count)
                            // {
                            //     Console.WriteLine();
                            // }
                            ++currentPrimesFound;
                            prime = bigInt;
                            Console.WriteLine("PRIME FOUND");
                            if (currentPrimesFound == count)
                            {
                                state.Stop();
                                return;
                            }
                        }
                        else if (currentPrimesFound == count)
                        {
                            state.Stop();
                            return;
                        }
                    }
                }
            });

            return prime;
        }

        /**
         * retrieves a random BigInteger within a range
         */
        private static BigInteger GetRandomBigIntBetween(int min, BigInteger maxExcl)
        {
            
            var bytes = new byte[maxExcl.ToByteArray().Length - 10];

            using RandomNumberGenerator gn = RandomNumberGenerator.Create();
            gn.GetBytes(bytes);
            bytes[^1] &= (byte)0x7F;
            var randomBigInt = new BigInteger();

            do
            {
                gn.GetBytes(bytes);
                bytes[^1] &= (byte)0x7F;
                randomBigInt = new BigInteger(bytes);
                if (randomBigInt < maxExcl && randomBigInt >= min) {
                    break;
                }
            } while(randomBigInt >= maxExcl && randomBigInt < min);

            return randomBigInt;
        }

        /**
         * checks whether a number is a prime number or not
         */
        private static Boolean IsProbablyPrime(this BigInteger bigInt, int k = 10)
        {
            var n = bigInt;
            var d = n - 1;
            var r = 0;

            while (d % 2 == 0)
            {
                d /= 2;
            }

            while (n - 1 != d * BigInteger.Pow(2, r))
            {
                ++r;
            }

            for (int i = 0; i < k; i++)
            {
                var a = GetRandomBigIntBetween(2, BigInteger.Subtract(n, 3));
                var x = BigInteger.ModPow(a, d, n);
                if (x == 1 || x == n - 1)
                {
                    continue;
                }

                Boolean continueWitnessLoop = false;
                for (int j = 0; j < r; j++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        continueWitnessLoop = true;
                        break;
                    }
                }

                if (continueWitnessLoop)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        
    }
}