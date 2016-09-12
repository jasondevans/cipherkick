#include "stdafx.h"

#include "Site.h"


using namespace WAppCpp;


// Constructor.
Site::Site()
{
	clear();
}

// Destructor.
Site::~Site()
{
}


// Clear site (reset all values to defaults).
void Site::clear()
{
	id = 0;
	siteId = 0;
	name = "Name";
	url = "url";
	user = "user";
	password = "pw";
	notes = "notes";
	version = 1;
	canonicalUrl2 = "";
	canonicalUrl3 = "";
	canonicalName1 = "";
	canonicalName2 = "";
	canonicalName3 = "";
	canonicalName4 = "";
}

