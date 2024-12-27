using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using bottlenoselabs.C2CS.Runtime;
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
        Array.Reverse(bytes); // Little endiand encoding goes back to front
        return new(bytes, isUnsigned: true);
    }

    public static string GetPoseidonHash(FieldElement input)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            return WebPoseidon(input);
        #else
            return WindowsPoseidon(input);
        #endif
    }

    private static string WebPoseidon(FieldElement input)
    {
        CString inputData = new(input.Hex());
        FieldElement hash = new(StarknetInterop.PoseidonHash(inputData));
        return hash.Hex();
    }

    private static string WindowsPoseidon(FieldElement input)
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

    public static string SecondsToTime(int seconds)
    {
        if (seconds == 0) return string.Empty;

        var minutes = seconds / 60;
        var remainingSeconds = seconds % 60;

        if (minutes > 0)
        {
            return $"{minutes}m {Math.Abs(remainingSeconds)}s";
        }
        else
        {
            return $"{Math.Abs(remainingSeconds)}s";
        }
    }
}
