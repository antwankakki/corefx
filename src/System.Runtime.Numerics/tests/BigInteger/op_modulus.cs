// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_modulusTest
    {
        private static int s_samples = 10;
        private static Random s_temp = new Random(-210220377);
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunRemainderTestsPositive()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Remainder Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");
            }

            // Remainder Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");
            }

            // Remainder Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");
            }
        }

        [Fact]
        public static void RunRemainderNegative()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Remainder Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyRemainderString(Print(tempByteArray2) + Print(tempByteArray1) + "b%");
                });
            }

            // Remainder Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyRemainderString(Print(tempByteArray1) + Print(tempByteArray2) + "b%");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyRemainderString(Print(tempByteArray2) + Print(tempByteArray1) + "b%");
                });
            }
        }

        [Fact]
        public static void RunRemainderBoundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyRemainderString(Math.Pow(2, 32) + " 2 b%");

            // 32 bit boundary  n1=0 n2=1
            VerifyRemainderString(Math.Pow(2, 33) + " 2 b%");
        }

        [Fact]
        public static void RunRemainderAxiomXModX()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X%X = 0
            VerifyIdentityString(Int32.MaxValue + " " + Int32.MaxValue + " b%", BigInteger.Zero.ToString());
            VerifyIdentityString(Int64.MaxValue + " " + Int64.MaxValue + " b%", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + randBigInt + "b%", BigInteger.Zero.ToString());
            }
        }

        [Fact]
        public static void RunRemainderAxiomXY()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X%(X + Y) = X where Y is 1 if x>=0 and -1 if x<0
            VerifyIdentityString((new BigInteger(Int32.MaxValue) + 1) + " " + Int32.MaxValue + " b%", Int32.MaxValue.ToString());
            VerifyIdentityString((new BigInteger(Int64.MaxValue) + 1) + " " + Int64.MaxValue + " b%", Int64.MaxValue.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                byte[] test = GetRandomByteArray(s_random);
                String randBigInt = Print(test);
                BigInteger modify = new BigInteger(1);
                if ((test[test.Length - 1] & 0x80) != 0)
                {
                    modify = BigInteger.Negate(modify);
                }
                VerifyIdentityString(randBigInt + modify.ToString() + " bAdd " + randBigInt + "b%", randBigInt.Substring(0, randBigInt.Length - 1));
            }
        }

        [Fact]
        public static void RunRemainderAxiomXMod1()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Axiom: X%1 = 0
            VerifyIdentityString(BigInteger.One + " " + Int32.MaxValue + " b%", BigInteger.Zero.ToString());
            VerifyIdentityString(BigInteger.One + " " + Int64.MaxValue + " b%", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(BigInteger.One + " " + randBigInt + "b%", BigInteger.Zero.ToString());
            }
        }

        [Fact]
        public static void RunRemainderAxiom0ModX()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: 0%X = 0
            VerifyIdentityString(Int32.MaxValue + " " + BigInteger.Zero + " b%", BigInteger.Zero.ToString());
            VerifyIdentityString(Int64.MaxValue + " " + BigInteger.Zero + " b%", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.Zero + " b%", BigInteger.Zero.ToString());
            }
        }

        private static void VerifyRemainderString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static void VerifyIdentityString(string opstring1, string opstring2)
        {
            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {
                //Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {
                //Run the full calculation
                sc2.DoNextOperation();
            }

            Assert.Equal(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString());
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(1, 100));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetNonZeroRandomByteArray(random, size);
        }
        
        private static String Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
