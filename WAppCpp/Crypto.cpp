#include "stdafx.h"
#include <openssl/conf.h>
#include <openssl/evp.h>
#include <openssl/err.h>
#include <openssl/hmac.h>
#include <openssl/applink.c>

#include "Crypto.h"
#include "Util.h"


using namespace WAppCpp;
using namespace System::Runtime::InteropServices;
using namespace std;


// Constructor.
Crypto::Crypto()
{
	// Initialize OpenSSL.
	ERR_load_crypto_strings();
	OpenSSL_add_all_algorithms();
	OPENSSL_config(NULL);
}


// Destructor.
Crypto::~Crypto()
{
	// Clean up OpenSSL.
	EVP_cleanup();
	ERR_free_strings();
}


// Generate a 256-bit encyption key and a 256-bit authentication key from the given passphrase
// and salt, using PBKDF2/SHA-512 with 10,000 iterations and 512-bit output, taking the 
// encryption key from the first 32 bytes, and the authentication key from the second 32 bytes.
// Returns 0 if success, or another value if there is an error.
int Crypto::deriveKeys(System::Security::SecureString^ password, const vector<unsigned char>& salt,
	vector<unsigned char>& encryptionKey, vector<unsigned char>& authKey)
{
	IntPtr password_bstr = IntPtr::Zero;
	char* password_utf8 = 0;
	int password_utf8_len = 0;
	try
	{
		password_bstr = Marshal::SecureStringToBSTR(password);
		password_utf8_len = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, 0, NULL, NULL);
		password_utf8 = new char[password_utf8_len];
		int bytesWritten = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, password_utf8_len, NULL, NULL);

		unsigned char pbkdf2Output[64];
		PKCS5_PBKDF2_HMAC(password_utf8, password_utf8_len - 1, &salt[0], (int) salt.size(), 10000, EVP_sha512(), 64, pbkdf2Output);
		encryptionKey.resize(32);
		authKey.resize(32);
		for (unsigned int i = 0; i < 32; i++) encryptionKey[i] = pbkdf2Output[i];
		for (unsigned int i = 32; i < 64; i++) authKey[i - 32] = pbkdf2Output[i];
		return 0;
	}
	finally
	{
		Marshal::ZeroFreeBSTR(password_bstr);
		if (password_utf8)
		{
			SecureZeroMemory(password_utf8, password_utf8_len);
			delete password_utf8;
			password_utf8 = 0;
		}
	}
}


// Encrypt a given byte array.
int Crypto::encrypt(const string& plainText, const vector<unsigned char>& encryptKey,
	const vector<unsigned char>& authKey, vector<unsigned char>& cipherText)
{
	unsigned char* plainTextPtr = (unsigned char*) plainText.c_str();
	vector<unsigned char> plainTextVector(plainTextPtr, plainTextPtr + plainText.length());
	return encrypt(plainTextVector, encryptKey, authKey, cipherText);
}


// Encrypt a given string.
// This method encrypts the give plain text message, in the following way:
// * Generates two keys from the passphrase and salt, by using PBKDF2/SHA-512 with 10,000
//   iterations to generate a 512-bit value, which is split into two --
//   a 256-bit key for AES, and a 256-bit key for HMAC.
// * Then, plaintext is encrypted with AES-256 and random IV, and IV is
//   prepended to the message.
// * Then, HMAC is computed for ciphertext + IV, and prepended.
// Returns: 0 for success, or another number if there was an error.
int Crypto::encrypt(const vector<unsigned char>& plainText, const vector<unsigned char>& encryptKey,
	const vector<unsigned char>& authKey, vector<unsigned char>& cipherText)
{
	// Create and initialize the context.
	EVP_CIPHER_CTX *cipherCtx;
	if (!(cipherCtx = EVP_CIPHER_CTX_new())) { return -1; }

	// Create a random 128-bit IV.
	vector<unsigned char> iv;
	for (int i = 0; i < 16; i++) iv.push_back(genRand());

	// Initialize encryption.  Here, use 256-bit key and 128-bit IV.
	int status = EVP_EncryptInit_ex(cipherCtx, EVP_aes_256_cbc(), NULL, &encryptKey[0], &iv[0]);
	if (status != 1) { return -1; }

	// Encrypt.
	int len;
	cipherText.resize((int) plainText.size() + 16 /* padding */ + 16 /* IV */ + 32 /* HMAC */);
	status = EVP_EncryptUpdate(cipherCtx, &cipherText[32 + 16], &len, &plainText[0], (int) plainText.size());
	if (status != 1) { return -1; }
	int ciphertext_len = len;

	// Finalize encryption.
	status = EVP_EncryptFinal_ex(cipherCtx, &cipherText[32 + 16 + len], &len);
	if (status != 1) { return -1; }
	ciphertext_len += len;

	// Prepend the IV.
	for (int i = 0; i < 16; i++) cipherText[i + 32] = iv[i];

	// Resize the cipher text vector to be the actual size.
	cipherText.resize(ciphertext_len + 16 + 32);

	// Clean up.
	EVP_CIPHER_CTX_free(cipherCtx);

	// HMAC for authentication.
	vector<unsigned char> hmac;
	hmac.resize(32);
	unsigned int hmacSize;
	HMAC(EVP_sha256(), &authKey[0], (int) authKey.size(), &cipherText[32], cipherText.size() - 32, &hmac[0], &hmacSize);
	for (unsigned int i = 0; i < 32; i++) cipherText[i] = hmac[i];

	// Return success.
	return 0;
}


