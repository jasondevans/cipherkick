using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WPhoneApp
{

class Crypto
{



public Crypto()
{
}


public static void testCrypto()
{
    string password = "MypAssw0Rd1";
    IBuffer saltBuffer = CryptographicBuffer.GenerateRandom(16);
    byte[] saltBytes;
    CryptographicBuffer.CopyToByteArray(saltBuffer, out saltBytes);
    string plainText = "This a sample message!";
    byte[] encryptedBytes;
    byte[] decryptedBytes;
    byte[] encryptKey;
    byte[] authKey;
    deriveKeys(password, saltBytes, out encryptKey, out authKey);
    encrypt(plainText, encryptKey, authKey, out encryptedBytes);
    decrypt(encryptedBytes, encryptKey, authKey, out decryptedBytes);
    IBuffer decryptedBuffer = CryptographicBuffer.CreateFromByteArray(decryptedBytes);
    string decryptedText = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);
    if (plainText.Equals(decryptedText))
    {
        // Success!
    }
    else
    {
        // Failed.
    }
}


// Derive encryption and authentication keys from a password and salt.
public static void deriveKeys(string password, byte[] salt, out byte[] encryptKey, out byte[] authKey)
{
    IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
    IBuffer saltBuffer = CryptographicBuffer.CreateFromByteArray(salt);
    UInt32 iterations = 10000;
    KeyDerivationParameters kdp = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, iterations);
    KeyDerivationAlgorithmProvider kdap = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha512);
    CryptographicKey ck = kdap.CreateKey(passwordBuffer);
    IBuffer derivedKeyBuffer = CryptographicEngine.DeriveKeyMaterial(ck, kdp, 64);
    byte[] derivedKeyBytes;
    CryptographicBuffer.CopyToByteArray(derivedKeyBuffer, out derivedKeyBytes);
    encryptKey = new byte[32];
    System.Buffer.BlockCopy(derivedKeyBytes, 0, encryptKey, 0, 32);
    authKey = new byte[32];
    System.Buffer.BlockCopy(derivedKeyBytes, 32, authKey, 0, 32);
}


// Encrypt a given string.
public static void encrypt(string plainText, byte[] encryptKey, byte[] authKey, out byte[] cipherText)
{
	IBuffer plainTextBuffer = CryptographicBuffer.ConvertStringToBinary(plainText, BinaryStringEncoding.Utf8);
    byte[] plainTextBytes = new byte[plainTextBuffer.Length];
    CryptographicBuffer.CopyToByteArray(plainTextBuffer, out plainTextBytes);
    encrypt(plainTextBytes, encryptKey, authKey, out cipherText);
}

// Encrypt a byte array.
public static void encrypt(byte[] plainText, byte[] encryptKey, byte[] authkey, out byte[] cipherText)
{
    IBuffer cipherTextBuffer;
    encrypt(CryptographicBuffer.CreateFromByteArray(plainText),
        CryptographicBuffer.CreateFromByteArray(encryptKey),
        CryptographicBuffer.CreateFromByteArray(authkey),
        out cipherTextBuffer);
    CryptographicBuffer.CopyToByteArray(cipherTextBuffer, out cipherText);
}

// Encrypt an IBuffer.
public static void encrypt(IBuffer plainText, IBuffer encryptKey, IBuffer authKey, out IBuffer cipherText)
{
    SymmetricKeyAlgorithmProvider skap = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
    CryptographicKey ck = skap.CreateSymmetricKey(encryptKey);
    IBuffer iv = CryptographicBuffer.GenerateRandom(skap.BlockLength);
    IBuffer encryptedText = CryptographicEngine.Encrypt(ck, plainText, iv);

    byte[] ctbytes;
    CryptographicBuffer.CopyToByteArray(encryptedText, out ctbytes);
    byte[] ivbytes;
    CryptographicBuffer.CopyToByteArray(iv, out ivbytes);
    byte[] ivctbytes = new byte[ctbytes.Length + ivbytes.Length];
    System.Buffer.BlockCopy(ivbytes, 0, ivctbytes, 0, ivbytes.Length);
    System.Buffer.BlockCopy(ctbytes, 0, ivctbytes, ivbytes.Length, ctbytes.Length);
    IBuffer ivctBuffer = CryptographicBuffer.CreateFromByteArray(ivctbytes);

    MacAlgorithmProvider map = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
    CryptographicKey hmacKey = map.CreateKey(authKey);
    IBuffer ivctHmac = CryptographicEngine.Sign(hmacKey, ivctBuffer);

    byte[] hmacBytes;
    CryptographicBuffer.CopyToByteArray(ivctHmac, out hmacBytes);
    byte[] cipherTextBytes = new byte[ivctbytes.Length + hmacBytes.Length];
    System.Buffer.BlockCopy(hmacBytes, 0, cipherTextBytes, 0, hmacBytes.Length);
    System.Buffer.BlockCopy(ivctbytes, 0, cipherTextBytes, hmacBytes.Length, ivctbytes.Length);
    cipherText = CryptographicBuffer.CreateFromByteArray(cipherTextBytes);
}


