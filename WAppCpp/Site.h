#pragma once

#include "stdafx.h"

using namespace System;

namespace WAppCpp {
	public ref class Site
	{

	public:

		int id;
		int siteId;
		String^ name;
		String^ url;
		String^ user;
		String^ password;
		String^ notes;
		int version;
		String^ canonicalUrl2;
		String^ canonicalUrl3;
		String^ canonicalName1;
		String^ canonicalName2;
		String^ canonicalName3;
		String^ canonicalName4;

		property int Id {
			int get() { return id; }
			void set(int _id) { id = _id; }
		}
		property int SiteId {
			int get() { return siteId; }
			void set(int _siteId) { siteId = _siteId; }
		}
		property String^ Name {
			String^ get() { return name; }
			void set(String^ _name) { name = _name; }
		}
		property String^ Url {
			String^ get() { return url; }
			void set(String^ _url) { url = _url; }
		}
		property String^ User {
			String^ get() { return user; }
			void set(String^ _user) { user = _user; }
		}
		property String^ Password {
			String^ get() { return password; }
			void set(String^ _password) { password = _password; }
		}
		property String^ Notes {
			String^ get() { return notes; }
			void set(String^ _notes) { notes = _notes; }
		}
		property int Version {
			int get() { return version; }
			void set(int _version) { version = _version; }
		}
		property String^ CanonicalUrl2 {
			String^ get() { return canonicalUrl2; }
			void set(String^ _canonicalUrl2) { canonicalUrl2 = _canonicalUrl2; }
		}
		property String^ CanonicalUrl3 {
			String^ get() { return canonicalUrl3; }
			void set(String^ _canonicalUrl3) { canonicalUrl3 = _canonicalUrl3; }
		}
		property String^ CanonicalName1 {
			String^ get() { return canonicalName1; }
			void set(String^ _canonicalName1) { canonicalName1 = _canonicalName1; }
		}
		property String^ CanonicalName2 {
			String^ get() { return canonicalName2; }
			void set(String^ _canonicalName2) { canonicalName2 = _canonicalName2; }
		}
		property String^ CanonicalName3 {
			String^ get() { return canonicalName3; }
			void set(String^ _canonicalName3) { canonicalName3 = _canonicalName3; }
		}
		property String^ CanonicalName4 {
			String^ get() { return canonicalName4; }
			void set(String^ _canonicalName4) { canonicalName4 = _canonicalName4; }
		}

		// Constructor.
		Site();

		// Destructor.
		virtual ~Site();

		// Clear site (reset all values to defaults.
		virtual void clear();

	};
}

