#include "stdafx.h"
#include "UtilException.h"

#include <msclr/marshal_cppstd.h>



using namespace WAppCpp;


UtilException::UtilException()
	: System::Exception()
{

}

UtilException::UtilException(std::string message)
	: System::Exception(msclr::interop::marshal_as<System::String^>(message))
{

}

UtilException::UtilException(std::string message, System::Exception^ cause)
	: System::Exception(msclr::interop::marshal_as<System::String^>(message), cause)
{

}

UtilException::UtilException(System::String^ message)
	: System::Exception(message)
{

}

UtilException::UtilException(System::String^ message, System::Exception^ cause)
	: System::Exception(message, cause)
{

}

