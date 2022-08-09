// kym.h - Contains declarations of Keymeleon's cpp library functions
#pragma once

#ifdef KYM_EXPORTS
#define KYM_TEST_API __declspec(dllexport)
#else
#define KYM_TEST_API __declspec(dllimport)
#endif

extern "C" KYM_TEST_API int test();