// Decrypt data.
public static string decrypt(string cipherText64)
{
    IBuffer cipherTextBuffer = CryptographicBuffer.DecodeFromBase64String(cipherText64);
    byte[] cipherTextBytes = new byte[cipherTextBuffer.Length];
    CryptographicBuffer.CopyToByteArray(cipherTextBuffer, out cipherTextBytes);
    byte[] decryptedBytes;
    SharedState sharedState = App.Current.Resources["sharedState"] as SharedState;
    decrypt(cipherTextBytes, sharedState.encryptKey, sharedState.authKey, out decryptedBytes);
    IBuffer decryptedBuffer = CryptographicBuffer.CreateFromByteArray(decryptedBytes);
    return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);
}


// Decrypt data.
public static void decrypt(byte[] cipherText, byte[] encryptKey, byte[] authKey, out byte[] decryptedText)
{
    // Grab the HMAC from the first 32 bytes.
    byte[] hmacSentBytes = new byte[32];
    System.Buffer.BlockCopy(cipherText, 0, hmacSentBytes, 0, 32);
    IBuffer hmacSentBuffer = CryptographicBuffer.CreateFromByteArray(hmacSentBytes);

    // Compute the HMAC ourselves.
    byte[] cipherMinusHmacBytes = new byte[cipherText.Length - 32];
    System.Buffer.BlockCopy(cipherText, 32, cipherMinusHmacBytes, 0, cipherText.Length - 32);
    IBuffer cipherMinusHmacBuffer = CryptographicBuffer.CreateFromByteArray(cipherMinusHmacBytes);
    MacAlgorithmProvider map = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
    CryptographicKey hmacKey = map.CreateKey(CryptographicBuffer.CreateFromByteArray(authKey));
    IBuffer hmacComputedBuffer = CryptographicEngine.Sign(hmacKey, cipherMinusHmacBuffer);

    // Compare sent and computed HMAC's.
    byte[] hmacComputedBytes;
    CryptographicBuffer.CopyToByteArray(hmacComputedBuffer, out hmacComputedBytes);
    for (int i = 0; i < 32; i++)
    {
        if (hmacSentBytes[i] != hmacComputedBytes[i])
        {
            throw new Exception("HMAC verification failed.");
        }
    }

    // Grab the IV from bytes 33-48 of the cipher text.
    byte[] ivBytes = new byte[16];
    System.Buffer.BlockCopy(cipherText, 32, ivBytes, 0, 16);
    IBuffer ivBuffer = CryptographicBuffer.CreateFromByteArray(ivBytes);

    // Decrypt.
    byte[] dataBytes = new byte[cipherText.Length - (32 + 16)];
    System.Buffer.BlockCopy(cipherText, 16 + 32, dataBytes, 0, cipherText.Length - (32 + 16));
    IBuffer dataBuffer = CryptographicBuffer.CreateFromByteArray(dataBytes);
    SymmetricKeyAlgorithmProvider skap = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
    CryptographicKey ck = skap.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(encryptKey));
    IBuffer decryptedTextBuffer = CryptographicEngine.Decrypt(ck, dataBuffer, ivBuffer);
    CryptographicBuffer.CopyToByteArray(decryptedTextBuffer, out decryptedText);
}


}

}


