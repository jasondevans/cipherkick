#include "stdafx.h"

#include "Util.h"
#include "Crypto.h"
#include "UtilException.h"

#include <iterator>

#include <boost/tokenizer.hpp>
#include <boost/regex.hpp>
#include <boost/algorithm/string/replace.hpp>
#include <boost/filesystem.hpp>

#include "leptonica/allheaders.h"

#include <msclr/marshal_cppstd.h>


using namespace WAppCpp;
using namespace System;
using namespace System::Collections;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;


Util::Util()
{
	// Initialize database pointer to null.
	db = NULL;

	// Define base64 characters.
	base64_chars =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZ"
		"abcdefghijklmnopqrstuvwxyz"
		"0123456789+/";

	// Start the timer.
	timer = new Timer();
	timer->start();


	// Get executable directory.
	char pgmptr[512];
	GetModuleFileNameA(NULL, pgmptr, 512);
	boost::filesystem::path executablePath(pgmptr);
	boost::filesystem::path executableDir = executablePath.parent_path();

	// Initialize Tesseract.
	api = new tesseract::TessBaseAPI();
	//int initResult = api->Init("C:\\Users\\jason\\Documents\\Projects\\Tesseract-Build\\tesseract-vs2013\\tessdata", "eng");
	boost::filesystem::path tessDataDir(executableDir);
	tessDataDir /= "tessdata";
	int initResult = api->Init(tessDataDir.string().c_str(), "eng");
	if (initResult != 0)
	{
		throw gcnew UtilException("Tesseract failed to initialize successfully.");
	}

}


// Attempt to open a database file.
void Util::openDbFile(String^ filePath, System::Security::SecureString^ password)
{
	// Close the database if it is currently open.
	sqlite3_close(db);
	db = NULL;

	// Open the database.
	pin_ptr<sqlite3*> db_pp = &db;
	std::string filePathStr = msclr::interop::marshal_as<std::string>(filePath);
	int status = sqlite3_open(filePathStr.c_str(), db_pp);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to open database file (SQLITE status " + std::to_string(status) + ").");
	}

	// Set the encrypt password.
	IntPtr password_bstr = IntPtr::Zero;
	char* password_utf8 = 0;
	int password_utf8_len = 0;
	char* setKeySQL_2 = 0;
	try
	{
		char* setKeySQL_1 = "pragma key = %Q";
		password_bstr = Marshal::SecureStringToBSTR(password);

		// Convert UTF-16 to UTF-8.
		password_utf8_len = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, 0, NULL, NULL);
		password_utf8 = new char[password_utf8_len];
		int bytesWritten = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, password_utf8_len, NULL, NULL);

		setKeySQL_2 = sqlite3_mprintf(setKeySQL_1, password_utf8);
		status = sqlite3_exec(db, setKeySQL_2, NULL, NULL, NULL);
		if (status != SQLITE_OK)
		{
			throw gcnew UtilException("Failed to set encryption key (SQLITE status " + std::to_string(status) + ").");
		}
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
		if (setKeySQL_2)
		{
			SecureZeroMemory(setKeySQL_2, strlen(setKeySQL_2));
			sqlite3_free(setKeySQL_2);
		}
	}

	// Test our connection (mainly to see if our password was correct).
	std::string test_conn_query = "select count(*) from sqlite_master;";
	status = sqlite3_exec(db, (const char*)test_conn_query.c_str(), NULL, NULL, NULL);
	if (status != SQLITE_OK)
	{
		try {
			sqlite3_close(db);
			db = NULL;
		}
		catch (...) {}
		throw gcnew UtilException("Password incorrect (SQLITE status " + std::to_string(status) + ").");
	}
}


