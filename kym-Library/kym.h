// kym.h - Contains declarations of Keymeleon's cpp library functions
#pragma once
#include <fstream>
#include <string>
#include <map>
#include <array>
#include <vector>
#include <hidapi.h>

#ifdef KYM_EXPORTS
#define KYM_TEST_API __declspec(dllexport)
#else
#define KYM_TEST_API __declspec(dllimport)
#endif

extern "C" KYM_TEST_API int setCustomLayout(char* configFileName, int profileToModify);
extern "C" KYM_TEST_API int setActiveProfile(int profile);

extern std::map<std::string, std::array<uint8_t, 3>> map_keycodes;

extern uint8_t data_profile[64];
extern uint8_t data_start[64];
extern uint8_t data_end[64];
extern uint8_t data_settings[64];

hid_device* openKeyboard();
int writeToKeyboard(hid_device* handle, uint8_t buf[], int length);
std::vector<std::pair<std::string, std::array<uint8_t, 3>>> readConfigFromFile(char* filename);