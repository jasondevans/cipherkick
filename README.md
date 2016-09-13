# Cipher Kick Password Utility

I wasn't happy with existing password managers, so I decided to write my own. Cipher Kick is designed to be simple, functional, and with a user interface that is as pleasant to use as possible.

I found many password managers attempt to "do it all" -- automatically figuring out which fields need to be filled in and automatically doing so, automatically changing passwords, automatically launching and logging you in to sites.  These features are great in principle, but I found that in practice there were often issues, with many of them working correctly around 90% of the time, and the other 10% things would go wrong in irritating ways.  For example, filling in the wrong fields, being unable to handle non-standard login windows, attempting to automatically log me in as one user when I have a second user at the same site I wanted to log in as.  Cipher Kick attempts to give you back the control, while still making it easy to use.  In many cases, Cipher Kick can figure out which user / password is needed, and you tell it when to type in either of those using simple hotkey shortcuts, e.g. "Ctrl+Shift+U" means type the appropriate username here, and "Ctrl+Shift+P" means type the appropriate password here.

Many existing password managers have user interfaces that I found awkward to use and/or unpleasant to look at.  Cipher Kick is designed to have a simple, clean look, making it easy to do the things you're trying to do.

While syncing your passwords through the cloud is certainly convenient, I also have concerns about doing so, and about trusting a third party.  While there are pros and cons to cloud syncing, currently Cipher Kick is conservative and supports only local syncing via Bluetooth.

## Features
* Save usernames, passwords, and URL's, along with another other information in a general notes field
* Hotkeys to fill in user and password, which Cipher Kick attempts to guess in two main ways:
  * Through looking at the window title, e.g. "Amazon -- Log In - Google Chrome" would match to an "Amazon" entry
  * If that doesn't find a match, taking a screen shot of the current window, and performing OCR text recognition of the top portion and attempting to match to the URL field
* Currently runs on Windows (desktop version) and Windows Phone (mobile version).  Android app in progress.
* Bluetooth sync
* Can automatically time out when user is idle
* Import/export via CSV
* Password generation

## Open source software used
* [SQLite] (https://www.sqlite.org/)
* [SQLCipher] (https://www.zetetic.net/sqlcipher/)
* [OpenSSL] (https://www.openssl.org/)
* [32feet.NET Bluetooth library] (https://32feet.codeplex.com/)
* [Windows Input Simulator] (https://inputsimulator.codeplex.com/)
* [Tesseract OCR] (https://github.com/tesseract-ocr/tesseract)
* [Leptonica] (http://www.leptonica.com/) (and libgif, libjpeg, libpng, libtiff, libwebp, zlib)
* [Boost] (http://www.boost.org/)

## Building
Building currently requires Visual Studio 2015, and running requires .NET Framework version 4.5.2 or later.  All other dependencies, including libraries for the above open source projects, are included pre-built with this project (e.g. in the "lib" and "include" directories).

### Desktop application
Download source and open "CipherKick.sln", then run the appropriate build, currently four build configurations are defined, with variants for Debug/Release and x64/x86.  After build, binaries and all necessary libraries should be in out/{configdir} where "configdir" is one of x64, x64d, x86, x86d.  You can run the Cipher Kick Password Utility by running "WindowsApp.exe".

### Windows Phone app
Inside the WPhoneApp directory is another VS2015 solution file.  Open this, and build.  Output is in the "out" directories.
