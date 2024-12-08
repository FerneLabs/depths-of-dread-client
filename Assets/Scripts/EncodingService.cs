using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Dojo.Starknet;
using dojo_bindings;
using UnityEngine;


class EncodingService
{
    public static string HexToASCII(string hex)
    {
        if (hex.StartsWith("0x"))
        {
            hex = hex.Substring(2); // Remove the "0x" prefix
        }

        // Trim leading zeros
        hex = hex.TrimStart('0');

        // Pad with a leading zero if the length is odd
        if (hex.Length % 2 != 0)
        {
            hex = "0" + hex; 
        }

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return Encoding.ASCII.GetString(bytes);
    }

    public static BigInteger ASCIIToBigInt(string ascii)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(ascii);
        UnityEngine.Debug.Log($"Bytes: {bytes.Length}");
        Array.Reverse(bytes); // Little endiand encoding goes back to front
        return new(bytes, isUnsigned: true);
    }

    public static string GetPoseidonHash(FieldElement input) 
    {
        dojo.FieldElement[] inputFelts = { input.Inner };

        unsafe 
        {
            dojo.FieldElement* inputPtr;
            fixed (dojo.FieldElement* ptr = inputFelts)
            {
                inputPtr = ptr;
            }

            // Call the native function
            dojo.FieldElement result = dojo.poseidon_hash(inputPtr, (UIntPtr)inputFelts.Length);
            FieldElement value = new(result);
            return value.Hex();
        }   
    }

    private static dojo.FieldElement[] SplitIntoFieldElements(byte[] inputBytes)
    {
        // Define the maximum size for a single chunk (32 bytes)
        const int maxChunkSize = 32;

        // Calculate the number of chunks
        int numberOfChunks = (inputBytes.Length + maxChunkSize - 1) / maxChunkSize;

        dojo.FieldElement[] fieldElements = new dojo.FieldElement[numberOfChunks];

        for (int i = 0; i < numberOfChunks; i++)
        {
            // Get the chunk of bytes (up to 32 bytes)
            int start = i * maxChunkSize;
            int length = Math.Min(maxChunkSize, inputBytes.Length - start);
            byte[] chunk = new byte[length];
            Array.Copy(inputBytes, start, chunk, 0, length);

            // Convert chunk to BigInteger
            BigInteger bigInt = new BigInteger(chunk, isUnsigned: true, isBigEndian: true);

            // Ensure the BigInteger fits in a FieldElement (implementation of FieldElement assumed)
            fieldElements[i] = new Dojo.Starknet.FieldElement(bigInt).Inner;
        }

        return fieldElements;
    }
}
