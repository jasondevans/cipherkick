#pragma once

#include "stdafx.h"


namespace WAppCpp {
	class Crypto
	{

	public:

		// Constructor.
		Crypto();

		// Return a reference to our singleton.
		static Crypto& getInstance()
		{
			static Crypto instance;
			return instance;
		}

		// Destructor.
		~Crypto();

		// Derive a 256-bit encyption key and a 256-bit authentication key from the given passphrase
		// and salt, using PBKDF2/SHA-512 with 10,000 iterations and 512-bit output, taking the 
		// encryption key from the first 32 bytes, and the authentication key from the second 32 bytes.
		// Returns 0 if success, or another value if there is an error.
		int Crypto::deriveKeys(System::Security::SecureString^ password, const std::vector<unsigned char>& salt,
			std::vector<unsigned char>& encryptionKey, std::vector<unsigned char>& authKey);

		// Encrypt a given byte array.
		int Crypto::encrypt(const std::vector<unsigned char>& plainText, const std::vector<unsigned char>& encryptKey,
			const std::vector<unsigned char>& authKey, std::vector<unsigned char>& cipherText);

		// Encrypt a given byte array.
		int Crypto::encrypt(const std::string& plainText, const std::vector<unsigned char>& encryptKey,
			const std::vector<unsigned char>& authKey, std::vector<unsigned char>& cipherText);

		// Decrypt.
		int Crypto::decrypt(const std::vector<unsigned char>& cipherText, const std::vector<unsigned char>& encryptKey,
			const std::vector<unsigned char>& authKey, std::vector<unsigned char>& decryptedText);

		// Generate a random byte.
		unsigned char Crypto::genRand();

		static void testOpenSSL();

	};
}