// Set up a new database, creating tables and populating initial values.
void Util::setupNewDb(String^ friendlyName)
{
	int status;

	// Create sites table.
	std::string queryStr = "create table sites(id integer primary key autoincrement, site_id integer, name text, url text, user text, password text, notes text, version integer, deleted integer default 0);";
	sqlite3_stmt* sqlStmt;
	status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to create sites table (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to create sites table (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	// Create metadata table.
	queryStr = "create table metadata(id integer primary key, guid text, friendly_name text, last_modified_utc text);";
	status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to create metadata table (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to create metadata table (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	// Populate metadata table.
	int id = 1;
	std::string guid = msclr::interop::marshal_as<std::string>(Guid::NewGuid().ToString());
	std::string friendlyNameStr = msclr::interop::marshal_as<std::string>(friendlyName);
	std::string lastModifiedTemp = "LAST MODIFIED PLACEHOLDER";
	queryStr = "insert into metadata(id, guid, friendly_name, last_modified_utc) values (?, ?, ?, ?)";
	status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to populate metadata table (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_int(sqlStmt, 1, id);
	if (status == SQLITE_OK) status = 
		sqlite3_bind_text(sqlStmt, 2, (const char*)guid.c_str(), -1, NULL);
	if (status == SQLITE_OK) status = 
		sqlite3_bind_text(sqlStmt, 3, (const char*)friendlyNameStr.c_str(), -1, NULL);
	if (status == SQLITE_OK) status = 
		sqlite3_bind_text(sqlStmt, 4, (const char*)lastModifiedTemp.c_str(), -1, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to populate metadata table (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to populate metadata table (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	// Update last modified time.
	updateLastModifiedTime();
}


// Get metadata.
Metainfo^ Util::getMetadata()
{
	Metainfo^ metadata = gcnew Metainfo();

	std::string query_str = "select guid, friendly_name, last_modified_utc from metadata where id = 1";
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)query_str.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to get metadata (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status == SQLITE_ROW)
	{
		std::string guidStr = (char*)sqlite3_column_text(sqlStmt, 0);
		metadata->guid = msclr::interop::marshal_as<String^>(guidStr);
		std::string friendlyNameStr = (char*)sqlite3_column_text(sqlStmt, 1);
		metadata->friendlyName = msclr::interop::marshal_as<String^>(friendlyNameStr);
		std::string lastModifiedUtcStr = (char*)sqlite3_column_text(sqlStmt, 2);
		metadata->lastModifiedUtc = msclr::interop::marshal_as<String^>(lastModifiedUtcStr);
		status = sqlite3_step(sqlStmt);
	}
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to get metadata (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);
	return metadata;
}


// Get a list of all sites.
List<Site^>^ Util::getSiteList()
{
	std::string query_str = "select s.name, s.site_id, s.id, s.url, s.version from sites s, "
		"(select site_id, max(version) as max_version from sites group by site_id) v "
		"where s.site_id = v.site_id and s.version = v.max_version and s.deleted == 0 "
		"order by lower(s.name)";
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)query_str.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to get site list (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	List<Site^>^ siteList = gcnew List<Site^>();
	while (status == SQLITE_ROW)
	{
		// Get this result.
		Site^ thisSite = gcnew Site();
		std::string thisName = (char*)sqlite3_column_text(sqlStmt, 0);
		thisSite->name = msclr::interop::marshal_as<String^>(thisName);
		thisSite->siteId = sqlite3_column_int(sqlStmt, 1);
		thisSite->id = sqlite3_column_int(sqlStmt, 2);
		std::string thisUrl = (char*)sqlite3_column_text(sqlStmt, 3);
		thisSite->url = msclr::interop::marshal_as<String^>(thisUrl);
		thisSite->version = sqlite3_column_int(sqlStmt, 4);
		siteList->Add(thisSite);

		// Get the next result.
		status = sqlite3_step(sqlStmt);
	}
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to get site list (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);
	return siteList;
}


// Search for records with names like search string.
List<Site^>^ Util::search(String^ searchTermParam)
{
	std::string query_str = "select s.name, s.site_id, s.id, s.version from sites s, "
		"(select site_id, max(version) as max_version from sites group by site_id) v "
		"where s.site_id = v.site_id and s.version = v.max_version and s.deleted == 0 "
		"and (lower(s.name) like ? escape '\\' or lower(s.url) like ? escape '\\' "
		"or lower(s.notes) like ? escape '\\') order by lower(s.name)";
	std::string searchTerm = msclr::interop::marshal_as<std::string>(searchTermParam);
	unsigned int pos = 0;
	std::string charsToEscape = "%_\\";
	while (pos < searchTerm.size())
	{
		if (std::string::npos != charsToEscape.find(searchTerm.substr(pos, 1)))
		{
			searchTerm.insert(pos, "\\");
			pos += 2;
		}
		else
		{
			pos++;
		}
	}
	searchTerm.insert(0, "%");
	searchTerm.insert(searchTerm.size(), "%");
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)query_str.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to perform search (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_text(sqlStmt, 1, (const char*)searchTerm.c_str(), -1, NULL);
	if (status == SQLITE_OK) sqlite3_bind_text(sqlStmt, 2, (const char*)searchTerm.c_str(), -1, NULL);
	if (status == SQLITE_OK) sqlite3_bind_text(sqlStmt, 3, (const char*)searchTerm.c_str(), -1, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to perform search (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	List<Site^>^ siteList = gcnew List<Site^>();
	while (status == SQLITE_ROW)
	{
		// Get this result.
		Site^ thisSite = gcnew Site();
		std::string thisName = (char*)sqlite3_column_text(sqlStmt, 0);
		thisSite->name = msclr::interop::marshal_as<String^>(thisName);
		thisSite->siteId = sqlite3_column_int(sqlStmt, 1);
		thisSite->id = sqlite3_column_int(sqlStmt, 2);
		thisSite->version = sqlite3_column_int(sqlStmt, 3);
		siteList->Add(thisSite);

		// Get the next result.
		status = sqlite3_step(sqlStmt);
	}
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to get site list (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);
	return siteList;
}


// Get a specific site.
Site^ Util::getSite(Site^ site)
{
	return getSite(site->siteId);
}


// Get a specific site.
Site^ Util::getSite(int siteId)
{
	std::string query_str = "select id, site_id, name, url, user, password, notes, version "
		"from sites where site_id = ? and version in "
		"(select max(version) from sites where site_id = ?)";

	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)query_str.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to get site (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_int(sqlStmt, 1, siteId);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to get site (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_int(sqlStmt, 2, siteId);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to get site (bind error " + std::to_string(status) + ").");
	}
	Site^ thisSite = gcnew Site();
	status = sqlite3_step(sqlStmt);
	if (status == SQLITE_ROW)
	{
		thisSite->id = sqlite3_column_int(sqlStmt, 0);
		thisSite->siteId = sqlite3_column_int(sqlStmt, 1);
		std::string thisName = (char*)sqlite3_column_text(sqlStmt, 2);
		thisSite->name = msclr::interop::marshal_as<String^>(thisName);
		std::string thisUrl = (char*)sqlite3_column_text(sqlStmt, 3);
		thisSite->url = msclr::interop::marshal_as<String^>(thisUrl);
		std::string thisUser = (char*)sqlite3_column_text(sqlStmt, 4);
		thisSite->user = msclr::interop::marshal_as<String^>(thisUser);
		std::string thisPassword = (char*)sqlite3_column_text(sqlStmt, 5);
		thisSite->password = msclr::interop::marshal_as<String^>(thisPassword);
		std::string thisNotes = (char*)sqlite3_column_text(sqlStmt, 6);
		thisSite->notes = msclr::interop::marshal_as<String^>(thisNotes);
		thisSite->version = sqlite3_column_int(sqlStmt, 7);

		// Attempt to get the next result (there shouldn't be one).
		status = sqlite3_step(sqlStmt);
	}
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to get site (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);
	return thisSite;
}


// Save a site.
int Util::saveSite(Site^ site)
{
	int status;

	// If this is a new site, determine the site id.
	if (site->version == 1)
	{
		// Get the current max site id.
		int maxSiteId = 0;
		std::string query_str = "select max(site_id) from sites;";
		sqlite3_stmt* sqlStmt;
		status = sqlite3_prepare_v2(db, query_str.c_str(), -1, &sqlStmt, NULL);
		if (status != SQLITE_OK)
		{
			throw gcnew UtilException("Failed to determine max site id (prepare error " + std::to_string(status) + ").");
		}
		status = sqlite3_step(sqlStmt);
		if (status == SQLITE_ROW)
		{
			maxSiteId = sqlite3_column_int(sqlStmt, 0);
			status = sqlite3_step(sqlStmt);
		}
		if (status != SQLITE_DONE)
		{
			throw gcnew UtilException("Failed to determine max site id (step error " + std::to_string(status) + ").");
		}
		sqlite3_finalize(sqlStmt);
		maxSiteId = std::max(0, maxSiteId);
		site->siteId = maxSiteId + 1;
	}

	// Insert the record.
	std::string queryStr = "insert into sites (site_id, name, url, user, password, notes, version) values (?, ?, ?, ?, ?, ?, ?)";
	sqlite3_stmt* sqlStmt;
	status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to save site (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_int(sqlStmt, 1, site->siteId);
	String^ nameTempS = site->name;	std::string nameTemp = msclr::interop::marshal_as<std::string>(nameTempS);
	if (status == SQLITE_OK) status = sqlite3_bind_text(sqlStmt, 2, (const char*)nameTemp.c_str(), -1, NULL);
	String^ urlTempS = site->url; std::string urlTemp = msclr::interop::marshal_as<std::string>(urlTempS);
	if (status == SQLITE_OK) status = sqlite3_bind_text(sqlStmt, 3, (const char*)urlTemp.c_str(), -1, NULL);
	String^ userTempS = site->user; std::string userTemp = msclr::interop::marshal_as<std::string>(userTempS);
	if (status == SQLITE_OK) status = sqlite3_bind_text(sqlStmt, 4, (const char*)userTemp.c_str(), -1, NULL);
	String^ pwTempS = site->password; std::string pwTemp = msclr::interop::marshal_as<std::string>(pwTempS);
	if (status == SQLITE_OK) status = sqlite3_bind_text(sqlStmt, 5, (const char*)pwTemp.c_str(), -1, NULL);
	String^ notesTempS = site->notes; std::string notesTemp = msclr::interop::marshal_as<std::string>(notesTempS);
	if (status == SQLITE_OK) status = sqlite3_bind_text(sqlStmt, 6, (const char*)notesTemp.c_str(), -1, NULL);
	if (status == SQLITE_OK) status = sqlite3_bind_int(sqlStmt, 7, site->version);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to save site (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to save site (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	// Update last modified time.
	updateLastModifiedTime();

	// Return the new site id.
	return site->siteId;
}


// Delete site.
void Util::deleteSite(int id)
{
	// Insert the record.
	std::string queryStr = "update sites set deleted = 1 where id = ?";
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to delete site (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_int(sqlStmt, 1, id);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to delete site (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to delete site (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	// Update last modified time.
	updateLastModifiedTime();
}


// Get export data as a string.
String^ Util::getExportData(System::Security::SecureString^ encryptPassword)
{
	std::stringstream ss;
	ss << "<?xml version=\"1.0\"?>\n";
	ss << "<password-manager>\n";

	// Salt.
	std::vector<unsigned char> salt;
	for (int i = 0; i < 16 /* bytes */; i++) salt.push_back(Crypto::getInstance().genRand());
	ss << "<salt>" << base64_encode(&salt[0], (unsigned int) salt.size()) << "</salt>\n";

	// Derive encryption and auth keys.
	Crypto crypto = Crypto::getInstance();
	std::vector<unsigned char> encryptKey;
	std::vector<unsigned char> authKey;
	crypto.deriveKeys(encryptPassword, salt, encryptKey, authKey);

	// Get and write metadata.
	Metainfo^ metadata = getMetadata();
	ss << "<metadata>\n";
	String^ guid = metadata->guid;
	ss << "<guid>" << msclr::interop::marshal_as<std::string>(guid) << "</guid>\n";
	String^ friendlyName = metadata->friendlyName;
	ss << "<friendly-name>" << msclr::interop::marshal_as<std::string>(friendlyName) << "</friendly-name>\n";
	String^ lastModifiedUtc = metadata->lastModifiedUtc;
	ss << "<last-modified-utc>" << msclr::interop::marshal_as<std::string>(lastModifiedUtc) << "</last-modified-utc>\n";
	ss << "</metadata>\n";

	// Get and write site data.
	std::string query_str = "select s.name, s.url, s.user, s.password, s.notes from sites s, "
		"(select site_id, max(version) as max_version from sites group by site_id) v "
		"where s.site_id = v.site_id and s.version = v.max_version and s.deleted == 0 "
		"order by lower(s.name)";
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)query_str.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to perform search (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	while (status == SQLITE_ROW)
	{
		ss << "<site>\n";

		// Get this result.
		unsigned char ch;
		const unsigned char* namePtr = sqlite3_column_text(sqlStmt, 0);
		int nameBytes = sqlite3_column_bytes(sqlStmt, 0);
		std::vector<unsigned char> thisName(namePtr, namePtr + nameBytes);
		const unsigned char* urlPtr = sqlite3_column_text(sqlStmt, 1);
		int urlBytes = sqlite3_column_bytes(sqlStmt, 1);
		std::vector<unsigned char> thisUrl(urlPtr, urlPtr + urlBytes);
		const unsigned char* userPtr = sqlite3_column_text(sqlStmt, 2);
		int userBytes = sqlite3_column_bytes(sqlStmt, 2);
		std::vector<unsigned char> thisUser(userPtr, userPtr + userBytes);
		const unsigned char* passwordPtr = sqlite3_column_text(sqlStmt, 3);
		int passwordBytes = sqlite3_column_bytes(sqlStmt, 3);
		std::vector<unsigned char> thisPassword(passwordPtr, passwordPtr + passwordBytes);

		// For now, replace invalid UTF-8 characters with '?'.  Eventually,
		// should figure out the best way to convert these.
		const unsigned char* notesPtr = sqlite3_column_text(sqlStmt, 4);
		int notesBytes = sqlite3_column_bytes(sqlStmt, 4);
		unsigned char* notesPtrM = new unsigned char[notesBytes + 1];
		std::copy(notesPtr, notesPtr + notesBytes + 1, notesPtrM);
		int invalidPos = getInvalidUtf8SymbolPosition(notesPtrM, ch);
		while (invalidPos != -1)
		{
			notesPtrM[invalidPos] = '?';
			invalidPos = getInvalidUtf8SymbolPosition(notesPtrM, ch);
		}
		std::vector<unsigned char> thisNotes(notesPtrM, notesPtrM + notesBytes);

		// Encrypt and write XML data.
		std::vector<unsigned char> encryptedVal;
		std::string b64Val;
		if (thisName.size() > 0)
		{
			crypto.encrypt(thisName, encryptKey, authKey, encryptedVal);
			b64Val = base64_encode(&encryptedVal[0], (unsigned int) encryptedVal.size());
			ss << "<name>" << b64Val << "</name>\n";
		}
		else ss << "<name/>\n";
		if (thisUrl.size() > 0)
		{
			crypto.encrypt(thisUrl, encryptKey, authKey, encryptedVal);
			b64Val = base64_encode(&encryptedVal[0], (unsigned int) encryptedVal.size());
			ss << "<url>" << b64Val << "</url>\n";
		}
		else ss << "<url/>\n";
		if (thisUser.size() > 0)
		{
			crypto.encrypt(thisUser, encryptKey, authKey, encryptedVal);
			b64Val = base64_encode(&encryptedVal[0], (unsigned int) encryptedVal.size());
			ss << "<user>" << b64Val << "</user>\n";
		}
		else ss << "<user/>\n";
		if (thisPassword.size() > 0)
		{
			crypto.encrypt(thisPassword, encryptKey, authKey, encryptedVal);
			b64Val = base64_encode(&encryptedVal[0], (unsigned int) encryptedVal.size());
			ss << "<password>" << b64Val << "</password>\n";
		}
		else ss << "<password/>\n";
		if (thisNotes.size() > 0)
		{
			crypto.encrypt(thisNotes, encryptKey, authKey, encryptedVal);
			b64Val = base64_encode(&encryptedVal[0], (unsigned int) encryptedVal.size());
			ss << "<notes>" << b64Val << "</notes>\n";
		}
		else ss << "<notes/>\n";

		ss << "</site>\n";

		// Get the next result.
		status = sqlite3_step(sqlStmt);
	}
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to perform search (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);

	ss << "</password-manager>\n";
	
	return msclr::interop::marshal_as<String^>(ss.str());
}


// Export to a file.
void Util::exportData(String^ filePath, System::Security::SecureString^ encryptPassword)
{
	std::ofstream ofile;
	ofile.open(msclr::interop::marshal_as<std::string>(filePath));
	ofile << msclr::interop::marshal_as<std::string>(getExportData(encryptPassword));
	ofile.close();
}


// Import a CSV.
void Util::importLastPassCSV(String^ csvText)
{
	std::string csvTextMutable = msclr::interop::marshal_as<std::string>(csvText);
	boost::algorithm::replace_all(csvTextMutable, "&amp;", "&"); // Replace &amp; with &.
	boost::char_separator<char> sep("", ",\n", boost::keep_empty_tokens);
	boost::tokenizer<boost::char_separator<char>> tokens(csvTextMutable, sep);
	bool inQuote = false;
	std::string thisField = "";
	int currentField = 0; // 0 = url, 1 = user, 2 = password, 3 = notes, 4 = name, 5 = grouping, 6 = fav
	bool fieldComplete = false;
	Site^ thisSite = gcnew Site();
	thisSite->clear();
	for (const auto& t : tokens)
	{
		std::string thisToken = t;

		if (thisToken.compare("") == 0)
		{
			if (!inQuote) fieldComplete = true;
		}

		if (thisToken.compare("") != 0 && !inQuote)
		{
			if (thisToken.substr(0, 1).compare("\"") == 0)
			{
				inQuote = true;
				thisToken = thisToken.substr(1);
			}
			else if (thisToken.compare("\n") == 0 || thisToken.compare(",") == 0)
			{
				continue;  // This is just a delimiter.
			}
			else
			{
				fieldComplete = true;
			}
		}

		if (thisToken.compare("") != 0 && inQuote)
		{
			if (thisToken.compare("\n") == 0 || thisToken.compare(",") == 0)
			{
				// Leave thisToken = t;
			}
			else
			{
				boost::regex e("(\"*)$");
				boost::match_results<std::string::const_iterator> what;
				boost::match_flag_type flags = boost::match_default;
				if (boost::regex_search(thisToken, what, e, flags) && what[1].length() % 2 == 1)
				{
					inQuote = false;
					fieldComplete = true;
					boost::algorithm::replace_all(thisToken, "\"\"", "\"");
					thisToken = thisToken.substr(0, thisToken.length() - 1);
				}
				else
				{
					boost::algorithm::replace_all(thisToken, "\"\"", "\"");
				}
			}
		}

		thisField += thisToken;

		if (fieldComplete)
		{
			fieldComplete = false;
			switch (currentField)
			{
				case 0: thisSite->url = msclr::interop::marshal_as<String^>(thisField); currentField++; break;
				case 1: thisSite->user = msclr::interop::marshal_as<String^>(thisField); currentField++; break;
				case 2: thisSite->password = msclr::interop::marshal_as<String^>(thisField); currentField++; break;
				case 3: thisSite->notes = msclr::interop::marshal_as<String^>(thisField); currentField++; break;
				case 4: thisSite->name = msclr::interop::marshal_as<String^>(thisField); currentField++; break;
				case 5: currentField++; break;
				case 6: saveSite(thisSite); thisSite->clear(); currentField = 0; break;
				default: break;
			}
			thisField = "";
		}
	}

	// Update last modified time.
	updateLastModifiedTime();
}


// Change master password.
void Util::changeMasterPassword(System::Security::SecureString^ newPassword)
{
	IntPtr password_bstr = IntPtr::Zero;
	char* password_utf8 = 0;
	int password_utf8_len = 0;
	char* setKeySQL_2 = 0;
	try
	{
		char* setKeySQL_1 = "pragma rekey = %Q";
		password_bstr = Marshal::SecureStringToBSTR(newPassword);

		// Convert UTF-16 to UTF-8.
		password_utf8_len = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, 0, NULL, NULL);
		password_utf8 = new char[password_utf8_len];
		int bytesWritten = WideCharToMultiByte(CP_UTF8, 0, (wchar_t*)password_bstr.ToPointer(),
			-1, password_utf8, password_utf8_len, NULL, NULL);

		setKeySQL_2 = sqlite3_mprintf(setKeySQL_1, password_utf8);
		int status = sqlite3_exec(db, setKeySQL_2, NULL, NULL, NULL);
		if (status != SQLITE_OK)
		{
			throw gcnew UtilException("Failed to change master password (rekey status " + std::to_string(status) + ").");
		}
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
		if (setKeySQL_2)
		{
			SecureZeroMemory(setKeySQL_2, strlen(setKeySQL_2));
			sqlite3_free(setKeySQL_2);
		}
	}

	std::string test_conn_query = "select count(*) from sites;";
	int status = sqlite3_exec(db, (const char*)test_conn_query.c_str(), NULL, NULL, NULL);
	if (status != SQLITE_OK)
	{
		try {
			sqlite3_close(db);
		}
		catch (...) {}
		throw gcnew UtilException("Failed to change master password (select status " + std::to_string(status) + ").");
	}

	// Update last modified time.
	updateLastModifiedTime();
}


// Update last modified date/time.
void Util::updateLastModifiedTime()
{
	// Get the current date/time.
	SYSTEMTIME st;
	GetSystemTime(&st);
	char buffer[24];
	sprintf(buffer, "%04d/%02d/%02d %02d:%02d:%02d %03d",
		st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

	// Insert the record.
	std::string queryStr = "update metadata set last_modified_utc = ? where id = 1";
	sqlite3_stmt* sqlStmt;
	int status = sqlite3_prepare_v2(db, (const char*)queryStr.c_str(), -1, &sqlStmt, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to update last modified time (prepare error " + std::to_string(status) + ").");
	}
	status = sqlite3_bind_text(sqlStmt, 1, buffer, -1, NULL);
	if (status != SQLITE_OK)
	{
		throw gcnew UtilException("Failed to update last modified time (bind error " + std::to_string(status) + ").");
	}
	status = sqlite3_step(sqlStmt);
	if (status != SQLITE_DONE)
	{
		throw gcnew UtilException("Failed to update last modified time (step error " + std::to_string(status) + ").");
	}
	sqlite3_finalize(sqlStmt);
}


// Encode into base64.
std::string Util::base64_encode(unsigned char const* buf, unsigned int bufLen)
{
	std::string ret;
	int i = 0;
	int j = 0;
	unsigned char char_array_3[3];
	unsigned char char_array_4[4];

	while (bufLen--) {
		char_array_3[i++] = *(buf++);
		if (i == 3) {
			char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
			char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
			char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
			char_array_4[3] = char_array_3[2] & 0x3f;

			for (i = 0; (i <4); i++)
				ret += (char) base64_chars[char_array_4[i]];
			i = 0;
		}
	}

	if (i)
	{
		for (j = i; j < 3; j++)
			char_array_3[j] = '\0';

		char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
		char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
		char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
		char_array_4[3] = char_array_3[2] & 0x3f;

		for (j = 0; (j < i + 1); j++)
			ret += (char) base64_chars[char_array_4[j]];

		while ((i++ < 3))
			ret += '=';
	}

	return ret;
}


// Decode from base64 into bytes.
std::vector<unsigned char> Util::base64_decode(std::string const& encoded_string)
{
	int in_len = (int) encoded_string.size();
	int i = 0;
	int j = 0;
	int in_ = 0;
	unsigned char char_array_4[4], char_array_3[3];
	std::vector<unsigned char> ret;

	while (in_len-- && (encoded_string[in_] != '=') && is_base64(encoded_string[in_])) {
		char_array_4[i++] = encoded_string[in_]; in_++;
		if (i == 4) {
			for (i = 0; i <4; i++)
				char_array_4[i] = base64_chars->IndexOf(char_array_4[i]);

			char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
			char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
			char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

			for (i = 0; (i < 3); i++)
				ret.push_back(char_array_3[i]);
			i = 0;
		}
	}

	if (i) {
		for (j = i; j <4; j++)
			char_array_4[j] = 0;

		for (j = 0; j <4; j++)
char_array_4[j] = base64_chars->IndexOf(char_array_4[j]);

char_array_3[0] = (char_array_4[0] << 2) + ((char_array_4[1] & 0x30) >> 4);
char_array_3[1] = ((char_array_4[1] & 0xf) << 4) + ((char_array_4[2] & 0x3c) >> 2);
char_array_3[2] = ((char_array_4[2] & 0x3) << 6) + char_array_4[3];

for (j = 0; (j < i - 1); j++) ret.push_back(char_array_3[j]);
	}

	return ret;
}


///Returns -1 if string is valid. Invalid character is put to ch.
int Util::getInvalidUtf8SymbolPosition(const unsigned char *input, unsigned char &ch) {
	int                 nb, na;
	const unsigned char *c = input;

	for (c = input; *c; c += (nb + 1)) {
		if (!(*c & 0x80))
			nb = 0;
		else if ((*c & 0xc0) == 0x80)
		{
			ch = *c;
			// return (int)c - (int)input;
			return (int)(c - input);
		}
		else if ((*c & 0xe0) == 0xc0)
			nb = 1;
		else if ((*c & 0xf0) == 0xe0)
			nb = 2;
		else if ((*c & 0xf8) == 0xf0)
			nb = 3;
		else if ((*c & 0xfc) == 0xf8)
			nb = 4;
		else if ((*c & 0xfe) == 0xfc)
			nb = 5;
		na = nb;
		while (na-- > 0)
			if ((*(c + nb) & 0xc0) != 0x80)
			{
				ch = *(c + nb);
				// return (int)(c + nb) - (int)input;
				return (int)(c + nb - input);
			}
	}

	return -1;
}


// Clean up.
void Util::cleanUp()
{
	// Close the database.
	sqlite3_close(db);

	// Delete the timer.
	delete timer;
}


#define NO_VALUE    (-1)
#define LEPT_TRUE   1
#define LEPT_FALSE  0
#define LEPT_OK     0
#define LEPT_ERROR  1
#define MAX_FILE_LEN  512

typedef enum
{
	NEGATE_NO,   /* Do not negate image */
	NEGATE_YES,  /* Force negate */
	NEGATE_AUTO, /* Automatically negate if border pixels are dark */

} Negate_enum;


// Process an image and return OCR text.
String^ Util::doOCR(System::Drawing::Bitmap^ bitmap)
{
	// Using namespaces.
	using namespace System::Drawing;
	using namespace System::Drawing::Imaging;

	// Convert from Bitmap to Pix.
	PixelFormat bitmapFormat = bitmap->PixelFormat;
	int bitmapDepth = 0;
	if (bitmapFormat == PixelFormat::Format1bppIndexed) bitmapDepth = 1;
	else if (bitmapFormat == PixelFormat::Format8bppIndexed) bitmapDepth = 8;
	else if (bitmapFormat == PixelFormat::Format32bppArgb) bitmapDepth = 32;
	else if (bitmapFormat == PixelFormat::Format24bppRgb) bitmapDepth = 32;
	else
	{
		throw gcnew UtilException("Bitmap pixel format " + std::to_string((int)bitmapFormat) + " is not supported.");
	}

	// Create new pix.
	Pix* pix = pixCreate(bitmap->Width, bitmap->Height, bitmapDepth);

	// Copy the color map, if necessary.
	if ((bitmapFormat & PixelFormat::Indexed) == PixelFormat::Indexed)
	{
		ColorPalette^ bitmapPalette = bitmap->Palette;
		array<Color>^ bpEntries = bitmapPalette->Entries;
		PixColormap* pixColormap = pixcmapCreate(pix->d);
		try
		{
			for (int i = 0; i < bpEntries->Length; i++)
			{
				int result = pixcmapAddColor(pixColormap, bpEntries[i].R, bpEntries[i].G, bpEntries[i].B);
				if (result != 0)
				{
					throw gcnew UtilException("Error adding color to color map.");
				}
			}
			pix->colormap = pixColormap;
		}
		catch (Exception^ ex)
		{
			throw gcnew UtilException("Exception while trying to copy colormap.", ex);
		}
	}

	// Copy pixel data.
	System::Drawing::Rectangle bitmapRect(0, 0, bitmap->Width, bitmap->Height);
	BitmapData^ bitmapData = bitmap->LockBits(bitmapRect, ImageLockMode::ReadOnly, bitmapFormat);
	l_uint32* pixData = pix->data;
	int height = bitmap->Height;
	int width = (bitmapFormat == PixelFormat::Format1bppIndexed) ? bitmap->Width / 8 : bitmap->Width;
	int pixWordsPerLine = pixGetWpl(pix);
	for (int y = 0; y < height; y++)
	{
		unsigned char* bitmapLine = (unsigned char*)(bitmapData->Scan0.ToPointer()) + (y * bitmapData->Stride);
		l_uint32* pixLine = pixData + (y * pixWordsPerLine);
		if (bitmapFormat == PixelFormat::Format1bppIndexed)
		{
			for (int x = 0; x < width; x++)
			{
				unsigned char pixelVal = *(bitmapLine + x);
				SET_DATA_BYTE(pixLine, x, pixelVal);
			}
		}
		else if (bitmapFormat == PixelFormat::Format24bppRgb)
		{
			for (int x = 0; x < width; x++)
			{
				unsigned char* ppixelVal = bitmapLine + x * 3;
				unsigned char blue = ppixelVal[0];
				unsigned char green = ppixelVal[1];
				unsigned char red = ppixelVal[2];
				unsigned char alpha = 255;
				l_uint32 rgbaVal = (l_uint32)(red << 24 | green << 16 | blue << 8 | alpha);
				SET_DATA_FOUR_BYTES(pixLine, x, rgbaVal);
			}
		}
		else if (bitmapFormat == PixelFormat::Format32bppArgb)
		{
			for (int x = 0; x < width; x++)
			{
				unsigned char* ppixelVal = bitmapLine + x * 4;
				unsigned char blue = ppixelVal[0];
				unsigned char green = ppixelVal[1];
				unsigned char red = ppixelVal[2];
				unsigned char alpha = ppixelVal[3];
				l_uint32 rgbaVal = (l_uint32)(red << 24 | green << 16 | blue << 8 | alpha);
				SET_DATA_FOUR_BYTES(pixLine, x, rgbaVal);
			}
		}
		else if (bitmapFormat == PixelFormat::Format8bppIndexed)
		{
			for (int x = 0; x < width; x++)
			{
				unsigned char pixelVal = *(bitmapLine + x);
				SET_DATA_BYTE(pixLine, x, pixelVal);
			}
		}
	}

	l_int32 status = LEPT_ERROR;
	
	// char source_file[MAX_FILE_LEN] = "in.png";
	// char dest_file[MAX_FILE_LEN] = "out.png";

	Negate_enum perform_negate = NEGATE_AUTO;
	l_float32 dark_bg_threshold = 0.5f; /* From 0.0 to 1.0, with 0 being all white and 1 being all black */

	int perform_scale = LEPT_TRUE;
	l_float32 scale_factor = 3.5f;

	int perform_unsharp_mask = LEPT_TRUE;
	l_int32 usm_halfwidth = 5;
	l_float32 usm_fract = 2.5f;

	int perform_otsu_binarize = LEPT_TRUE;
	l_int32 otsu_sx = 2000;
	l_int32 otsu_sy = 2000;
	l_int32 otsu_smoothx = 0;
	l_int32 otsu_smoothy = 0;
	l_float32 otsu_scorefract = 0.0f;

	l_float32 border_avg = 0.0f;
	//Pix *pixs = NULL;
	char *ext = NULL;

	// strcpy_s(source_file, MAX_FILE_LEN, "C:\\Users\\jason\\Documents\\Projects\\PasswordManager\\screenshot.bmp");
	// strcpy_s(dest_file, MAX_FILE_LEN, "C:\\Users\\jason\\Documents\\Projects\\PasswordManager\\ocr_input.tif");

	// perform_negate = atoi("2");  (Leave at NEGATE_AUTO [= 2])
	dark_bg_threshold = (float)atof("0.5");

	perform_scale = atoi("1");
	scale_factor = (float)atof("3.5");

	perform_unsharp_mask = atoi("1");
	usm_halfwidth = atoi("5");
	usm_fract = (float)atof("2.5");

	perform_otsu_binarize = atoi("1");
	otsu_sx = atoi("2000");
	otsu_sy = atoi("2000");
	otsu_smoothx = atoi("0");
	otsu_smoothy = atoi("0");
	otsu_scorefract = (float)atof("0.0");

	// Convert to grayscale.
	pix = pixConvertRGBToGray(pix, 0.0f, 0.0f, 0.0f); if (pix == NULL) return "ERROR 2";

	/*// See if we need to negate the image.
	Pix *otsu_pixs = NULL;
	status = pixOtsuAdaptiveThreshold(pix, otsu_sx, otsu_sy, otsu_smoothx, otsu_smoothy, otsu_scorefract, NULL, &otsu_pixs);
	if (status != LEPT_OK) return "ERROR 4";
	// Get the average intensity of the border pixels,
	// with average of 0.0 being completely white and 1.0 being completely black.
	border_avg = pixAverageOnLine(otsu_pixs, 0, 0, otsu_pixs->w - 1, 0, 1); // Top
	border_avg += pixAverageOnLine(otsu_pixs, 0, otsu_pixs->h - 1, otsu_pixs->w - 1, otsu_pixs->h - 1, 1); // Bottom
	border_avg += pixAverageOnLine(otsu_pixs, 0, 0, 0, otsu_pixs->h - 1, 1); // Left
	border_avg += pixAverageOnLine(otsu_pixs, otsu_pixs->w - 1, 0, otsu_pixs->w - 1, otsu_pixs->h - 1, 1); // Right
	border_avg /= 4.0f;
	pixDestroy(&otsu_pixs);
	
	// Negate the image if background is dark.
	if (border_avg > dark_bg_threshold) pixInvert(pix, pix);
	if (pix == NULL) return "ERROR 5";*/

	// Scale the image (linear interpolation).
	pix = pixScaleGrayLI(pix, scale_factor, scale_factor); if (pix == NULL) return "ERROR 6";

	// Apply unsharp mask.
	pix = pixUnsharpMaskingGray(pix, usm_halfwidth, usm_fract); if (pix == NULL) return "ERROR 7";

	// Binarize.
	status = pixOtsuAdaptiveThreshold(pix, otsu_sx, otsu_sy, otsu_smoothx, otsu_smoothy, otsu_scorefract, NULL, &pix);
	if (status != LEPT_OK) return "ERROR 8";

	// Write the image to file.
	// status = pixWriteImpliedFormat(dest_file, pix, 0, 0); if (status != LEPT_OK) return "ERROR 9";

	// Open input image with leptonica library
	//Pix *image = pixRead("/usr/src/tesseract-3.02/phototest.tif");
	// api->SetPageSegMode(tesseract::PageSegMode::PSM_AUTO);
	api->SetImage(pix);
	api->SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz/.:");
	// Get OCR result
	char* outText = api->GetUTF8Text();
	String^ result = gcnew String(outText);

	// Destroy used object and release memory
	//api->End();
	delete[] outText;
	pixDestroy(&pix);

	return result;
}


