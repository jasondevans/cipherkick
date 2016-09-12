#pragma once

#include "stdafx.h"
#include "sqlite3.h"
#include "Timer.h"
#include "Site.h"
#include "Metainfo.h"
#include "tesseract/baseapi.h"


namespace WAppCpp {
	public ref class Util
	{

	private:
		
		Util();
		Util(const Util%) {
			throw gcnew System::InvalidOperationException("Singleton can't be copy-constructed.");
		}
		static Util instance;

	public:

		// Get our singleton instance.
		static property Util^ Instance {
			Util^ get() { return %instance; }
		}

		// Tesseract instance.
		tesseract::TessBaseAPI *api;

		// Operation success status constants.
		static const int SUCCEEDED = 0;
		static const int FAILED = 1;

		// Base64 characters.
		String^ base64_chars;

		// A reference to our SQLite database.
		sqlite3 *db;

		// A timer.
		Timer* timer;

		// Clean up.
		void cleanUp();

		// Attempt to open a database file.
		void openDbFile(String^ filePath, System::Security::SecureString^ password);

		// Set up a new database, creating tables and populating initial values.
		void setupNewDb(String^ friendlyName);

		// Get metadata.
		Metainfo^ getMetadata();

		// Get a list of all sites.
		System::Collections::Generic::List<Site^>^ getSiteList();

		// Search for records with names like search string.
		System::Collections::Generic::List<Site^>^ search(String^ searchTerm);

		// Get a specific site.
		Site^ getSite(Site^ site);

		// Get a specific site.
		Site^ getSite(int siteId);

		// Save a site.
		int saveSite(Site^ site);

		// Delete a site.
		void deleteSite(int id);

		// Get export data as a string.
		String^ getExportData(System::Security::SecureString^ encryptPassword);

		// Export to a file.
		void exportData(String^ filePath, System::Security::SecureString^ encryptPassword);

		// Import a CSV.
		void importLastPassCSV(String^ csvText);

		// Change master password.
		void changeMasterPassword(System::Security::SecureString^ newPassword);

		// Update last modified date/time.
		void updateLastModifiedTime();

		// Encode into base64.
		std::string base64_encode(unsigned char const* buf, unsigned int bufLen);

		// Decode from base64 into bytes.
		std::vector<unsigned char> base64_decode(std::string const& encoded_string);

		///Returns -1 if string is valid. Invalid character is put to ch.
		int getInvalidUtf8SymbolPosition(const unsigned char *input, unsigned char &ch);

		// Determine whether a given byte is a base64 character.
		static inline bool is_base64(unsigned char c) {
			return (isalnum(c) || (c == '+') || (c == '/'));
		}

		// Process an image and return OCR text.
		String^ doOCR(System::Drawing::Bitmap^ bitmap);

	};

}