// Decrypt.
int Crypto::decrypt(const vector<unsigned char>& cipherText, const vector<unsigned char>& encryptKey,
	const vector<unsigned char>& authKey, vector<unsigned char>& decryptedText)
{
	// Create and initialize the context.
	EVP_CIPHER_CTX *ctx;
	if (!(ctx = EVP_CIPHER_CTX_new())) { return -1; }

	// Grab the HMAC from the first 32 bytes of the cipher text.
	vector<unsigned char> hmacGiven(&cipherText[0], &cipherText[0] + 32);

	// Compute the HMAC ourselves.
	vector<unsigned char> hmacComputed(32);
	unsigned int hmacSize;
	HMAC(EVP_sha256(), &authKey[0], (int) authKey.size(), &cipherText[32], (int) cipherText.size() - 32, &hmacComputed[0], &hmacSize);

	// Verify the HMAC.
	bool hmacMatch = true;
	for (unsigned int i = 0; i < 32; i++) if (hmacGiven[i] != hmacComputed[i]) { hmacMatch = false; break; }
	if (!hmacMatch) { return -1; }

	// Grab the IV from bytes 33-48 of the cipher text.
	vector<unsigned char> iv;
	for (int i = 32; i < 48; i++) iv.push_back(cipherText[i]);

	// Initialize decryption.  Here, use 256-bit key and 128-bit IV.
	int status = EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), NULL, &encryptKey[0], &iv[0]);
	if (status != 1) { return -1; }

	// Decrypt.
	int len;
	decryptedText.resize(cipherText.size() + 16);
	status = EVP_DecryptUpdate(ctx, &decryptedText[0], &len, &cipherText[16 + 32], (int) cipherText.size() - 16 /* IV */ - 32 /* HMAC */);
	if (status != 1) { return -1; }
	int decryptedtext_len = len;

	// Finalize decryption.
	status = EVP_DecryptFinal_ex(ctx, &decryptedText[0 + len], &len);
	if (status != 1) { return -1; }
	decryptedtext_len += len;
	decryptedText.resize(decryptedtext_len);

	// Clean up.
	EVP_CIPHER_CTX_free(ctx);

	// Return success.
	return 0;
}


// Generate a random 8-bit integer.
unsigned char Crypto::genRand()
{
	static boost::random_device rd;
	static boost::random::uniform_int_distribution<unsigned char> dis;
	return dis(rd);
}


void Crypto::testOpenSSL()
{
	// Create the password.
	System::Security::SecureString^ password;
	password->AppendChar('t');
	password->AppendChar('e');
	password->AppendChar('s');
	password->AppendChar('t');

	// Create the salt (in real life, this is created randomly and then stored).
	string saltTemp = "X6dd7toXD5+Lc8vCfQ0VwQ==";
	vector<unsigned char> salt;  // TODO:  REMOVE THIS LINE
	// TODO:  UNCOMMENT THIS LINE: vector<unsigned char> salt = Util::Instance->base64_decode(saltTemp);
	// unsigned char* saltTemp = (unsigned char*) "X6dd7toXD5+Lc8vCfQ0VwQ==";
	// vector<unsigned char> salt(saltTemp, saltTemp + strlen((char*)saltTemp));

	// Message to be encrypted.
	unsigned char *plainTextTemp = (unsigned char *)"The quick brown fox jumps over the lazy dog";
	vector<unsigned char> plainText(plainTextTemp, plainTextTemp + strlen((char*)plainTextTemp));

	// Cipher text and decrypted text vectors.
	vector<unsigned char> cipherText;
	vector<unsigned char> decryptedText;

	// Generate keys.
	vector<unsigned char> encryptKey;
	vector<unsigned char> authKey;
	int status = Crypto::getInstance().deriveKeys(password, salt, encryptKey, authKey);
	if (status != 0) { /* Do something */ }

	// Encrypt.
	status = Crypto::getInstance().encrypt(plainText, encryptKey, authKey, cipherText);
	if (status != 0)
	{
		// Do something about the error.
	}

	// Decrypt.
	status = Crypto::getInstance().decrypt(cipherText, encryptKey, authKey, decryptedText);
	if (status != 0)
	{
		// Do something about the error.
	}
}



