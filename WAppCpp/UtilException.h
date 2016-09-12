#pragma once


namespace WAppCpp
{
	public ref class UtilException : public System::Exception
	{

	public:

		UtilException();

		UtilException(std::string message);

		UtilException(std::string message, System::Exception^ cause);

		UtilException(System::String^ message);

		UtilException(System::String^ message, System::Exception^ cause);

	};
}

